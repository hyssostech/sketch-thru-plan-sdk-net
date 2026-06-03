using System.Text.RegularExpressions;

namespace StpSDK;

public static class Utility
{
    public static string SpelledLettersToAcronym(string alternate)
    {
        string res = null;
        Regex acroAny = new(@"^(([a-zA-Z]{2,}\s)*?)(([a-zA-Z]\s){2,})(([a-zA-Z]{2,}\s)*?)$");
        string text = alternate.Trim() + " ";
        if (acroAny.IsMatch(text))
        {
            Match m = acroAny.Match(text);
            if (!m.Success || m.Groups.Count != 7)
                return null;
            string prefix = m.Groups[1].Value;
            string acronym = m.Groups[3].Value.Replace(" ", string.Empty).ToUpper();
            string suffix = m.Groups[5].Value;
            res = prefix + " " + acronym + " " + suffix;
        }
        return res?.Trim();
    }
}
