using LibraryManagementApp.Contexts;
using LibraryManagementApp.Interfaces;
using LibraryManagementApp.Presentation;
using LibraryManagementApp.Repositories;
using LibraryManagementApp.Services;

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
