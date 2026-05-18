using System;
using LibraryManagementApp.Enums;
using LibraryManagementApp.Models;
using LibraryManagementApp.Models.Exceptions;
using LibraryManagementApp.Repositories;
using LibraryManagementApp.Services;

namespace LibraryManagementApp.Presentation;

internal class InteractService
{
	private readonly MemberService _memberService;
	private readonly BookService _bookService;
	private readonly BorrowService _borrowService;
	private readonly CategoryRepository _categoryRepository;
	private readonly MembershipRepository _membershipRepository;

	private Member? _currentUser;

	public InteractService(MemberService memberService, BookService bookService, BorrowService borrowService, CategoryRepository categoryRepository, MembershipRepository membershipRepository)
	{
		_memberService = memberService;
		_bookService = bookService;
		_borrowService = borrowService;
		_categoryRepository = categoryRepository;
		_membershipRepository = membershipRepository;
	}

	public void Start()
	{
		bool exit = false;

		while (!exit)
		{
			Console.WriteLine();
			ConsolePrinter.PrintHeader("MAIN FLOW");
			Console.WriteLine("1. Login");
			Console.WriteLine("2. Exit");

			int choice = ConsolePrinter.ReadInt("\nChoose an option: ");
			switch (choice)
			{
				case 1:
					HandleLogin();
					break;
				case 2:
					exit = true;
					Console.WriteLine("Exiting application...");
					break;
				default:
					Console.WriteLine("Invalid option. Try again.");
					break;
			}
		}
	}

	private void HandleLogin()
	{
		try
		{
			string email = ConsolePrinter.ReadString("Email: ");
			string password = ConsolePrinter.ReadString("Password: ");

			_currentUser = _memberService.Login(email, password);
			ConsolePrinter.WriteSuccess($"Welcome {_currentUser!.Username}!");

			if (_currentUser.Role == UserRole.Admin)
				ShowAdminMenu();
			else
				ShowMemberMenu();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Login failed: {ex.Message}");
		}
	}

