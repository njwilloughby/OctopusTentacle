﻿using System;
using System.Linq;
using Octopus.Shared.Util;

namespace Octopus.Shared.Diagnostics.KnowledgeBase
{
    public class ExceptionKnowledgeBaseEntry
    {
        public ExceptionKnowledgeBaseEntry(string summary, string? helpText, string? helpLink)
        {
            HelpLink = helpLink;
            HelpText = helpText;
            Summary = summary;
        }

        public string Summary { get; }
        public string? HelpText { get; }
        public string? HelpLink { get; }

        public override string ToString()
        {
            var parts = new[] { Summary, HelpText ?? string.Empty }.NotNullOrWhiteSpace().ToList();
            if (HelpLink != null)
                parts.Add("See: " + HelpLink);

            return string.Join(" ", parts);
        }
    }
}