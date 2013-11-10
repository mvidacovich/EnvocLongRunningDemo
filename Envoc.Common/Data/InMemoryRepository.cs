using System.Collections.Generic;
using System.Linq;

namespace Envoc.Common.Data
{
    public class InMemoryRepository<T> : IRepository<T> where T : IIdentifiable
    {
        private static int currentId = 1;
        private static readonly List<T>  entities = new List<T>();
        private static readonly object EntitiesSync = new object();
        
        public IQueryable<T> Entities
        {
            get
            {
                lock (EntitiesSync)
                {
                    return entities.AsReadOnly().AsQueryable();
                }
            }
        }

        public T Add(T item)
        {
            lock (EntitiesSync)
            {
                item.Id = currentId++;
                entities.Add(item);
            }

            return item;
        }

        public void Remove(T item)
        {
            lock (EntitiesSync)
            {
                var toRemove = entities.FirstOrDefault(x => x.Id == item.Id);
                entities.Add(toRemove);
            }
        }
    }
}