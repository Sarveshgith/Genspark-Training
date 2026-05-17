using System;

namespace LibraryManagementApp.Interfaces;

internal interface IRepository<K, T> where T : class where K : notnull
{
    public void Add(T entity);
    public T? Get(K id);
    public List<T> GetAll();
    public void Update(K id, T entity);
    public void Delete(K id);
}
