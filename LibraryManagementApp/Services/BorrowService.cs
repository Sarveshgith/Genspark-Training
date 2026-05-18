using System;
using System.Linq;
using System.Collections.Generic;
using LibraryManagementApp.Models;
using LibraryManagementApp.Models.Exceptions;
using LibraryManagementApp.Repositories;
using LibraryManagementApp.Utils;
using LibraryManagementApp.Enums;
using LibraryManagementApp.Contexts;

namespace LibraryManagementApp.Services;

internal class BorrowService
{
	private readonly BorrowRepository _borrowRepo;
	private readonly MemberRepository _memberRepo;
	private readonly BookRepository _bookRepo;
	private readonly BookCopyRepository _copyRepo;
	private readonly MembershipRepository _membershipRepo;
	private readonly FineRepository _fineRepo;
    private readonly LibraryDbContext _context;

	public BorrowService(LibraryDbContext context,BorrowRepository borrowRepo, MemberRepository memberRepo, BookRepository bookRepo, BookCopyRepository copyRepo, MembershipRepository membershipRepo, FineRepository fineRepo)
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
		return fineAmount;
	}

	public List<Borrow> GetActiveBorrowings(int userId)
	{
		if (userId <= 0)
			throw new InvalidArgumentException("Invalid user ID. It must be a positive number.");

		var user = _memberRepo.Get(userId);
		if (user is null)
			throw new NotFoundException("User not found.");

		return _borrowRepo.GetActiveBorrowings(userId);
	}

	public List<Borrow> GetBorrowHistory(int userId)
	{
		if (userId <= 0)
			throw new InvalidArgumentException("Invalid user ID. It must be a positive number.");

		var user = _memberRepo.Get(userId);
		if (user is null)
			throw new NotFoundException("User not found.");

		return _borrowRepo.GetBorrowHistory(userId);
	}

	public List<Fine> GetFineHistory(int userId)
	{
		if (userId <= 0)
			throw new InvalidArgumentException("Invalid user ID. It must be a positive number.");

		var user = _memberRepo.Get(userId);
		if (user is null)
			throw new NotFoundException("User not found.");

		return _fineRepo.GetFineHistory(userId);
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
	}

	public List<Borrow> GetOverdueBorrowings()
	{
		return _borrowRepo.GetOverdueBorrowings();
	}
}
