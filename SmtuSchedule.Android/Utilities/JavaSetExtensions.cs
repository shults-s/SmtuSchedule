using System;
using System.Collections.Generic;
using Android.Runtime;

namespace SmtuSchedule.Android.Utilities
{
    internal static class JavaSetExtensions
    {
        public static IEnumerable<T> ToIEnumerable<T>(this JavaSet set) where T : class
        {
            foreach (Object item in set)
            {
                yield return item as T;
            }
        }
    }
}