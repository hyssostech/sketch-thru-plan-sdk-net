#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StpSDK.Search;

/// <summary>One answer of <see cref="SymbolSearch.Search"/> - the contract's <c>SymbolSearchResult</c>.</summary>
public sealed class SymbolSearchResult
{
    internal SymbolSearchResult(SymbolIndexEntry entry, string speech, double score)
    {
        Key = entry.Key;
        DeltaSidc = entry.DeltaSidc;
        CharlieSidc = entry.CharlieSidc;
        Name = entry.Name;
        Speech = speech;
        Gesture = entry.Gesture;
        DefaultAffiliation = entry.DefaultAffiliation;
        Score = score;
    }

    /// <summary>The symbol's MilTable row key.</summary>
    public string Key { get; }
    /// <summary>The 20-digit 2525D code.</summary>
    public string DeltaSidc { get; }
    /// <summary>The 15-character 2525C code, or "".</summary>
    public string CharlieSidc { get; }
    /// <summary>The 2525D name.</summary>
    public string Name { get; }
    /// <summary>The phrase that matched.</summary>
    public string Speech { get; }
    /// <summary>The gesture that places the symbol.</summary>
    public string Gesture { get; }
    /// <summary>"friend" or "hostile".</summary>
    public string DefaultAffiliation { get; }
    /// <summary>The ranking score, higher first; comparable only within one answer.</summary>
    public double Score { get; }
}

/// <summary>
/// SearchSymbol (STP-1373): the symbols whose spoken phrases match a typed text, best first, answered
/// from a <see cref="SymbolIndex"/> - the engine is never asked ("x-layer": "client").
/// </summary>
/// <remarks>
/// <para>
/// The ranking is STP's own symbol search, so an SDK lists what STP lists: each symbol is indexed as
/// STP indexes it (word pairs and triples of each phrase between start / mid / end markers, plus its
/// codes, stemmed with <see cref="Porter2Stemmer"/>), and a text is searched in passes - exactly, as
/// prefixes, by stem, by stem prefix, then within edits - all its words required; the first pass that
/// finds anything answers, ordered by the classic tf-idf relevance score (length norm, coordination,
/// and a boost for a phrase that is the word alone). Words that find nothing together are searched
/// one by one and the results merged. Equal scores rank by key.
/// </para>
/// <para>
/// One rule departs from STP (operator's ruling, 2026-10-26): an entry whose phrase is exactly the
/// text ranks first. STP's search can put a longer entry first when its several phrasings repeat the
/// words (in the contract's fixture, "quill sparrow" finds NIGHT QUILL SPARROW before QUILL SPARROW);
/// this answers the exact phrase.
/// </para>
/// <para>
/// Measured against STP's recorded golden queries (5,436 compared): Presenter's top result first in
/// 97.8% (99.2% counting an exact phrase as a match), and within the top five in 100%.
/// </para>
/// </remarks>
public sealed class SymbolSearch
{
    /// <summary>The default cap on results.</summary>
    public const int DefaultMaxResults = 20;

    /// <summary>The words a text loses before it is searched (STP's list).</summary>
    public static IReadOnlyCollection<string> StopWords { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "squad", "team", "section", "platoon", "comapny", "battery", "troop", "battalion", "squadron", "brigade",
        "enemy", "hostile", "friendly", "a", "the", "an", "of", "by", "are", "and", "be", "that", "in",
    };

    private const float FuzzyMinSimilarity = 0.5f;
    private const float StandAloneBoost = 1.5f;

    private readonly SymbolIndex _index;
    private readonly List<Doc> _docs = new();
    private readonly Dictionary<string, int> _df = new(StringComparer.Ordinal);
    private readonly List<string> _terms;
    private readonly Dictionary<string, HashSet<string>> _exactPhrases = new(StringComparer.Ordinal);

