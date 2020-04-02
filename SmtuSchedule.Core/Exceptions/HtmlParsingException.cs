using System;
using HtmlAgilityPack;

namespace SmtuSchedule.Core.Exceptions
{
    internal sealed class HtmlParsingException : Exception
    {
        public override String Message =>
            (_node == null) ? base.Message : base.Message + " " + GetHtmlNodeInformation();

        public HtmlParsingException(String message, Exception innerException, HtmlNode node)
            : base(message, innerException)
        {
            _node = node ?? throw new ArgumentNullException(nameof(node));
        }

        public HtmlParsingException(String message, HtmlNode node) : base(message)
        {
            _node = node ?? throw new ArgumentNullException(nameof(node));
        }

        public HtmlParsingException(String message) : base(message)
        {
        }

        private String GetHtmlNodeInformation()
        {
            return $"(Line: {_node!.Line}, Position: {_node.LinePosition}, Name: {_node.Name})";
        }

        private readonly HtmlNode? _node;
    }
}