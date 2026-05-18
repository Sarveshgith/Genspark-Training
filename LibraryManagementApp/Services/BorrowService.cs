using System;
using System.Linq;
using System.Collections.Generic;
using LibraryManagementApp.Models;
using LibraryManagementApp.Models.Exceptions;
using LibraryManagementApp.Interfaces;
using LibraryManagementApp.Utils;
using LibraryManagementApp.Enums;
using LibraryManagementApp.Contexts;
using Serilog;

namespace LibraryManagementApp.Services;

internal class BorrowService
{
	private readonly IBorrowRepository _borrowRepo;
	private readonly IMemberRepository _memberRepo;
	private readonly IBookRepository _bookRepo;
	private readonly IBookCopyRepository _copyRepo;
	private readonly IMembershipRepository _membershipRepo;
	private readonly IFineRepository _fineRepo;
    private readonly LibraryDbContext _context;

	public BorrowService(LibraryDbContext context, IBorrowRepository borrowRepo, IMemberRepository memberRepo, IBookRepository bookRepo, IBookCopyRepository copyRepo, IMembershipRepository membershipRepo, IFineRepository fineRepo)
	{
        _context = context;
		_borrowRepo = borrowRepo;
		_memberRepo = memberRepo;
		_bookRepo = bookRepo;
		_copyRepo = copyRepo;
		_membershipRepo = membershipRepo;
		_fineRepo = fineRepo;
	}

