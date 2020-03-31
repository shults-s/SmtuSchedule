using System.Collections.Generic;

namespace SmtuSchedule.Core.Utilities
{
    internal static class KeyValuePairExtensions
    {
        public static void Deconstruct<T1, T2>(this in KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
        {
            key = tuple.Key;
            value = tuple.Value;
        }
    }
}