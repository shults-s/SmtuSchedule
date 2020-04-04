using System;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace SmtuSchedule.Core.Utilities
{
    internal sealed class HrefsEqualityComparer : IEqualityComparer<HtmlNode>
    {
        public Int32 GetHashCode(HtmlNode link)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            return link.GetHashCode();
        }

        public Boolean Equals(HtmlNode link1, HtmlNode link2)
        {
            if (link1 == null || link2 == null)
            {
                return false;
            }

            if (link1 == link2)
            {
                return true;
            }

            String? href1 = link1.Attributes["href"]?.Value;
            String? href2 = link2.Attributes["href"]?.Value;
            return (href1 == null || href2 == null) ? false : (href1 == href2);
        }
    }
}