using System.Collections.Generic;

namespace MultiTierArchi_NotifApp.Repositories;

internal abstract class Repository<T>
{
    protected readonly List<T> _items = new List<T>();

    public void Create(T obj)
    {
        _items.Add(obj);
    }

    public abstract List<T> GetAll();
}
