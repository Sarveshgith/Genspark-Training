using System;
using OrderNKitchenMS_API.Models.Entities;

namespace OrderNKitchenMS_API.Repositories.Interfaces;

public interface IUserRepository
{
    public Task<IQueryable<User>> GetAll();

    public Task<User?> GetByIdAsync(int id);

    public Task<User> CreateAsync(User user);

    public Task<User?> UpdateAsync(int id, User user);

    public Task<IEnumerable<Role>> GetAllRolesAsync();

    public Task<bool> ChangePasswordAsync(int id, string hashedPassword);

    public Task<User?> GetByEmailAsync(string email);

    public Task<bool> DeleteAsync(int id);
}
