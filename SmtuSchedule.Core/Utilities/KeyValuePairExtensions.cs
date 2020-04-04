using System.Collections.Generic;

namespace SmtuSchedule.Core.Utilities
{
    internal static class KeyValuePairExtensions
    {
        public static void Deconstruct<TKey, TValue>(this in KeyValuePair<TKey, TValue> tuple,
            out TKey key, out TValue value)
        {
            key = tuple.Key;
            value = tuple.Value;
        }
    }
}