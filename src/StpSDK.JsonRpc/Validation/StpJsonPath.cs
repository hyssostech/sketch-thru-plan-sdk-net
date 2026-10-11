#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace StpSDK.Validation;

/// <summary>
/// The path and value vocabulary of the call-validation rules (sketch-thru-plan-validations.json):
/// dotted paths, numeric segments that index arrays, <c>length</c> of an array, <c>*</c> for every
/// item, and equality that ignores the case of strings.
/// </summary>
internal static class StpJsonPath
{
    /// <summary>Whether a value is absent: no token, or JSON null.</summary>
    public static bool IsAbsent(JToken? value) =>
        value == null || value.Type == JTokenType.Null || value.Type == JTokenType.Undefined;

    /// <summary>The value at a dotted path; numeric segments index arrays, <c>length</c> is an array's length.</summary>
    public static JToken? Get(JToken? value, string path)
    {
        JToken? cur = value;
        foreach (string seg in path.Split('.'))
        {
            if (cur is JArray array)
            {
                if (seg == "length") cur = new JValue(array.Count);
                else if (IsIndex(seg) && int.TryParse(seg, NumberStyles.None, CultureInfo.InvariantCulture, out int i) && i < array.Count) cur = array[i];
                else return null;
            }
            else if (cur is JObject obj)
            {
                cur = obj[seg];
            }
            else
            {
                return null;
            }
        }
        return cur;
    }

    /// <summary>Every concrete path and value a path with <c>*</c> segments reaches (absent values left out).</summary>
    public static IReadOnlyList<KeyValuePair<string, JToken>> Expand(JToken? value, string path)
    {
        var found = new List<KeyValuePair<string, JToken>>();
        Walk(value, path.Split('.'), 0, new List<string>(), found);
        return found;
    }

    private static void Walk(JToken? cur, string[] segs, int at, List<string> done, List<KeyValuePair<string, JToken>> found)
    {
        if (at == segs.Length)
        {
            if (!IsAbsent(cur)) found.Add(new KeyValuePair<string, JToken>(string.Join(".", done), cur!));
            return;
        }
        string seg = segs[at];
        if (seg == "*")
        {
            if (cur is JArray array)
            {
                for (int i = 0; i < array.Count; i++)
                {
                    done.Add(i.ToString(CultureInfo.InvariantCulture));
                    Walk(array[i], segs, at + 1, done, found);
                    done.RemoveAt(done.Count - 1);
                }
            }
            return;
        }
        done.Add(seg);
        Walk(Get(cur, seg), segs, at + 1, done, found);
        done.RemoveAt(done.Count - 1);
    }

    /// <summary>Equality as the rules define it: strings without regard to case, anything else by value.</summary>
    public static bool Same(JToken? a, JToken? b)
    {
        if (IsAbsent(a) || IsAbsent(b)) return IsAbsent(a) && IsAbsent(b) && a?.Type == b?.Type;
        if (a!.Type == JTokenType.String && b!.Type == JTokenType.String)
        {
            return string.Equals((string?)a, (string?)b, StringComparison.OrdinalIgnoreCase);
        }
        if (IsNumber(a) && IsNumber(b!))
        {
            return ((JValue)a).CompareTo((JValue)b!) == 0;
        }
        return JToken.DeepEquals(a, b);
    }

    /// <summary>A value as the rules' messages show it: JSON text (a string in quotes).</summary>
    public static string Show(JToken? value) => value == null ? "undefined" : value.ToString(Formatting.None);

    /// <summary>A value as a confirmation text shows it: a string as is, anything else as JSON.</summary>
    public static string Text(JToken value) =>
        value.Type == JTokenType.String ? (string)value! : value.ToString(Formatting.None);

    /// <summary>A string value, or null when the token is not a string.</summary>
    public static string? AsString(JToken? value) => value != null && value.Type == JTokenType.String ? (string?)value : null;

    private static bool IsNumber(JToken t) => t.Type == JTokenType.Integer || t.Type == JTokenType.Float;

    private static bool IsIndex(string seg)
    {
        if (seg.Length == 0) return false;
        foreach (char c in seg)
        {
            if (c < '0' || c > '9') return false;
        }
        return true;
    }
}
