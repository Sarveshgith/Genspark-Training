using LibraryManagementApp.Contexts;
using LibraryManagementApp.Presentation;
using LibraryManagementApp.Repositories;
using LibraryManagementApp.Services;

using var context = new LibraryDbContext();

var memberRepository = new MemberRepository(context);
var membershipRepository = new MembershipRepository(context);
var categoryRepository = new CategoryRepository(context);
var bookRepository = new BookRepository(context);
var bookCopyRepository = new BookCopyRepository(context);
var borrowRepository = new BorrowRepository(context);
var fineRepository = new FineRepository(context);

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
