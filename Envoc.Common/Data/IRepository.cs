using System.Linq;

namespace Envoc.Common.Data
{
    public interface IRepository<T>
    {
        IQueryable<T> Entities { get; }
        T Add(T item);
        void Remove(T item);
    }
}