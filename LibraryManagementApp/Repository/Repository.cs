using Microsoft.EntityFrameworkCore;
using LibraryManagementApp.Interfaces;
using LibraryManagementApp.Contexts;

namespace LibraryManagementApp.Repositories;

internal abstract class Repository<K, T> : IRepository<K, T>
    where T : class
    where K : notnull
{
    protected readonly LibraryDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(LibraryDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual void Add(T entity)
    {
        _dbSet.Add(entity);
        _context.SaveChanges();
    }

    public virtual T? Get(K id)
    {
        return _dbSet.Find(id);
    }

    public virtual List<T> GetAll()
    {
        return _dbSet.ToList();
    }

    public virtual void Update(K id, T entity)
    {
        _dbSet.Update(entity);
        _context.SaveChanges();
    }

    public virtual void Delete(K id)
    {
        T? entity = Get(id);

        if (entity is not null)
        {
            _dbSet.Remove(entity);
            _context.SaveChanges();
        }
    }
}