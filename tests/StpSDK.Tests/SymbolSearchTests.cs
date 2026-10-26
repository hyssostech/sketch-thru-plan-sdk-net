using Newtonsoft.Json.Linq;
using NUnit.Framework;
using StpSDK.Search;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StpSDK.Tests;

/// <summary>
/// STP-1373: SearchSymbol answered by the SDK from a symbol index. The conformance cases and the
/// invented fixture index are the contract's (sdk-js json-api/symbol-search), vendored verbatim and
/// pinned; every SDK answers them alike.
/// </summary>
[TestFixture]
public class SymbolSearchTests
{
    private static string DataPath(string file) => Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "SymbolSearch", file);

    private static SymbolSearch FixtureSearch() => new(SymbolIndex.Load(DataPath("fixture-index.json")));

    private static string Sha256(string path)
    {
        using var sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(File.ReadAllText(path).Replace("\r\n", "\n")));
        return string.Concat(hash.Select(b => b.ToString("x2", System.Globalization.CultureInfo.InvariantCulture)));
    }

    // ---- the vendored contract files ------------------------------------------------------------

    [Test]
    public void TheVendoredFixtureAndCases_AreTheFilesThePinNames()
    {
        var pin = JObject.Parse(File.ReadAllText(DataPath("symbol-search.pin.json")));
        Assert.That((string)pin["repo"]!, Is.EqualTo("hyssostech/sketch-thru-plan-sdk-js"));
        foreach (var file in new[] { "fixture-index.json", "cases.json" })
            Assert.That(Sha256(DataPath(file)), Is.EqualTo((string)pin["files"]![file]!), file);
    }

    // ---- the conformance cases ------------------------------------------------------------------

    public static IEnumerable<TestCaseData> Cases()
    {
        var cases = JObject.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "SymbolSearch", "cases.json")));
        foreach (JObject c in cases["cases"]!)
            yield return new TestCaseData(c).SetName("Conformance: " + (string)c["name"]!);
    }

    [TestCaseSource(nameof(Cases))]
    public void AnswersTheContractCase(JObject c)
    {
        string text = (string)c["text"]!;
        int max = c["maxResults"] != null ? (int)c["maxResults"]! : SymbolSearch.DefaultMaxResults;

        var results = FixtureSearch().Search(text, max);
        var keys = results.Select(r => r.Key).ToList();

        if (c["expectTop"] is JArray top)
            Assert.That(keys.Take(top.Count), Is.EqualTo(top.Select(t => (string)t!)), "top of " + string.Join(",", keys));
        if (c["expectAll"] is JArray all)
            Assert.That(keys, Is.EqualTo(all.Select(t => (string)t!)));
        if (c["expectSet"] is JArray set)
            Assert.That(keys, Is.EquivalentTo(set.Select(t => (string)t!)));
        if (c["expectFirst"] is JObject first)
        {
            var r = results[0];
            var fields = new Dictionary<string, string>
            {
                ["key"] = r.Key, ["deltaSidc"] = r.DeltaSidc, ["charlieSidc"] = r.CharlieSidc, ["name"] = r.Name,
                ["speech"] = r.Speech, ["gesture"] = r.Gesture, ["defaultAffiliation"] = r.DefaultAffiliation,
            };
            foreach (var p in first.Properties())
                Assert.That(fields[p.Name], Is.EqualTo((string)p.Value!), p.Name);
        }
    }

    // ---- the ranking ----------------------------------------------------------------------------

    [Test]
    public void ScoresDescend_InEveryAnswer()
    {
        var search = FixtureSearch();
        foreach (var text in new[] { "zephyr", "zeph", "marble", "quill sparrow", "zephyr xylograph", "snare" })
        {
            var scores = search.Search(text).Select(r => r.Score).ToList();
            Assert.That(scores, Is.Ordered.Descending, text);
        }
    }

    [Test]
    public void TheExactPhraseRule_IgnoresCaseAndSpacingButNotStopWords()
    {
        var search = FixtureSearch();
        Assert.That(search.Search("  QUILL   sparrow ")[0].Key, Is.EqualTo("Q600"));
        Assert.That(search.Search("quill sparrow")[0].Speech, Is.EqualTo("QUILL SPARROW"));
    }

    [Test]
    public void MaxResults_BelowOne_IsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FixtureSearch().Search("zephyr", 0));
    }

    [Test]
    public void TheLengthNorm_IsStoredInOneByte_AsTheClassicIndexStoresIt()
    {
        // 1/sqrt(tokens) rounded down to the byte's few bits: documents of 3 and 4 tokens weigh the same.
        Assert.That(SymbolSearch.LengthNorm(1), Is.EqualTo(1f));
        Assert.That(SymbolSearch.LengthNorm(2), Is.EqualTo(0.625f));
        Assert.That(SymbolSearch.LengthNorm(3), Is.EqualTo(0.5f));
        Assert.That(SymbolSearch.LengthNorm(4), Is.EqualTo(0.5f));
        Assert.That(SymbolSearch.LengthNorm(100), Is.EqualTo(0.09375f));
        Assert.That(SymbolSearch.LengthNorm(0), Is.EqualTo(0f));
    }

    [Test]
    public void ACodeLine_IsOneWord_AndAPhraseSplitsAtPunctuation()
    {
        Assert.That(SymbolSearch.Tokenize("Start_10031000001211000000_End"), Is.EqualTo(new[] { "start_10031000001211000000_end" }));
        Assert.That(SymbolSearch.Tokenize("Start_SFGPUCI--------_End"), Is.EqualTo(new[] { "start", "sfgpuci", "end" }));
        Assert.That(SymbolSearch.Tokenize("Quill-Sparrow Mid"), Is.EqualTo(new[] { "quill", "sparrow", "mid" }));
    }

    [Test]
    public void ADocument_IsThePhrasePairsAndTriplesThenTheCodes()
    {
        var entry = new SymbolIndexEntry("K1", "10031000001211000000", "", "UNIT", "Name", new[] { "QUILL SPARROW" }, "point", "friend");

        var lines = SymbolSearch.DocumentLines(entry);

        Assert.That(lines.Take(7), Is.EqualTo(new[]
        {
            "Start QUILL", "QUILL Mid", "Mid SPARROW", "SPARROW End",
            "Start QUILL Mid", "QUILL Mid SPARROW", "Mid SPARROW End",
        }));
        // the key, then the 2525D code swept over friend (03, the code itself here), hostile, unknown, neutral
        Assert.That(lines.Skip(7), Is.EqualTo(new[]
        {
            "Start_K1_End", "Start_10031000001211000000_End", "Start_10061000001211000000_End",
            "Start_10011000001211000000_End", "Start_10041000001211000000_End",
        }));
    }

    // ---- the index ------------------------------------------------------------------------------

    [Test]
    public void AMissingIndex_IsAnError_NamingThePath()
    {
        string path = Path.Combine(Path.GetTempPath(), "no-such-dir-" + Guid.NewGuid().ToString("N"), "symbol-index.json");
        var ex = Assert.Throws<SymbolIndexUnavailableException>(() => SymbolIndex.Load(path));
        Assert.That(ex!.Message, Is.EqualTo("symbol index not available: " + path + " (not found)"));
    }

    [Test]
    public void AnIndexOfAnotherSchema_IsAnError()
    {
        var ex = Assert.Throws<SymbolIndexUnavailableException>(() => SymbolIndex.Parse("{\"schema\":\"other\",\"entries\":[]}", "x.json"));
        Assert.That(ex!.Message, Does.StartWith("symbol index not available: x.json (schema is 'other'"));
    }

    [Test]
    public void AKeyTwice_IsAnError()
    {
        const string Json = "{\"schema\":\"stp.symbol-index.v1\",\"entries\":[{\"key\":\"A\",\"phrases\":[\"X\"]},{\"key\":\"A\",\"phrases\":[\"Y\"]}]}";
        var ex = Assert.Throws<SymbolIndexUnavailableException>(() => SymbolIndex.Parse(Json, "x.json"));
        Assert.That(ex!.Reason, Is.EqualTo("key 'A' appears twice"));
    }

    // ---- the stemmer ----------------------------------------------------------------------------

    [TestCase("caresses", "caress")]
    [TestCase("ponies", "poni")]
    [TestCase("cats", "cat")]
    [TestCase("hopping", "hop")]
    [TestCase("falling", "fall")]
    [TestCase("hissing", "hiss")]
    [TestCase("happy", "happi")]
    [TestCase("sky", "sky")]
    [TestCase("generously", "generous")]
    [TestCase("communication", "communic")]
    [TestCase("harbored", "harbor")]
    [TestCase("canary", "canari")]
    [TestCase("lanterns", "lantern")]
    public void Porter2_StemsAsSnowballEnglish(string word, string stem)
    {
        Assert.That(Porter2Stemmer.Stem(word), Is.EqualTo(stem));
    }
}
