﻿namespace Zebble
{
    using HtmlAgilityPack;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class SpannableString
    {
        string InputText = string.Empty;
        public string Stripedtext = string.Empty;
        readonly List<SpannableStringStyle> ParsedItems = new List<SpannableStringStyle>();

        List<string> AcceptedTags => Enum.GetNames(typeof(SpannableStringTypes)).ToList();

        public SpannableString(string inputText) => InputText = inputText;

        public List<SpannableStringStyle> ParseText()
        {
            Stripedtext = StripHTML(InputText);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(InputText);

            ParseTags(htmlDocument.DocumentNode.ChildNodes);

            return ParsedItems;
        }

        Dictionary<SpannableStringParameterTypes, string> ExtractFontParameters(HtmlAttributeCollection attributes)
        {
            var result = new Dictionary<SpannableStringParameterTypes, string>();
            foreach (var attribute in attributes)
            {
                var isCorrectkey = Enum.TryParse(attribute.Name.CapitaliseFirstLetters(), out SpannableStringParameterTypes key);
                if (!isCorrectkey) continue;
                result.Add(key, attribute.Value);
            }

            return result;
        }

        SpannableStringStyle ExtractStyle(HtmlNode node, SpannableStringRange range)
        {
            var isCorrecttype = Enum.TryParse(node.Name.CapitaliseFirstLetters(), out SpannableStringTypes type);
            return new SpannableStringStyle
            {
                InnerText = node.InnerText,
                Parameters = node.Name.ToLower() == "font" ? ExtractFontParameters(node.Attributes) : null,
                Range = range,
                Type = isCorrecttype ? type : SpannableStringTypes.PlainText
            };
        }

        void ParseTags(HtmlNodeCollection children, SpannableStringStyle parentStyle = null, HtmlNode parentNode = null)
        {
            var inputText = InputText;
            if (parentNode != null)
            {
                var builder = new System.Text.StringBuilder(inputText);
                var index = inputText.IndexOf(parentNode.OuterHtml);
                builder.Remove(index, parentNode.OuterHtml.Length);
                builder.Insert(index, parentNode.InnerHtml);
                inputText = builder.ToString();
            }

            foreach (var node in children)
            {
                var startIndex = inputText.IndexOf(node.OuterHtml);

                var previousString = inputText.Substring(0, startIndex);
                if (Regex.IsMatch(previousString, "<(\\s*[(\\/?)\\w+]*)"))
                {
                    inputText = inputText.Replace(previousString, StripHTML(previousString));
                }

                startIndex = inputText.IndexOf(node.OuterHtml);
                var range = new SpannableStringRange(startIndex, startIndex + node.InnerText.Length);
                var style = ExtractStyle(node, range);

                var strBuilder = new System.Text.StringBuilder(inputText);
                strBuilder.Remove(startIndex, node.OuterHtml.Length);
                strBuilder.Insert(startIndex, node.InnerText);
                inputText = strBuilder.ToString();

                if (node.ChildNodes.Count > 0 && node.ChildNodes.Count(n => AcceptedTags.Any(at => at.ToLower() == n.Name)) > 0)
                {
                    style.InnerText = "";
                    ParseTags(node.ChildNodes, style, node);
                }

                if (parentStyle == null) ParsedItems.Add(style);
                else parentStyle.Children.Add(style);
            }
        }

        string StripHTML(string input) => Regex.Replace(input, "<.*?>", string.Empty);
    }
}