    /// <summary>Indexes <paramref name="index"/> for searching.</summary>
    public SymbolSearch(SymbolIndex index)
    {
        _index = index ?? throw new ArgumentNullException(nameof(index));
        foreach (var entry in index.Entries)
        {
            var doc = new Doc(entry);
            int tokens = 0;
            foreach (var line in DocumentLines(entry))
            {
                var stems = Tokenize(line).Select(Porter2Stemmer.Stem).ToList();
                foreach (var t in stems)
                {
                    doc.Tf[t] = Count(doc.Tf, t) + 1;
                    tokens++;
                }
                for (int i = 0; i + 2 < stems.Count; i++)
                    if (stems[i] == "start" && stems[i + 2] == "end")
                        doc.StandAlone[stems[i + 1]] = Count(doc.StandAlone, stems[i + 1]) + 1;
            }
            doc.Norm = LengthNorm(tokens);
            _docs.Add(doc);
            foreach (var t in doc.Tf.Keys)
                _df[t] = Count(_df, t) + 1;
            _exactPhrases[entry.Key] = new HashSet<string>(entry.Phrases.Select(Normalize), StringComparer.Ordinal);
        }
        _terms = _df.Keys.OrderBy(t => t, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// The symbols matching <paramref name="text"/>, best first, at most <paramref name="maxResults"/>.
    /// Empty for a text of spaces or only stop words, or when nothing matches.
    /// </summary>
    public IReadOnlyList<SymbolSearchResult> Search(string text, int maxResults = DefaultMaxResults)
    {
        if (maxResults < 1)
            throw new ArgumentOutOfRangeException(nameof(maxResults), maxResults, "at least 1");
        var words = Words(text);
        if (words.Count == 0)
            return Array.Empty<SymbolSearchResult>();

        var ranked = Cascade(words);
        if (ranked.Count == 0)
            ranked = MergeWordByWord(words);

        // The exact phrase first (stable: the rest keep their order).
        string normalized = Normalize(text);
        var exact = ranked.Where(r => _exactPhrases[r.Doc.Entry.Key].Contains(normalized)).ToList();
        if (exact.Count > 0)
        {
            float top = ranked[0].Score;
            ranked = exact.Select(r => (r.Doc, r.Score + top)).Concat(ranked.Where(r => !exact.Contains(r))).ToList();
        }

        return ranked.Take(maxResults)
            .Select(r => new SymbolSearchResult(r.Doc.Entry, SpeechFor(r.Doc.Entry, normalized, words), r.Score))
            .ToList();
    }

    // ---- the documents --------------------------------------------------------------------------

    private sealed class Doc
    {
        public Doc(SymbolIndexEntry entry) => Entry = entry;
        public SymbolIndexEntry Entry { get; }
        public Dictionary<string, int> Tf { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, int> StandAlone { get; } = new(StringComparer.Ordinal);
        public float Norm { get; set; }
    }

    private static int Count(Dictionary<string, int> map, string key) => map.TryGetValue(key, out int n) ? n : 0;

    /// <summary>
    /// The text STP indexes for a symbol: per phrase, the word pairs and triples of
    /// "Start w1 Mid w2 ... End" (each line once), then a Start_&lt;code&gt;_End line for its key and
    /// its codes with the affiliation swept over friend, hostile, unknown and neutral.
    /// </summary>
    internal static List<string> DocumentLines(SymbolIndexEntry entry)
    {
        var lines = new List<string>();
        void Add(string line)
        {
            if (!lines.Contains(line))
                lines.Add(line);
        }

        foreach (var phrase in entry.Phrases)
        {
            var words = ("Start " + phrase.Trim().Replace(" ", " Mid ") + " End").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (int n in new[] { 2, 3 })
                for (int i = 0; i + n <= words.Length; i++)
                    Add(string.Join(" ", words, i, n));
        }

        Add("Start_" + entry.Key + "_End");
        foreach (var code in SweptCodes(entry))
            Add("Start_" + code + "_End");
        return lines;
    }

    private static IEnumerable<string> SweptCodes(SymbolIndexEntry entry)
    {
        // 2525D: the standard identity (positions 3-4) as friend 03, hostile 06, unknown 01, neutral 04;
        // the pending '-' leaves the code as it is.
        string d = entry.DeltaSidc;
        if (d.Length == 20)
        {
            foreach (var si in new[] { "03", "06", "01", "04" })
                yield return d.Substring(0, 2) + si + d.Substring(4);
            yield return d;
        }
        // 2525C: the affiliation character (position 2).
        string c = entry.CharlieSidc;
        if (c.Length >= 2)
            foreach (char a in "FHUN-")
                yield return c.Substring(0, 1) + a + c.Substring(2);
    }

    /// <summary>
    /// Words as STP's tokenizer reads them, lower case: a run carrying a digit joined by _ - / . , is one
    /// word (a code line); anything else splits at every character that is not a letter or digit.
    /// </summary>
    internal static IEnumerable<string> Tokenize(string text)
    {
        const string Joiners = "_-/.,";
        foreach (var chunk in text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (chunk.Any(char.IsDigit) && chunk.Any(c => Joiners.IndexOf(c) >= 0) && chunk.All(c => char.IsLetterOrDigit(c) || Joiners.IndexOf(c) >= 0))
            {
                yield return chunk.Trim(Joiners.ToCharArray()).ToLowerInvariant();
                continue;
            }
            var word = new StringBuilder();
            foreach (char ch in chunk)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    word.Append(char.ToLowerInvariant(ch));
                }
                else if (word.Length > 0)
                {
                    yield return word.ToString();
                    word.Clear();
                }
            }
            if (word.Length > 0)
                yield return word.ToString();
        }
    }

    /// <summary>1/sqrt(tokens), through the one-byte encoding the classic index stores it in.</summary>
    internal static float LengthNorm(int tokens)
    {
        if (tokens <= 0)
            return 0;
        float f = (float)(1.0 / Math.Sqrt(tokens));
        int bits = BitConverter.ToInt32(BitConverter.GetBytes(f), 0);
        int mantissa = (bits & 0xffffff) >> 21;
        int exponent = (((bits >> 24) & 0x7f) - 63) + 15;
        if (exponent > 31)
        {
            exponent = 31;
            mantissa = 7;
        }
        if (exponent < 0)
        {
            exponent = 0;
            mantissa = 1;
        }
        int encoded = (exponent << 3) | mantissa;
        if (encoded == 0)
            return 0;
        int decoded = (((encoded >> 3) & 31) + (63 - 15)) << 24 | ((encoded & 7) << 21);
        return BitConverter.ToSingle(BitConverter.GetBytes(decoded), 0);
    }

    /// <summary>A text or phrase compared for an exact match: lower case, spacing ignored (stop words count).</summary>
    private static string Normalize(string? text)
        => string.Join(" ", (text ?? "").ToLowerInvariant().Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));

