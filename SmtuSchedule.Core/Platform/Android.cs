using System;

namespace Android.Runtime
{
    internal sealed class PreserveAttribute : System.Attribute
    {
        public Boolean AllMembers;
        public Boolean Conditional;
    }
}