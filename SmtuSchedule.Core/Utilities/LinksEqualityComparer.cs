using System;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace SmtuSchedule.Core.Utilities
{
    internal sealed class LinksEqualityComparer : IEqualityComparer<HtmlNode>
    {
        public Int32 GetHashCode(HtmlNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return node.GetHashCode();
        }

        public Boolean Equals(HtmlNode node1, HtmlNode node2)
        {
            if (node1 == null || node2 == null)
            {
                return false;
            }

            if (node1 == node2)
            {
                return true;
            }

            String? value1 = node1.Attributes["href"]?.Value;
            String? value2 = node2.Attributes["href"]?.Value;
            return (value1 == null || value2 == null) ? false : (value1 == value2);
        }
    }
}