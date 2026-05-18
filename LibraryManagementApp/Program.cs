using LibraryManagementApp.Contexts;
using LibraryManagementApp.Interfaces;
using LibraryManagementApp.Presentation;
using LibraryManagementApp.Repositories;
using LibraryManagementApp.Services;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
	.MinimumLevel.Information()
	.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
	.Enrich.FromLogContext()
	.WriteTo.Console(outputTemplate: "\n{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
	.WriteTo.File(
		path: "Logs/library-management-.log",
		rollingInterval: RollingInterval.Day,
		retainedFileCountLimit: 30,
		shared: true,
		outputTemplate: "\n{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
	.CreateLogger();

try
{
	Log.Information("Application starting");

	using var context = new LibraryDbContext();

	IMemberRepository memberRepository = new MemberRepository(context);
	IMembershipRepository membershipRepository = new MembershipRepository(context);
	ICategoryRepository categoryRepository = new CategoryRepository(context);
	IBookRepository bookRepository = new BookRepository(context);
	IBookCopyRepository bookCopyRepository = new BookCopyRepository(context);
	IBorrowRepository borrowRepository = new BorrowRepository(context);
	IFineRepository fineRepository = new FineRepository(context);

	var memberService = new MemberService(memberRepository);
	var bookService = new BookService(bookRepository, bookCopyRepository, categoryRepository);
	var borrowService = new BorrowService(
		context,
		borrowRepository,
		memberRepository,
		bookRepository,
		bookCopyRepository,
		membershipRepository,
		fineRepository);

	var interactService = new InteractService(memberService, bookService, borrowService, categoryRepository, membershipRepository);
	interactService.Start();

	Log.Information("Application stopped normally");
}
catch (Exception ex)
{
	Log.Fatal(ex, "Application terminated unexpectedly");
	throw;
}
finally
{
	Log.CloseAndFlush();
}
