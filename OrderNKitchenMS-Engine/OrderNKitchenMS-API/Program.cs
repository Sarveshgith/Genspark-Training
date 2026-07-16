using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderNKitchenMS_API.Data;
using OrderNKitchenMS_API.Exceptions;
using OrderNKitchenMS_API.Repositories;
using OrderNKitchenMS_API.Repositories.Interfaces;
using OrderNKitchenMS_API.Services;
using OrderNKitchenMS_API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using QuestPDF.Infrastructure;
using Scalar.AspNetCore;
using Serilog;
using OrderNKitchenMS_API.Hubs;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    Env.Load();
}

// In production, the CSI Secret Store driver mounts each Key Vault secret as a
// file inside /mnt/secrets-store/<objectName>. We add these as configuration
// sources so IConfiguration.GetValue / GetConnectionString resolve them
// without needing the Kubernetes Secret sync (secretObjects) step, which is
// the source of the CreateContainerConfigError.
const string csiMountPath = "/mnt/secrets-store";
if (Directory.Exists(csiMountPath))
{
    foreach (var file in Directory.EnumerateFiles(csiMountPath))
    {
        // The objectName uses "--" as a hierarchy separator (Azure KV limitation).
        // Replace "--" with ":" so .NET IConfiguration understands the key path.
        // e.g. "ConnectionStrings--DefaultConnection" → "ConnectionStrings:DefaultConnection"
        var key = Path.GetFileName(file).Replace("--", ":");
        var value = File.ReadAllText(file).Trim();
        builder.Configuration[key] = value;
    }
}

//AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

//Serilog Service
builder.Logging.ClearProviders();
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services));

//CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        var allowedOrigins = builder.Configuration["AllowedOrigins"] ?? "http://localhost:4200";
        var origins = allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        policy.WithOrigins(origins) 
              .WithHeaders("Content-Type", "Authorization", "Accept", "X-Requested-With", "x-signalr-user-agent")
              .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
              .AllowCredentials();
    });
});

//PdfGen Provider License
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddMemoryCache();

builder.Services.AddSignalR()
    .AddAzureSignalR(builder.Configuration["SignalR:ConnectionString"]);

//JWT Authentication
var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"] 
                   ?? "mysuperlongkeywithnospellingmistakesandalsoitneedstobeatleast32characterslongmysuperlongkeywithnospellingmistakesandalsoitneedstobeatleast32characterslong";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "AmbrosiaOrderSystems";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "AmbrosiaOrderSystemsUsers";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];

                // If the request is for our hub...
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/restaurantHub")))
                {
                    // Read the token out of the query string
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

//Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

    options.AddPolicy("ChefOnly", policy => policy.RequireRole("Chef"));

    options.AddPolicy("AdminOrChef", policy => policy.RequireRole("Admin", "Chef"));

    options.AddPolicy("AdminOrWaiter", policy => policy.RequireRole("Admin", "Waiter"));

    options.AddPolicy("AllStaff", policy => policy.RequireRole("Admin", "Chef", "Waiter"));

    options.AddPolicy("WaiterOnly", policy => policy.RequireRole("Waiter"));

    options.AddPolicy("GuestSession",
        policy => policy.RequireClaim("SessionType", "Guest"));

    options.AddPolicy("CanPlaceOrder",
        policy => policy.RequireAssertion(ctx =>
            ctx.User.IsInRole("Admin")  ||
            ctx.User.IsInRole("Waiter")    
        ));

    options.AddPolicy("All",
        policy => policy.RequireAssertion(ctx =>
            ctx.User.IsInRole("Admin")  ||
            ctx.User.IsInRole("Chef")   ||
            ctx.User.IsInRole("Waiter") ||
            ctx.User.HasClaim(c => c.Type == "SessionType" && c.Value == "Guest")
        ));
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    var dbHost = builder.Configuration["DB_HOST"];
    var dbPort = builder.Configuration["DB_PORT"] ?? "5432";
    var dbDatabase = builder.Configuration["DB_DATABASE"] ?? builder.Configuration["DB_NAME"];
    var dbUsername = builder.Configuration["DB_USERNAME"] ?? builder.Configuration["DB_USER"];
    var dbPassword = builder.Configuration["DB_PASSWORD"];

    if (!string.IsNullOrEmpty(dbHost) && !string.IsNullOrEmpty(dbDatabase) && !string.IsNullOrEmpty(dbUsername) && !string.IsNullOrEmpty(dbPassword))
    {
        connectionString = $"Host={dbHost};Port={dbPort};Database={dbDatabase};Username={dbUsername};Password={dbPassword};";
    }
    else
    {
        connectionString = builder.Configuration["DB_CONNECTION_STRING"] ?? string.Empty;
    }
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<ITableRepository, TableRepository>();
builder.Services.AddScoped<ITableService, TableService>();

builder.Services.AddScoped<IMenuItemRepository, MenuItemRepository>();
builder.Services.AddScoped<IMenuService, MenuService>();

builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<IMenuItemIngredientRepository, MenuItemIngredientRepository>();
builder.Services.AddScoped<IItemService, ItemService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

builder.Services.AddScoped<IOrderItemRepository, OrderItemRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddScoped<IBillRepository, BillRepository>();
builder.Services.AddScoped<IBillService, BillService>();

builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IReportService, ReportService>();

builder.Services.AddScoped<ISignalService, SignalService>();

builder.Services.AddScoped<IGenAIService, GenAIService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

//Strictly follow Content-Type, Avoid Clickjacking, Omit Referrer headers
app.Use(async (context, next) =>
{   
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHub<RestaurantHub>("/restaurantHub");

app.MapGet("/health/live", () => Results.Ok(new { Status = "Healthy" }));
app.MapGet("/health/ready", async (AppDbContext dbContext) => 
{
    try 
    {
        var canConnect = await dbContext.Database.CanConnectAsync();
        return canConnect ? Results.Ok(new { Status = "Ready" }) : Results.Problem("Database connection failed", statusCode: 500);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Database check failed: {ex.Message}", statusCode: 500);
    }
});

app.MapGet("/api/health", async (AppDbContext dbContext) => {
    try 
    {
        var canConnect = await dbContext.Database.CanConnectAsync();
        if (canConnect)
        {
            return Results.Ok(new { Status = "Healthy", Database = "Connected" });
        }
        return Results.Problem("Database connection failed", statusCode: 500);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Database check failed: {ex.Message}", statusCode: 500);
    }
});

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
