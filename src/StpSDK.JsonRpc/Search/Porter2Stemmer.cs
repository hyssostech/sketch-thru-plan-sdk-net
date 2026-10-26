#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace StpSDK.Search;

/// <summary>
/// The Snowball English ("Porter2") stemmer, which STP's own symbol search applies to the index and
/// to the query (STP-1373). <see cref="SymbolSearch"/> matches stems with it, so "harbored" finds
/// "harbor" and "lanterns" finds "lantern".
/// </summary>
/// <remarks>
/// An implementation of the published algorithm (snowballstem.org, "The English (Porter2) stemming
/// algorithm"). Input is one lower-case word; the result is its stem.
/// </remarks>
public static class Porter2Stemmer
{
    private static readonly Dictionary<string, string> Exceptions = new(StringComparer.Ordinal)
    {
        ["skis"] = "ski", ["skies"] = "sky", ["dying"] = "die", ["lying"] = "lie", ["tying"] = "tie",
        ["idly"] = "idl", ["gently"] = "gentl", ["ugly"] = "ugli", ["early"] = "earli", ["only"] = "onli",
        ["singly"] = "singl", ["sky"] = "sky", ["news"] = "news", ["howe"] = "howe", ["atlas"] = "atlas",
        ["cosmos"] = "cosmos", ["bias"] = "bias", ["andes"] = "andes",
    };

    private static readonly HashSet<string> Invariant1a = new(StringComparer.Ordinal)
    {
        "inning", "outing", "canning", "herring", "earring", "proceed", "exceed", "succeed",
    };

    // Longest suffix first within each step: the step acts on the longest suffix that matches.
    private static readonly string[][] Step2 =
    {
        new[] { "ization", "ize" }, new[] { "ational", "ate" }, new[] { "fulness", "ful" }, new[] { "ousness", "ous" },
        new[] { "iveness", "ive" }, new[] { "tional", "tion" }, new[] { "biliti", "ble" }, new[] { "lessli", "less" },
        new[] { "entli", "ent" }, new[] { "ation", "ate" }, new[] { "alism", "al" }, new[] { "aliti", "al" },
        new[] { "ousli", "ous" }, new[] { "iviti", "ive" }, new[] { "fulli", "ful" }, new[] { "enci", "ence" },
        new[] { "anci", "ance" }, new[] { "abli", "able" }, new[] { "izer", "ize" }, new[] { "ator", "ate" },
        new[] { "alli", "al" }, new[] { "bli", "ble" }, new[] { "ogi", "og" }, new[] { "li", "" },
    };

    private static readonly string[][] Step3 =
    {
        new[] { "ational", "ate" }, new[] { "tional", "tion" }, new[] { "alize", "al" }, new[] { "icate", "ic" },
        new[] { "iciti", "ic" }, new[] { "ative", "" }, new[] { "ical", "ic" }, new[] { "ness", "" }, new[] { "ful", "" },
    };

    private static readonly string[] Step4 =
    {
        "ement", "ance", "ence", "able", "ible", "ment", "ant", "ent", "ism", "ate", "iti", "ous", "ive", "ize", "ion", "al", "er", "ic",
    };

    /// <summary>The stem of <paramref name="word"/> (lower case).</summary>
    public static string Stem(string word)
    {
        if (word is null)
            throw new ArgumentNullException(nameof(word));
        if (word.Length <= 2)
            return word;
        if (Exceptions.TryGetValue(word, out var exception))
            return exception;

        var w = word[0] == '\'' ? word.Substring(1) : word;
        w = MarkConsonantY(w);
        int r1 = w.StartsWith("gener", StringComparison.Ordinal) || w.StartsWith("arsen", StringComparison.Ordinal) ? 5
            : w.StartsWith("commun", StringComparison.Ordinal) ? 6
            : RegionAfter(w, 0);
        int r2 = RegionAfter(w, r1);

        w = Step0(w);
        w = Step1a(w);
        if (Invariant1a.Contains(w))
            return w.Replace('Y', 'y');
        w = Step1b(w, r1);
        w = Step1c(w);
        w = Replace(w, Step2, r1, Step2Condition);
        w = Step3Apply(w, r1, r2);
        w = Step4Apply(w, r2);
        w = Step5(w, r1, r2);
        return w.Replace('Y', 'y');
    }

    private static bool IsVowel(char c) => c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u' || c == 'y';

    private static bool EndsWithDouble(string w)
        => w.Length >= 2 && w[w.Length - 1] == w[w.Length - 2] && "bdfgmnprt".IndexOf(w[w.Length - 1]) >= 0;

    private static string MarkConsonantY(string w)
    {
        var c = w.ToCharArray();
        for (int i = 0; i < c.Length; i++)
            if (c[i] == 'y' && (i == 0 || IsVowel(c[i - 1])))
                c[i] = 'Y';
        return new string(c);
    }

