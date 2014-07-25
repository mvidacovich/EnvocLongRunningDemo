using System.Collections;
using System.Collections.Generic;

namespace Envoc.AzureLongRunningTask.Web.Models
{
    public class StaticList<T> : IList<T>
    {
        private static readonly List<T> Content = new List<T>();

        public IEnumerator<T> GetEnumerator()
        {
            return Content.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) Content).GetEnumerator();
        }

        public void Add(T item)
        {
            Content.Add(item);
        }

        public void Clear()
        {
            Content.Clear();
        }

        public bool Contains(T item)
        {
            return Content.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Content.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return Content.Remove(item);
        }

        public int Count
        {
            get { return Content.Count; }
        }

        public bool IsReadOnly
        {
            get { return ((IList<T>)Content).IsReadOnly; }
        }

        public int IndexOf(T item)
        {
            return Content.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            Content.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            Content.RemoveAt(index);
        }

        public T this[int index]
        {
            get { return Content[index]; }
            set { Content[index] = value; }
        }
    }
}