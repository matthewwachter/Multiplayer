using System;
using System.Collections.Generic;
using Verse;

namespace Multiplayer.Client
{
    public static class CollectionExtensions
    {
        public static int RemoveAll<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, Func<TKey, TValue, bool> predicate)
        {
            List<TKey> list = null;
            try
            {
                foreach (var (key, value) in dictionary)
                {
                    if (predicate(key, value))
                    {
                        list ??= SimplePool<List<TKey>>.Get();
                        list.Add(key);
                    }
                }

                if (list == null) return 0;
                foreach (var key in list)
                {
                    dictionary.Remove(key);
                }
                return list.Count;
            }
            finally
            {
                if (list != null)
                {
                    list.Clear();
                    SimplePool<List<TKey>>.Return(list);
                }
            }
        }
    }
}
