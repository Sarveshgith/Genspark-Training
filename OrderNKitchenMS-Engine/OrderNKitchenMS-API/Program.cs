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

Env.Load();

var builder = WebApplication.CreateBuilder(args);

//AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

builder.Logging.ClearProviders();
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200") 
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddSignalR();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidateAudience = false,
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]!)),
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

var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
                       ?? builder.Configuration.GetConnectionString("DefaultConnection");

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

builder.Services.AddScoped<GenAIService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHub<RestaurantHub>("/restaurantHub");

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
