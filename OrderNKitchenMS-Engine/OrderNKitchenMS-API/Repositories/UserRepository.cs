using System;
using Microsoft.EntityFrameworkCore;
using OrderNKitchenMS_API.Data;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Repositories.Interfaces;
using OrderNKitchenMS_API.Exceptions;

namespace OrderNKitchenMS_API.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<User> _users;
    private readonly DbSet<Role> _roles;

    public UserRepository(AppDbContext context)
    {
        _context = context;
        _users = _context.Users;
        _roles = _context.Roles;
    }

    public Task<IQueryable<User>> GetAll()
    {
        return Task.FromResult(_users
            .Include(user => user.Role)
            .Where(user => !user.IsDeleted));
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _users
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.Id == id && !user.IsDeleted);
    }

    public async Task<User> CreateAsync(User user)
    {
        user.Email = user.Email.ToLower();
        _users.Add(user);
        await _context.SaveChangesAsync();
        await _context.Entry(user).Reference(createdUser => createdUser.Role).LoadAsync();
        return user;
    }

    public async Task<User?> UpdateAsync(int id, User user)
    {
        var existingUser = await GetByIdAsync(id);
        if (existingUser == null)
        {
            return null;
        }

        existingUser.Name = user.Name;
        existingUser.Email = user.Email.ToLower();
        existingUser.PhoneNumber = user.PhoneNumber;
        existingUser.Address = user.Address;
        await _context.SaveChangesAsync();
        return existingUser;
    }

    public async Task<User?> ApproveAsync(int id)
    {
        var user = await GetByIdAsync(id);
        if (user == null)
        {
            return null;
        }
        user.IsPending = false;
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User?> UpdateRoleAsync(int id, int roleId)
    {
        var user = await GetByIdAsync(id);
        if (user == null)
        {
            return null;
        }

        var roleExists = await _roles.AnyAsync(r => r.Id == roleId);
        if (!roleExists)
        {
            throw new NotFoundException($"Role with ID {roleId} does not exist.");
        }

        user.RoleId = roleId;
        await _context.SaveChangesAsync();
        await _context.Entry(user).Reference(u => u.Role).LoadAsync();
        return user;
    }

    public async Task<IEnumerable<Role>> GetAllRolesAsync()
    {
        return await _roles
            .OrderBy(role => role.Id)
            .ToListAsync();
    }

    public async Task<bool> ChangePasswordAsync(int id, string hashedPassword)
    {
        var user = await GetByIdAsync(id);
        if (user == null)
        {
            return false;
        }

        user.PasswordHash = hashedPassword;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _users
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.Email == email.ToLower() && !user.IsDeleted);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await GetByIdAsync(id);
        if (user == null)
        {
            return false;
        }

        user.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }
}
