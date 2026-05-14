using NotificationApp.Models;

namespace NotificationApp.Interfaces;

internal interface IRepository<K, T> where K : notnull where T : class
{
    T Create(T item);
    T? Get(K id);
    List<T> GetAll();
    T? Update(K id, T item);
    T? Delete(K id);
}