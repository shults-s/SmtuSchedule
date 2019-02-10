using System;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace SmtuSchedule.Core.Utilities
{
    internal class UrlComparer : IEqualityComparer<HtmlNode>
    {
        public Int32 GetHashCode(HtmlNode node) => node.GetHashCode();

        public Boolean Equals(HtmlNode node1, HtmlNode node2)
        {
            return node1.Attributes["href"].Value == node2.Attributes["href"].Value;
        }
    }
}