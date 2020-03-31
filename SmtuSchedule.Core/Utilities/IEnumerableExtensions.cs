using System;
using System.Collections.Generic;

namespace SmtuSchedule.Core.Utilities
{
    public static class IEnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> callback)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            foreach (T item in collection)
            {
                callback(item);
            }
        }
    }
}