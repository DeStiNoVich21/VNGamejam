using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DIALOGUE
{
    public static class TextEffectParser
    {
        private static readonly Regex tagRegex = new Regex(@"\{(\/?[a-zA-Z0-9#]+)(?:=([^}]+))?\}");

        public static string ParseCustomTags(string rawText, out List<TextSegmentData.TextEffect> effects)
        {
            effects = new List<TextSegmentData.TextEffect>();
            System.Text.StringBuilder finalText = new System.Text.StringBuilder();
            Stack<TextSegmentData.TextEffect> effectStack = new Stack<TextSegmentData.TextEffect>();

            int lastMatchEnd = 0;
            MatchCollection matches = tagRegex.Matches(rawText);

            foreach (Match match in matches)
            {
                finalText.Append(rawText.Substring(lastMatchEnd, match.Index - lastMatchEnd));

                string tagName = match.Groups[1].Value.ToLower();
                string tagValue = match.Groups[2].Value;

                if (IsNativeTag(tagName, out string tmpTag))
                {
                    string valuePart = string.IsNullOrEmpty(tagValue) ? "" : "=" + ValidateColor(tagValue);
                    finalText.Append($"<{tmpTag}{valuePart}>");
                }
                else if (tagName.StartsWith("/"))
                {
                    string pureName = tagName.Substring(1);
                    if (IsNativeTag(pureName, out string closeTmp))
                    {
                        finalText.Append($"</{closeTmp}>");
                    }
                    else if (effectStack.Count > 0)
                    {
                        var eff = effectStack.Pop();
                        eff.endIndex = GetVisibleLength(finalText.ToString());
                        effects.Add(eff);
                    }
                }
                else
                {
                    var eff = new TextSegmentData.TextEffect
                    {
                        type = tagName,
                        startIndex = GetVisibleLength(finalText.ToString()),
                        startTime = Time.time,
                        intensity = float.TryParse(tagValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float res) ? res : 1f
                    };
                    effectStack.Push(eff);
                }

                lastMatchEnd = match.Index + match.Length;
            }

            finalText.Append(rawText.Substring(lastMatchEnd));
            return finalText.ToString();
        }

        private static bool IsNativeTag(string tag, out string tmpTag)
        {
            tmpTag = "";
            switch (tag)
            {
                case "c": tmpTag = "color"; return true;
                case "b": tmpTag = "b"; return true;
                case "i": tmpTag = "i"; return true;
                case "size": tmpTag = "size"; return true;
                default: return false;
            }
        }

        private static string ValidateColor(string colorValue)
        {
            if (colorValue.StartsWith("#")) return colorValue;

            return colorValue.ToLower() switch
            {
                "red" => "#FF0000",
                "yellow" => "#FFFF00",
                "green" => "#00FF00",
                "blue" => "#0000FF",
                "white" => "#FFFFFF",
                "black" => "#000000",
                _ => colorValue
            };
        }

        private static int GetVisibleLength(string text)
        {
            return Regex.Replace(text, "<.*?>", "").Length;
        }
    }
}