    /// <summary>The region after the first non-vowel that follows a vowel, from <paramref name="start"/>.</summary>
    private static int RegionAfter(string w, int start)
    {
        for (int i = start + 1; i < w.Length; i++)
            if (!IsVowel(w[i]) && IsVowel(w[i - 1]))
                return i + 1;
        return w.Length;
    }

    private static bool EndsInShortSyllable(string w)
    {
        int n = w.Length;
        if (n == 2)
            return IsVowel(w[0]) && !IsVowel(w[1]);
        return n >= 3 && !IsVowel(w[n - 3]) && IsVowel(w[n - 2]) && !IsVowel(w[n - 1])
            && w[n - 1] != 'w' && w[n - 1] != 'x' && w[n - 1] != 'Y';
    }

    private static string Cut(string w, int count) => w.Substring(0, w.Length - count);

    private static string Step0(string w)
    {
        foreach (var s in new[] { "'s'", "'s", "'" })
            if (w.EndsWith(s, StringComparison.Ordinal))
                return Cut(w, s.Length);
        return w;
    }

    private static string Step1a(string w)
    {
        if (w.EndsWith("sses", StringComparison.Ordinal))
            return Cut(w, 2);
        if (w.EndsWith("ied", StringComparison.Ordinal) || w.EndsWith("ies", StringComparison.Ordinal))
            return w.Length > 4 ? Cut(w, 2) : Cut(w, 1);
        if (w.EndsWith("us", StringComparison.Ordinal) || w.EndsWith("ss", StringComparison.Ordinal))
            return w;
        if (w.EndsWith("s", StringComparison.Ordinal) && Cut(w, 2).Any(IsVowel))
            return Cut(w, 1);
        return w;
    }

    private static string Step1b(string w, int r1)
    {
        foreach (var s in new[] { "eedly", "eed" })
            if (w.EndsWith(s, StringComparison.Ordinal))
                return w.Length - s.Length >= r1 ? Cut(w, s.Length) + "ee" : w;

        foreach (var s in new[] { "ingly", "edly", "ing", "ed" })
        {
            if (!w.EndsWith(s, StringComparison.Ordinal))
                continue;
            var stem = Cut(w, s.Length);
            if (!stem.Any(IsVowel))
                return w;
            if (stem.EndsWith("at", StringComparison.Ordinal) || stem.EndsWith("bl", StringComparison.Ordinal) || stem.EndsWith("iz", StringComparison.Ordinal))
                return stem + "e";
            if (EndsWithDouble(stem))
                return Cut(stem, 1);
            if (r1 >= stem.Length && EndsInShortSyllable(stem))
                return stem + "e";
            return stem;
        }
        return w;
    }

    private static string Step1c(string w)
    {
        if (w.Length > 2 && (w[w.Length - 1] == 'y' || w[w.Length - 1] == 'Y') && !IsVowel(w[w.Length - 2]))
            return Cut(w, 1) + "i";
        return w;
    }

    private static bool Step2Condition(string w, string suffix)
    {
        if (suffix == "ogi")
            return w.Length > 3 && w[w.Length - 4] == 'l';
        if (suffix == "li")
            return w.Length > 2 && "cdeghkmnrt".IndexOf(w[w.Length - 3]) >= 0;
        return true;
    }

    /// <summary>Replaces the longest listed suffix when it lies in <paramref name="region"/> and the condition holds.</summary>
    private static string Replace(string w, string[][] table, int region, Func<string, string, bool> condition)
    {
        foreach (var pair in table)
        {
            if (!w.EndsWith(pair[0], StringComparison.Ordinal))
                continue;
            if (w.Length - pair[0].Length >= region && condition(w, pair[0]))
                return Cut(w, pair[0].Length) + pair[1];
            return w;
        }
        return w;
    }

    private static string Step3Apply(string w, int r1, int r2)
        => Replace(w, Step3, r1, (word, suffix) => suffix != "ative" || word.Length - suffix.Length >= r2);

    private static string Step4Apply(string w, int r2)
    {
        foreach (var s in Step4)
        {
            if (!w.EndsWith(s, StringComparison.Ordinal))
                continue;
            if (w.Length - s.Length < r2)
                return w;
            if (s == "ion")
                return w.Length > 3 && (w[w.Length - 4] == 's' || w[w.Length - 4] == 't') ? Cut(w, 3) : w;
            return Cut(w, s.Length);
        }
        return w;
    }

    private static string Step5(string w, int r1, int r2)
    {
        if (w.EndsWith("e", StringComparison.Ordinal))
        {
            int at = w.Length - 1;
            if (at >= r2 || (at >= r1 && !EndsInShortSyllable(Cut(w, 1))))
                return Cut(w, 1);
            return w;
        }
        if (w.EndsWith("l", StringComparison.Ordinal) && w.Length - 1 >= r2 && w.Length > 1 && w[w.Length - 2] == 'l')
            return Cut(w, 1);
        return w;
    }
}
