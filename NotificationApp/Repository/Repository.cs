using NotificationApp.Interfaces;
using NotificationApp.Models;

namespace NotificationApp.Repository;

//Repository Class => Implementation of Interface IRepository
internal abstract class Repository<K, T> : IRepository<K, T> where K : notnull where T : class
{
    protected readonly Dictionary<K, T> _items = new();

    public abstract T Create(T item);

    public T? Get(K id)
    {
        //TryGetValue => Returns false if key not found, without throwing an exception
        return _items.TryGetValue(id, out var item) ? item : null;
    }

    //Count = 0. Handled in Client Layer => Program.cs
    public List<T> GetAll()
    {
        return _items.Values
            .OrderBy(item => item.ToString())
            .ToList();
    }

    public T? Update(K id, T item)
    {
        if (!_items.ContainsKey(id))
        {
            return null;
        }

        _items[id] = item;
        return item;
    }

    public T? Delete(K id)
    {
        if (!_items.TryGetValue(id, out var item))
        {
            return null;
        }

        //Remove returns true if item was successfully removed, false if key not found
        _items.Remove(id);
        return item;
    }
}