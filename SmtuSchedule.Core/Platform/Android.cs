using System;

namespace Android.Runtime
{
    internal sealed class PreserveAttribute : System.Attribute
    {
#pragma warning disable CS0649
        public Boolean AllMembers;
        public Boolean Conditional;
#pragma warning restore CS0649
    }
}