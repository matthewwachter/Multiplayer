using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Multiplayer.Common
{
    public struct Container<T>
    {
        public T Inner { get; }

        public Container(T value)
        {
            Inner = value;
        }

        public static implicit operator Container<T>(T value)
        {
            return new Container<T>(value);
        }
    }

    public class UniqueList<T> : IEnumerable<T>
    {
        private List<T> list = new List<T>();
        private HashSet<T> set = new HashSet<T>();

        public int Count => list.Count;
        public T this[int index] => list[index];

        public bool Add(T t)
        {
            if (set.Add(t))
            {
                list.Add(t);
                return true;
            }

            return false;
        }

        public T[] ToArray()
        {
            return list.ToArray();
        }

        public bool Contains(T t)
        {
            return set.Contains(t);
        }

        public int IndexOf(T t)
        {
            return list.IndexOf(t);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }
    }

    public class IdentityComparer<T> : IEqualityComparer<T>
    {
        public static IdentityComparer<T> Instance = new();

        public bool Equals(T x, T y)
        {
            return object.Equals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }

    public sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }

    public class FixedSizeQueue<T> : IEnumerable<T>
    {
        private Queue<T> q = new Queue<T>();

        public int Limit { get; set; }

        public void Enqueue(T obj)
        {
            q.Enqueue(obj);
            while (q.Count > Limit) q.Dequeue();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return q.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return q.GetEnumerator();
        }
    }
}