	public Borrow BorrowBook(int userId, int bookId)
	{
        using var transaction = _context.Database.BeginTransaction();

        try{
            //Validate Member
            if (userId <= 0)
                throw new InvalidArgumentException("Invalid user ID. It must be a positive number.");

            var user = _memberRepo.Get(userId);
			if (user is null)
				throw new NotFoundException("User not found.");

			if (user.Status != MemberStatus.Active)
				throw new BusinessRuleException("User is not active.");

			var membership = _membershipRepo.Get(user.MembershipId);
			if (membership is null)
				throw new NotFoundException("Membership not found for this user.");

            //Validate Book
            if (bookId <= 0)
                throw new InvalidArgumentException("Invalid book ID. It must be a positive number.");

			var book = _bookRepo.Get(bookId);
			if (book is null)
				throw new NotFoundException("Book not found.");

            //Validate Unpaid Fines
            decimal pendingFine = _fineRepo.GetTotalPendingFine(userId);
			if (pendingFine > 500m)
				throw new BusinessRuleException("Cannot borrow books while unpaid fines are above Rs. 500.");

            //Check active borrowing count
            var activeBorrowings = _borrowRepo.GetActiveBorrowings(userId).Count;
			if(activeBorrowings >= membership.MaxBrwBooks)
				throw new BusinessRuleException($"Borrowing limit reached for {membership.Type} membership.");

            //Check duplicate borrowing for same book
			if (_borrowRepo.HasActiveBorrowing(userId, bookId))
				throw new BusinessRuleException("User already has an active borrowing for this book.");

            //Get available copy
			var copy = _copyRepo.GetAvailableCopy(bookId);
			if (copy is null)
				throw new BusinessRuleException("No available copies.");

            //Create borrow record
            var borrow = new Borrow
            {
                UserId = userId,
                BookId = bookId,
                BookCopyId = copy.Id,
                BorrowDate = DateTime.UtcNow,
				DueDate = DateTime.UtcNow.AddDays(membership.MaxBrwDays),
                Status = BorrowStatus.Borrowed
            };
            
            copy.Status = BookCopyStatus.Borrowed;
            _context.BookCopies.Update(copy);

            _context.Borrows.Add(borrow);
            _context.SaveChanges();

            transaction.Commit();
			Log.Information("Borrow created successfully. BorrowId={BorrowId}, UserId={UserId}, BookId={BookId}, CopyId={CopyId}, DueDate={DueDate}", borrow.Id, userId, bookId, copy.Id, borrow.DueDate);
            return borrow;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
	}

	public decimal ReturnBook(int borrowId)
	{
		if (borrowId <= 0)
			throw new InvalidArgumentException("Invalid borrow ID. It must be a positive number.");

		var borrow = _borrowRepo.Get(borrowId);
		if (borrow is null)
			throw new NotFoundException("Borrow record not found.");

		if (borrow.Status == BorrowStatus.Returned)
			throw new BusinessRuleException("Book already returned.");

		decimal fineAmount = 0m;
		borrow.ReturnDate = DateTime.UtcNow;

		if (borrow.ReturnDate > borrow.DueDate)
		{
			int daysOver = (borrow.ReturnDate.Value - borrow.DueDate).Days;
			if (daysOver > 0)
			{
				fineAmount = daysOver * 10.0m;
				var fine = new Fine
				{
					UserId = borrow.UserId,
					BorrowId = borrow.Id,
					Amount = fineAmount,
					IsPaid = false
				};

				_fineRepo.Add(fine);
			}
		}

		borrow.Status = BorrowStatus.Returned;
		_borrowRepo.Update(borrow.Id, borrow);
		_copyRepo.UpdateStatus(borrow.BookCopyId, BookCopyStatus.Available);
		Log.Information("Borrow returned successfully. BorrowId={BorrowId}, UserId={UserId}, FineAmount={FineAmount}", borrow.Id, borrow.UserId, fineAmount);
		return fineAmount;
	}

	public List<Borrow> GetActiveBorrowings(int userId)
	{
		if (userId <= 0)
			throw new InvalidArgumentException("Invalid user ID. It must be a positive number.");

		var user = _memberRepo.Get(userId);
		if (user is null)
			throw new NotFoundException("User not found.");

		var activeBorrowings = _borrowRepo.GetActiveBorrowings(userId);
		Log.Information("Fetched active borrowings. UserId={UserId}, Count={Count}", userId, activeBorrowings.Count);
		return activeBorrowings;
	}

	public List<Borrow> GetBorrowHistory(int userId)
	{
		if (userId <= 0)
			throw new InvalidArgumentException("Invalid user ID. It must be a positive number.");

		var user = _memberRepo.Get(userId);
		if (user is null)
			throw new NotFoundException("User not found.");

		var history = _borrowRepo.GetBorrowHistory(userId);
		Log.Information("Fetched borrow history. UserId={UserId}, Count={Count}", userId, history.Count);
		return history;
	}

	public List<Fine> GetFineHistory(int userId)
	{
		if (userId <= 0)
			throw new InvalidArgumentException("Invalid user ID. It must be a positive number.");

		var user = _memberRepo.Get(userId);
		if (user is null)
			throw new NotFoundException("User not found.");

		var fines = _fineRepo.GetFineHistory(userId);
		Log.Information("Fetched fine history. UserId={UserId}, Count={Count}", userId, fines.Count);
		return fines;
	}

	public void PayFine(int userId, int fineId)
	{
		if (userId <= 0)
			throw new InvalidArgumentException("Invalid user ID. It must be a positive number.");

		if (fineId <= 0)
			throw new InvalidArgumentException("Invalid fine ID. It must be a positive number.");

		var user = _memberRepo.Get(userId);
		if (user is null)
			throw new NotFoundException("User not found.");

		var fine = _fineRepo.Get(fineId);
		if (fine is null)
			throw new NotFoundException("Fine record not found.");

		if (fine.UserId != userId)
			throw new UnauthorizedException("You can only pay your own fines.");

		if (fine.IsPaid)
			throw new BusinessRuleException("Fine is already paid.");

		_fineRepo.PayFine(fineId);
		Log.Information("Fine paid successfully. UserId={UserId}, FineId={FineId}, Amount={Amount}", userId, fineId, fine.Amount);
	}

	public List<Borrow> GetOverdueBorrowings()
	{
		var overdueBorrowings = _borrowRepo.GetOverdueBorrowings();
		Log.Information("Fetched overdue borrowings. Count={Count}", overdueBorrowings.Count);
		return overdueBorrowings;
	}

	public List<Borrow> GetCurrentBorrowings()
	{
		var activeBorrowings = _borrowRepo.GetAllActiveBorrowings();

		Log.Information("Fetched all current borrowings. Count={Count}", activeBorrowings.Count);
		return activeBorrowings;
	}

	public List<(int BookId, string Title, int BorrowCount)> GetMostBorrowedBooks(int topN = 5)
	{
		if (topN <= 0)
			throw new InvalidArgumentException("Top N must be a positive number.");

		var result = _borrowRepo
			.GetAll()
			.GroupBy(b => new {b.BookId, b.Book.Title})
			.Select(g => (BookId: g.Key.BookId, Title: g.Key.Title, BorrowCount: g.Count()))
			.OrderByDescending(x => x.BorrowCount)
			.ThenBy(x => x.BookId)
			.Take(topN)
			.ToList();

		Log.Information("Fetched most borrowed books. TopN={TopN}, ResultCount={Count}", topN, result.Count);
		return result;
	}
}
