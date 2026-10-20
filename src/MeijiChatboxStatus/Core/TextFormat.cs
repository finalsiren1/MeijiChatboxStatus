using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MeijiChatboxStatus.Core;

public static partial class TextFormat
{
    /// <summary>VRChat chatbox limits.</summary>
    public const int MaxChars = 144;
    public const int MaxLines = 9;

    [GeneratedRegex(@"\{([a-zA-Z0-9_]+)(?::([^{}\r\n]+))?\}")]
    private static partial Regex PlaceholderRegex();

    [GeneratedRegex(@"^[\s\-–—|•·/:,]+|[\s\-–—|•·/:,]+$")]
    private static partial Regex DanglingSeparatorRegex();

    /// <summary>
    /// Fills {name} / {name:arg} placeholders line by line. Unknown placeholders are kept as typed so typos show in the preview.
    /// Lines whose placeholders had nothing to show are dropped, and separators left dangling at line edges
    /// (e.g. "Song - " when the artist is unknown) are trimmed.
    /// </summary>
    public static string Render(string template, Func<string, string?, string?> resolve)
    {
        var output = new List<string>();
        foreach (string source in template.Replace("\r\n", "\n").Split('\n'))
        {
            bool hasPlaceholder = PlaceholderRegex().IsMatch(source);
            string line = !hasPlaceholder ? source : PlaceholderRegex().Replace(source, m =>
                resolve(m.Groups[1].Value.ToLowerInvariant(), m.Groups[2].Success ? m.Groups[2].Value.Trim() : null) ?? m.Value);
            if (hasPlaceholder)
            {
                // A placeholder may expand to several lines (e.g. {music}); tidy each of them.
                foreach (string part in line.Split('\n'))
                {
                    string tidy = DanglingSeparatorRegex().Replace(part, "");
                    if (tidy.Length > 0) output.Add(tidy);
                }
            }
            else output.Add(line.TrimEnd());
        }
        while (output.Count > 0 && output[^1].Length == 0) output.RemoveAt(output.Count - 1);
        while (output.Count > 0 && output[0].Length == 0) output.RemoveAt(0);
        return string.Join('\n', output);
    }

    /// <summary>Cuts text to what the chatbox accepts without splitting emoji or Thai combining characters.</summary>
    public static string Clamp(string text)
    {
        var lines = text.Split('\n');
        if (lines.Length > MaxLines) text = string.Join('\n', lines.Take(MaxLines));
        if (text.Length <= MaxChars) return text;

        var sb = new StringBuilder(MaxChars);
        var e = StringInfo.GetTextElementEnumerator(text);
        while (e.MoveNext())
        {
            string element = e.GetTextElement();
            if (sb.Length + element.Length > MaxChars - 1) break;
            sb.Append(element);
        }
        return sb.ToString().TrimEnd() + "…";
    }

    public static string Duration(TimeSpan t) =>
        t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}" : $"{(int)t.TotalMinutes}:{t.Seconds:D2}";

    public static string ProgressBar(string style, int width, double fraction)
    {
        fraction = double.IsFinite(fraction) ? Math.Clamp(fraction, 0, 1) : 0;
        int filled = (int)Math.Round(fraction * width);
        int knob = Math.Min(width - 1, (int)Math.Floor(fraction * width));
        return style switch
        {
            "dots" => new string('●', filled) + new string('○', width - filled),
            "blocks" => new string('▓', filled) + new string('░', width - filled),
            "hearts" => new string('♥', filled) + new string('♡', width - filled),
            "classic" => "[" + string.Concat(Enumerable.Range(0, width).Select(i => i == knob ? 'o' : '-')) + "]",
            _ => string.Concat(Enumerable.Range(0, width).Select(i => i < knob ? '━' : i == knob ? '◉' : '─')),
        };
    }
}
