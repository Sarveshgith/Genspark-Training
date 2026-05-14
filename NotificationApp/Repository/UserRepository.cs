using NotificationApp.Contexts;
using NotificationApp.Interfaces;
using NotificationApp.Models;
using Microsoft.EntityFrameworkCore;

namespace NotificationApp.Repository;

internal class UserRepository : IUserRepository
{
    private readonly NotifContext _context;

    public UserRepository(NotifContext context = null)
    {
        _context = context ?? new NotifContext();
    }

    public User Create(User item)
    {
        _context.Users.Add(item);
        _context.SaveChanges();
        return item;
    }

    public User? Get(int id)
    {
        return _context.Users.FirstOrDefault(u => u.Id == id);
    }

    public User? GetByEmail(string email)
    {
        return _context.Users.FirstOrDefault(u => u.Email == email);
    }

    public User? GetByPhone(string phoneNo)
    {
        return _context.Users.FirstOrDefault(u => u.PhoneNo == phoneNo);
    }

    public List<User> GetAll()
    {
        return _context.Users
            .OrderBy(u => u.Name)
            .ThenBy(u => u.Email)
            .ToList();
    }

    public User? Update(int id, User item)
    {
        var existing = _context.Users.FirstOrDefault(u => u.Id == id);
        if (existing == null)
        {
            return null;
        }

        existing.Name = item.Name;
        existing.Email = item.Email;
        existing.PhoneNo = item.PhoneNo;
        _context.SaveChanges();
        return existing;
    }

    public User? Delete(int id)
    {
        var user = _context.Users.FirstOrDefault(u => u.Id == id);
        if (user == null)
        {
            return null;
        }

        _context.Users.Remove(user);
        _context.SaveChanges();
        return user;
    }
}