	private void ShowAdminMenu()
	{
		bool logout = false;

		while (!logout)
		{
			Console.WriteLine();
			ConsolePrinter.PrintHeader("ADMIN MENU");
			Console.WriteLine("1. Add Member");
			Console.WriteLine("2. View Members");
			Console.WriteLine("3. Add Category");
			Console.WriteLine("4. Add Book");
			Console.WriteLine("5. Add Book Copies");
			Console.WriteLine("6. View All Books");
			Console.WriteLine("7. Search Books");
			Console.WriteLine("8. Update Book Copy Status");
			Console.WriteLine("9. View Overdue Borrowings");
			Console.WriteLine("10. Logout");

			int choice = ConsolePrinter.ReadInt("\nChoose an option: ");

			try
			{
				switch (choice)
				{
					case 1:
						AddMember();
						break;
					case 2:
						ViewMembers();
						break;
					case 3:
						AddCategory();
						break;
					case 4:
						AddBook();
						break;
					case 5:
						AddBookCopies();
						break;
					case 6:
						ViewAllBooks();
						break;
					case 7:
						SearchBooks();
						break;
					case 8:
						UpdateBookCopyStatus();
						break;
					case 9:
						ViewOverdueBorrowings();
						break;
					case 10:
						logout = true;
						_currentUser = null;
						ConsolePrinter.WriteSuccess("Logged out.");
						break;
					default:
						Console.WriteLine("Invalid option. Try again.");
						break;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Operation failed: {ex.Message}");
			}
		}
	}

	private void ShowMemberMenu()
	{
		bool logout = false;

		while (!logout)
		{
			Console.WriteLine();
			ConsolePrinter.PrintHeader("MEMBER MENU");
			Console.WriteLine("1. Search Books");
			Console.WriteLine("2. Borrow Book");
			Console.WriteLine("3. Return Book");
			Console.WriteLine("4. View All Books");
			Console.WriteLine("5. History");
			Console.WriteLine("6. Pay Fine");
			Console.WriteLine("7. Logout");

			int choice = ConsolePrinter.ReadInt("\nChoose an option: ");

			try
			{
				switch (choice)
				{
					case 1:
						SearchBooks();
						break;
					case 2:
						BorrowBook();
						break;
					case 3:
						ReturnBook();
						break;
					case 4:
						ViewAllBooks();
						break;
					case 5:
						ShowHistoryMenu();
						break;
					case 6:
						PayFine();
						break;
					case 7:
						logout = true;
						_currentUser = null;
						ConsolePrinter.WriteSuccess("Logged out.");
						break;
					default:
						Console.WriteLine("Invalid option. Try again.");
						break;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Operation failed: {ex.Message}");
			}
		}
	}

	private void AddMember()
	{
		var membershipId = SelectMembershipType();

		var member = new Member
		{
			Username = ConsolePrinter.ReadString("Username: "),
			Password = ConsolePrinter.ReadString("Password: "),
			Name = ConsolePrinter.ReadString("Name: "),
			Email = ConsolePrinter.ReadString("Email: "),
			PhoneNo = ConsolePrinter.ReadString("Phone Number: "),
			MembershipId = membershipId,
			Role = ReadRole("Role (Admin/User): "),
			Status = MemberStatus.Active
		};

		_memberService.AddMember(member);
		Console.WriteLine();
		ConsolePrinter.WriteSuccess("Member added successfully.");
	}

	private void AddCategory()
	{
		Console.WriteLine();
		ConsolePrinter.PrintSection("Add Category");

		string categoryName = ConsolePrinter.ReadString("Category name: ");
		if (string.IsNullOrWhiteSpace(categoryName))
			throw new InvalidArgumentException("Category name cannot be empty.");

		if (_categoryRepository.GetCategoryByName(categoryName) is not null)
			throw new InvalidArgumentException("Category already exists.");

		_categoryRepository.Add(new Category { CategoryName = categoryName.Trim() });
		Console.WriteLine();
		ConsolePrinter.WriteSuccess("Category added successfully.");
	}

	private void ViewMembers()
	{
		var members = _memberService.GetAllMembers();
		if (members.Count == 0)
		{
			Console.WriteLine();
			ConsolePrinter.WriteInfo("No members found.");
			return;
		}

		Console.WriteLine();
		ConsolePrinter.PrintSection("Members");
		foreach (var member in members)
		{
			ConsolePrinter.WriteInfo($"Id: {member.Id} | Username: {member.Username} | Email: {member.Email} | Role: {member.Role} | Status: {member.Status}");
		}
	}

	private void AddBook()
	{
		int categoryId = SelectCategory();

		var book = new Book
		{
			ISBN = ConsolePrinter.ReadString("ISBN: "),
			Title = ConsolePrinter.ReadString("Title: "),
			Author = ConsolePrinter.ReadString("Author: "),
			CategoryId = categoryId
		};

		_bookService.AddBook(book);
		Console.WriteLine();
		ConsolePrinter.WriteSuccess("Book added successfully.");
	}

	private void AddBookCopies()
	{
		int bookId = ConsolePrinter.ReadInt("Book Id: ");
		int count = ConsolePrinter.ReadInt("Number of copies: ");

		_bookService.AddBookCopies(bookId, count);
		Console.WriteLine();
		ConsolePrinter.WriteSuccess("Book copies added successfully.");
	}

	private void ViewAllBooks()
	{
		var books = _bookService.GetAllBooks();
		if (books.Count == 0)
		{
			ConsolePrinter.WriteWarning("No books found.");
			return;
		}

		ConsolePrinter.PrintSection("All Books");
		foreach (var book in books)
		{
			var availableCount = _bookService.GetAvailableCopies(book.Id).Count;
			ConsolePrinter.WriteInfo($"Id: {book.Id} | Title: {book.Title} | Author: {book.Author} | ISBN: {book.ISBN} | Category Id: {book.CategoryId} | Available Copies: {availableCount}");
		}
	}

	private void SearchBooks()
	{
		string title = ConsolePrinter.ReadOptionalString("Title (optional): ");
		string author = ConsolePrinter.ReadOptionalString("Author (optional): ");

		int? categoryId = null;
		string categoryInput = ConsolePrinter.ReadOptionalString("Category Id (optional): ");
		if (!string.IsNullOrWhiteSpace(categoryInput))
		{
			if (!int.TryParse(categoryInput, out int parsedCategoryId))
				throw new InvalidArgumentException("Category Id must be a valid number.");

			categoryId = parsedCategoryId;
		}

		var books = _bookService.SearchBook(
			string.IsNullOrWhiteSpace(title) ? null : title,
			string.IsNullOrWhiteSpace(author) ? null : author,
			categoryId);

		if (books.Count == 0)
		{
			Console.WriteLine();
			ConsolePrinter.WriteWarning("No matching books found.");
			return;
		}

		ConsolePrinter.PrintSection("Books");
		foreach (var book in books)
		{
			var availableCount = _bookService.GetAvailableCopies(book.Id).Count;
			ConsolePrinter.WriteInfo($"Id: {book.Id} | Title: {book.Title} | Author: {book.Author} | ISBN: {book.ISBN} | Available Copies: {availableCount}");
		}
	}

	private void UpdateBookCopyStatus()
	{
		int copyId = ConsolePrinter.ReadInt("Copy Id: ");
		BookCopyStatus status = ReadBookCopyStatus("Status (Available/Borrowed/Damaged/Unavailable): ");

		_bookService.UpdateBookStatus(copyId, status);
		Console.WriteLine();
		ConsolePrinter.WriteSuccess("Book copy status updated successfully.");
	}

	private void ViewOverdueBorrowings()
	{
		var overdues = _borrowService.GetOverdueBorrowings();
		if (overdues.Count == 0)
		{
			Console.WriteLine();
			ConsolePrinter.WriteWarning("No overdue borrowings.");
			return;
		}

		ConsolePrinter.PrintSection("Overdue Borrowings");
		foreach (var borrow in overdues)
		{
			ConsolePrinter.WriteInfo($"Borrow Id: {borrow.Id} | User Id: {borrow.UserId} | Book Id: {borrow.BookId} | Due Date: {borrow.DueDate:d} | Status: {borrow.Status}");
		}
	}

	private void BorrowBook()
	{
		EnsureLoggedInUser();
		int bookId = ConsolePrinter.ReadInt("Book Id: ");

		var borrow = _borrowService.BorrowBook(_currentUser!.Id, bookId);
		Console.WriteLine();
		ConsolePrinter.WriteSuccess($"Book borrowed successfully. Borrow Id: {borrow.Id}, Due Date: {borrow.DueDate:d}");
	}

	private void ReturnBook()
	{
		EnsureLoggedInUser();
		var activeBorrowings = _borrowService.GetActiveBorrowings(_currentUser!.Id);
		if (activeBorrowings.Count == 0)
		{
			Console.WriteLine();
			ConsolePrinter.WriteWarning("You have no active borrowings.");
			return;
		}

		ConsolePrinter.PrintSection("Active Borrowings");
		foreach (var borrow in activeBorrowings)
		{
			ConsolePrinter.WriteInfo($"Borrow Id: {borrow.Id} | Book Id: {borrow.BookId} | Due Date: {borrow.DueDate:d}");
		}

		int borrowId = ConsolePrinter.ReadInt("Borrow Id to return: ");
		if (!activeBorrowings.Any(b => b.Id == borrowId))
			throw new InvalidArgumentException("Invalid borrow Id for current user.");

		decimal fineAmount = _borrowService.ReturnBook(borrowId);
		Console.WriteLine();
		ConsolePrinter.WriteSuccess("Book returned successfully.");
		if (fineAmount > 0m)
			ConsolePrinter.WriteWarning($"This return is overdue. Fine to be paid: Rs. {fineAmount:0.00}");
	}

	private void ShowHistoryMenu()
	{
		EnsureLoggedInUser();
		bool back = false;

		while (!back)
		{
			Console.WriteLine();
			ConsolePrinter.PrintHeader("HISTORY");
			Console.WriteLine("1. Borrow history");
			Console.WriteLine("2. Fine history");
			Console.WriteLine("3. Back");

			int choice = ConsolePrinter.ReadInt("\nChoose an option: ");

			switch (choice)
			{
				case 1:
					ViewBorrowHistory();
					break;
				case 2:
					ViewFineHistory();
					break;
				case 3:
					back = true;
					break;
				default:
					Console.WriteLine("Invalid option. Try again.");
					break;
			}
		}
	}

	private void ViewBorrowHistory()
	{
		var borrowings = _borrowService.GetBorrowHistory(_currentUser!.Id);
		if (borrowings.Count == 0)
		{
			Console.WriteLine();
			ConsolePrinter.WriteWarning("You have no borrow history.");
			return;
		}

		ConsolePrinter.PrintSection("Borrow History");
		foreach (var borrow in borrowings)
		{
			ConsolePrinter.WriteInfo($"Borrow Id: {borrow.Id} | Book Id: {borrow.BookId} | Borrowed On: {borrow.BorrowDate:d} | Due Date: {borrow.DueDate:d} | Returned On: {(borrow.ReturnDate.HasValue ? borrow.ReturnDate.Value.ToString("d") : "N/A")} | Status: {borrow.Status}");
		}
	}

	private void ViewFineHistory()
	{
		var fines = _borrowService.GetFineHistory(_currentUser!.Id);
		if (fines.Count == 0)
		{
			Console.WriteLine();
			ConsolePrinter.WriteWarning("You have no fine history.");
			return;
		}

		ConsolePrinter.PrintSection("Fine History");
		for (int i = 0; i < fines.Count; i++)
		{
			var fine = fines[i];
			ConsolePrinter.WriteInfo($"{i + 1}. Fine Id: {fine.Id} | Borrow Id: {fine.BorrowId} | Amount: Rs. {fine.Amount:0.00} | Status: {(fine.IsPaid ? "Paid" : "Unpaid")} | Created At: {fine.CreatedAt:d}");
		}
	}

	private void PayFine()
	{
		EnsureLoggedInUser();
		var fines = _borrowService.GetFineHistory(_currentUser!.Id)
			.Where(f => !f.IsPaid)
			.ToList();

		if (fines.Count == 0)
		{
			Console.WriteLine();
			ConsolePrinter.WriteWarning("You have no unpaid fines.");
			return;
		}

		ConsolePrinter.PrintSection("Unpaid Fines");
		for (int i = 0; i < fines.Count; i++)
		{
			var fine = fines[i];
			ConsolePrinter.WriteInfo($"{i + 1}. Fine Id: {fine.Id} | Borrow Id: {fine.BorrowId} | Amount: Rs. {fine.Amount:0.00} | Created At: {fine.CreatedAt:d}");
		}

		int fineIndex = ConsolePrinter.ReadInt("Select fine index to pay: ");
		if (fineIndex < 1 || fineIndex > fines.Count)
			throw new InvalidArgumentException("Invalid fine index.");

		var selectedFine = fines[fineIndex - 1];
		_borrowService.PayFine(_currentUser!.Id, selectedFine.Id);
		Console.WriteLine();
		ConsolePrinter.WriteSuccess($"Fine paid successfully. Fine Id: {selectedFine.Id}");
	}

	private int SelectCategory()
	{
		var categories = _categoryRepository.GetAllCategories();
		if (categories.Count == 0)
			throw new InvalidArgumentException("No categories found. Add a category first.");

		ConsolePrinter.PrintSection("Categories");
		for (int i = 0; i < categories.Count; i++)
		{
			ConsolePrinter.WriteInfo($"{i + 1}. {categories[i].CategoryName}");
		}

		int choice = ConsolePrinter.ReadInt("Select category by number: ");
		if (choice < 1 || choice > categories.Count)
			throw new InvalidArgumentException("Invalid category selection.");

		return categories[choice - 1].Id;
	}

	private int SelectMembershipType()
	{
		var memberships = _membershipRepository.GetAll();
		if (memberships.Count == 0)
			throw new InvalidArgumentException("No membership types found.");

		ConsolePrinter.PrintSection("Membership Types");
		for (int i = 0; i < memberships.Count; i++)
		{
			ConsolePrinter.WriteInfo($"{i + 1}. {memberships[i].Type} | Max Books: {memberships[i].MaxBrwBooks} | Max Days: {memberships[i].MaxBrwDays}");
		}

		int choice = ConsolePrinter.ReadInt("Select membership type by number: ");
		if (choice < 1 || choice > memberships.Count)
			throw new InvalidArgumentException("Invalid membership selection.");

		return memberships[choice - 1].Id;
	}

	

	private static UserRole ReadRole(string prompt)
	{
		while (true)
		{
			Console.Write(prompt);
			string? input = Console.ReadLine();
			if (Enum.TryParse<UserRole>(input, true, out var role))
				return role;

			Console.WriteLine("Invalid role. Enter Admin or User.");
		}
	}

	private static BookCopyStatus ReadBookCopyStatus(string prompt)
	{
		while (true)
		{
			Console.Write(prompt);
			string? input = Console.ReadLine();

			if (Enum.TryParse<BookCopyStatus>(input, true, out var status))
				return status;

			if (int.TryParse(input, out int numericStatus) && Enum.IsDefined(typeof(BookCopyStatus), numericStatus))
				return (BookCopyStatus)numericStatus;

			Console.WriteLine("Invalid status. Enter Available, Borrowed, Damaged, or Unavailable.");
		}
	}

	private void EnsureLoggedInUser()
	{
		if (_currentUser is null)
			throw new InvalidOperationException("No logged in user found.");
	}

}