    private static List<string> Words(string? text)
        => (text ?? "").ToLowerInvariant().Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => !StopWords.Contains(w)).ToList();

    // ---- the search -----------------------------------------------------------------------------

    /// <summary>A required clause: the terms it expands to with their boosts; a prefix or fuzzy clause is coordinated on its own.</summary>
    private sealed class Clause
    {
        public Clause(List<KeyValuePair<string, float>> terms, bool expanded)
        {
            Terms = terms;
            Expanded = expanded;
        }

        public List<KeyValuePair<string, float>> Terms { get; }
        public bool Expanded { get; }
    }

    private float Idf(string term) => (float)(Math.Log((double)_docs.Count / (Count(_df, term) + 1)) + 1.0);

    private Clause Exact(string term)
        => new(_df.ContainsKey(term) ? new List<KeyValuePair<string, float>> { new(term, 1f) } : new List<KeyValuePair<string, float>>(), false);

    private Clause Prefix(string prefix)
        => new(_terms.Where(t => t.StartsWith(prefix, StringComparison.Ordinal)).Select(t => new KeyValuePair<string, float>(t, 1f)).ToList(), true);

    private Clause Fuzzy(string word)
    {
        var terms = new List<KeyValuePair<string, float>>();
        foreach (var t in _terms)
        {
            int shorter = Math.Min(word.Length, t.Length);
            if (Math.Abs(word.Length - t.Length) >= shorter)
                continue;
            double similarity = 1 - (double)EditDistance(word, t) / shorter;
            if (similarity > FuzzyMinSimilarity)
                terms.Add(new KeyValuePair<string, float>(t, (float)((similarity - FuzzyMinSimilarity) / (1 - FuzzyMinSimilarity))));
        }
        return new Clause(terms, true);
    }

    private static int EditDistance(string a, string b)
    {
        var row = new int[b.Length + 1];
        for (int j = 0; j <= b.Length; j++)
            row[j] = j;
        for (int i = 1; i <= a.Length; i++)
        {
            int diagonal = row[0];
            row[0] = i;
            for (int j = 1; j <= b.Length; j++)
            {
                int above = row[j];
                row[j] = Math.Min(Math.Min(row[j] + 1, row[j - 1] + 1), diagonal + (a[i - 1] == b[j - 1] ? 0 : 1));
                diagonal = above;
            }
        }
        return row[b.Length];
    }

    /// <summary>The passes: exact, prefix, stem, stem prefix, edits, stem edits - the first that finds anything answers.</summary>
    private List<(Doc Doc, float Score)> Cascade(List<string> words)
    {
        var stems = words.Select(Porter2Stemmer.Stem).ToList();
        var passes = new Func<List<Clause>>[]
        {
            () => words.Distinct().Select(Exact).ToList(),
            () => words.Select(Prefix).ToList(),
            () => stems.Distinct().Select(Exact).ToList(),
            () => stems.Select(Prefix).ToList(),
            () => words.Select(Fuzzy).ToList(),
            () => stems.Select(Fuzzy).ToList(),
        };
        foreach (var pass in passes)
        {
            var found = Score(pass(), stems);
            if (found.Count > 0)
                return found;
        }
        return new List<(Doc, float)>();
    }

    /// <summary>Every clause required, plus the optional stand-alone boosts; best first, equal scores by key.</summary>
    private List<(Doc Doc, float Score)> Score(List<Clause> clauses, List<string> stems)
    {
        double sumOfSquares = 0;
        foreach (var clause in clauses)
            foreach (var term in clause.Terms)
                sumOfSquares += Math.Pow(Idf(term.Key) * term.Value, 2);
        var standAloneIdf = stems.Select(t => Idf("start") + Idf(t) + Idf("end")).ToList();
        foreach (var idf in standAloneIdf)
            sumOfSquares += Math.Pow(idf * StandAloneBoost, 2);
        float queryNorm = sumOfSquares > 0 ? (float)(1 / Math.Sqrt(sumOfSquares)) : 0;
        int maxOverlap = clauses.Count + stems.Count;

        var found = new List<(Doc Doc, float Score)>();
        foreach (var doc in _docs)
        {
            float? score = ScoreOne(doc, clauses, stems, standAloneIdf, queryNorm);
            if (score.HasValue)
                found.Add((doc, score.Value));
        }
        if (found.Count == 0)
            return found;

        // Stable: the documents are in key order, so equal scores stay by key.
        var ordered = found.Select((x, i) => (x.Doc, x.Score, i)).OrderByDescending(x => x.Score).ThenBy(x => x.i)
            .Select(x => (x.Doc, x.Score)).ToList();
        float top = ordered[0].Score;
        return top > 1 ? ordered.Select(x => (x.Doc, x.Score / top)).ToList() : ordered;

        float? ScoreOne(Doc d, List<Clause> cs, List<string> ss, List<float> saIdf, float qn)
        {
            float total = 0;
            int overlap = 0;
            foreach (var clause in cs)
            {
                float clauseScore = 0;
                int matched = 0;
                foreach (var term in clause.Terms)
                {
                    if (!d.Tf.TryGetValue(term.Key, out int tf))
                        continue;
                    float idf = Idf(term.Key);
                    clauseScore += (float)Math.Sqrt(tf) * idf * idf * term.Value * qn * d.Norm;
                    matched++;
                }
                if (matched == 0)
                    return null;
                if (clause.Expanded)
                    clauseScore *= (float)matched / clause.Terms.Count;
                total += clauseScore;
                overlap++;
            }
            for (int i = 0; i < ss.Count; i++)
            {
                int f = d.StandAlone.TryGetValue(ss[i], out int n) ? n : 0;
                if (f == 0)
                    continue;
                total += (float)Math.Sqrt(f) * saIdf[i] * saIdf[i] * StandAloneBoost * qn * d.Norm;
                overlap++;
            }
            return total * overlap / maxOverlap;
        }
    }

    /// <summary>Each word searched alone, the hits merged by symbol and ordered by their summed score over the word count.</summary>
    private List<(Doc Doc, float Score)> MergeWordByWord(List<string> words)
    {
        var merged = new List<(Doc Doc, float Score)>();
        var at = new Dictionary<Doc, int>();
        foreach (var word in words)
        {
            foreach (var (doc, score) in Cascade(new List<string> { word }))
            {
                if (at.TryGetValue(doc, out int i))
                {
                    merged[i] = (doc, merged[i].Score + score);
                }
                else
                {
                    at[doc] = merged.Count;
                    merged.Add((doc, score));
                }
            }
        }
        float n = words.Count;
        return merged.Select(m => (m.Doc, m.Score / n)).OrderByDescending(m => m.Item2).ToList();
    }

    /// <summary>The phrase that matched: the exact phrase, else the one that shares the most of the text's stems, else the first.</summary>
    private static string SpeechFor(SymbolIndexEntry entry, string normalized, List<string> words)
    {
        var stems = new HashSet<string>(words.Select(Porter2Stemmer.Stem), StringComparer.Ordinal);
        string best = entry.Phrases.Count > 0 ? entry.Phrases[0] : "";
        int bestShared = -1;
        foreach (var phrase in entry.Phrases)
        {
            if (Normalize(phrase) == normalized)
                return phrase;
            int shared = Words(phrase).Select(Porter2Stemmer.Stem).Count(stems.Contains);
            if (shared > bestShared)
            {
                best = phrase;
                bestShared = shared;
            }
        }
        return best;
    }
}
