using NotificationApp.Models;

namespace NotificationApp.Interfaces;

internal interface IRepository<K, T> where T : class
{
    public T Create(T item);
    public T? Get(K id);
    public List<T> GetAll();
    public T? Update(K id, T item);
    public T? Delete(K id);
}