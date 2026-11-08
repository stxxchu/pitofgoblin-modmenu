using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using I2.Loc;

namespace PitOfGoblinCheats;

internal static class Names
{
    static readonly Regex RichText = new("<[^>]+>", RegexOptions.Compiled);
    static readonly HashSet<string> NoiseWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "pfb", "prefab", "item", "items", "net", "clone", "entity", "new",
    };

    public static string Translate(string term)
    {
        if (string.IsNullOrEmpty(term)) return null;
        return Clean(Lookup(term, "English")) ?? Clean(Lookup(term, null));
    }

    public static string Label(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        string translated = Translate(text);
        if (translated != null) return translated;

        int slash = text.LastIndexOf('/');
        if (slash >= 0 || text.IndexOf(' ') < 0) return Pretty(slash >= 0 ? text.Substring(slash + 1) : text);
        return Clean(text);
    }

    static string Lookup(string term, string language)
    {
        try
        {
            return LocalizationManager.GetTranslation(term, true, 0, true, false, null, language, true);
        }
        catch
        {
            return null;
        }
    }

    static string Clean(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        text = RichText.Replace(text, "").Replace('\n', ' ').Trim();
        if (text.Length == 0) return null;
        return char.ToUpper(text[0]) + text.Substring(1);
    }

    public static string Pretty(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;

        var words = new List<string>();
        var current = new StringBuilder();

        void Flush()
        {
            if (current.Length == 0) return;
            string w = current.ToString();
            current.Clear();
            if (!NoiseWords.Contains(w) && !int.TryParse(w, out _)) words.Add(w);
        }

        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];
            if (c == '_' || c == '-' || c == ' ' || c == '(' || c == ')' || c == '.')
            {
                Flush();
                continue;
            }
            if (i > 0 && char.IsUpper(c) && char.IsLower(raw[i - 1])) Flush();
            current.Append(c);
        }
        Flush();

        if (words.Count == 0) return raw;
        for (int i = 0; i < words.Count; i++)
            words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
        return string.Join(" ", words);
    }
}
