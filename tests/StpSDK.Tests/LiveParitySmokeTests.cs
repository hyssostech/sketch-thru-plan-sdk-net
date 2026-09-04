using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace StpSDK.Tests;

/// <summary>
/// Live smoke tests exercised against a running STP engine (ws://localhost:9599) to
/// validate end-to-end parity with the JS SDK and the engine wire format - especially
/// the structured <see cref="Sidc"/> (the engine sends partA/partB/symbolSet/legacy)
/// and JMSML rendering. Category "SmokeTest" is excluded from CI; run explicitly with
/// a live engine: dotnet test --filter Category=SmokeTest
/// </summary>
[TestFixture]
[Category("SmokeTest")]
public class LiveParitySmokeTests
{
    // Override for a containerised engine; defaults to the standard port so a
    // developer's local STP still works unchanged.
    internal static string TestStpUrl =>
        Environment.GetEnvironmentVariable("STP_SDK_TEST_URL") ?? "ws://localhost:9599";

    private const int ConnectTimeoutSec = 15;
    private const int EventWaitMs = 30_000;
    private const string SvgPath = @"C:\ProgramData\STP\JMS\svg";

    // Map extent used for placing point symbols (Bogoland scenario area).
    private static readonly LatLon TopLeft = new(59.27, 9.70);
    private static readonly LatLon BotRight = new(59.01, 10.44);
    private static readonly LatLon Center = new(59.14, 10.07);

    private static StpSymbol MakeUnit(string sidc, Affiliation aff, string designator = null) => new()
    {
        Type = "unit",
        SymbolId = sidc,                 // 2525C/legacy code; stored on Sidc.Legacy
        Affiliation = aff,
        Designator1 = designator,
        Location = new Location
        {
            Type = "point",
            Shape = "point",
            Coords = new List<LatLon> { Center },
            Centroid = Center
        },
        Geometry = "point"
    };

    private static async Task<StpRecognizer> ConnectAsync(string agent, params Action<StpRecognizer>[] hooks)
    {
        var connector = new StpJsonRpcConnector(url: TestStpUrl);
        var recognizer = new StpRecognizer(connector);
        recognizer.OnStpMessage += (level, msg) => TestContext.Out.WriteLine($"[STP {level}] {msg}");
        foreach (var h in hooks) h(recognizer);
        // Unique session per test for isolation. (The default no-session / machine-id path
        // is validated separately by JsonRpcIntegrationSmokeTests.Agent_ConnectAndRegister_Succeeds.)
        string session = "smoke-" + Guid.NewGuid().ToString("N").Substring(0, 12);
        string assigned = await recognizer.ConnectAndRegisterAsync(agent, session: session, secondsToRetry: ConnectTimeoutSec);
        Assert.That(assigned, Is.Not.Null.And.Not.Empty, "connect+register should return a session id");
        Assert.That(recognizer.IsConnected, Is.True);
        recognizer.AdvertiseViewport(TopLeft, BotRight);
        return recognizer;
    }

    /// <summary>
    /// Core parity check: a placed unit comes back with a fully populated structured SIDC
    /// (2525C legacy + 2525D delta/parts + symbol set) and renders via JMSML.
    /// </summary>
    /// <remarks>
    /// Defect A16 regression, against a REAL engine. The engine renamed two affiliation
    /// members on 2026-07-30/31 (assumedfriend -> assumed_friend, suspected -> suspect) and
    /// puts the member NAME on the wire via .ToString(). This SDK carried the old spellings,
    /// and NullSafeStringEnumConverter swallowed the mismatch into null - so these symbols
    /// arrived with NO affiliation at all, silently. The unit tests pin the spelling; this
    /// pins the actual round trip through the engine.
    /// </remarks>
    [TestCase("assumed_friend", Affiliation.assumed_friend, "SAGPUCI----E---")]
    [TestCase("suspect", Affiliation.suspect, "SSGPUCI----E---")]
    public async Task AddUnit_RenamedAffiliation_SurvivesTheRoundTrip(
        string label, Affiliation affiliation, string sidc)
    {
        StpSymbol received = null;
        var added = new ManualResetEventSlim(false);

        using var recognizer = await ConnectAsync($"ParitySmoke_Aff_{label}",
            r => r.OnSymbolAdded += (poid, item, isUndo) =>
            {
                if (item is StpSymbol s && s.Type == "unit") { received = s; added.Set(); }
            });

        recognizer.AddSymbol(MakeUnit(sidc, affiliation));

        Assert.That(added.Wait(EventWaitMs), Is.True, "OnSymbolAdded should fire for the placed unit");
        Assert.That(received, Is.Not.Null);
        TestContext.Out.WriteLine(
            $"sent={affiliation}  received={received.Affiliation?.ToString() ?? "<NULL>"}  sidc={received.CharlieSIDC}");

        Assert.That(received.Affiliation, Is.Not.Null,
            $"affiliation '{label}' came back NULL - the engine's wire spelling did not match this " +
            "SDK's enum and NullSafeStringEnumConverter swallowed it (defect A16)");
        Assert.That(received.Affiliation, Is.EqualTo(affiliation));
    }

    /// <summary>
    /// Stronger form of the A16 regression: send NO affiliation at all and let the engine
    /// derive it from the 2525C SIDC ('A' in position 2 = assumed friend, 'S' = suspect).
    /// The affiliation that comes back is then the ENGINE's own spelling, not an echo of
    /// ours - which is what actually pins the wire contract.
    /// </summary>
    [TestCase("SAGPUCI----E---", Affiliation.assumed_friend)]
    [TestCase("SSGPUCI----E---", Affiliation.suspect)]
    public async Task AddUnit_EngineDerivedAffiliation_UsesEngineSpelling(
        string sidc, Affiliation expected)
    {
        StpSymbol received = null;
        var added = new ManualResetEventSlim(false);

        using var recognizer = await ConnectAsync($"ParitySmoke_Derived_{expected}",
            r => r.OnSymbolAdded += (poid, item, isUndo) =>
            {
                if (item is StpSymbol s && s.Type == "unit") { received = s; added.Set(); }
            });

        // Affiliation deliberately left unset - the engine must supply it.
        var unit = MakeUnit(sidc, default);
        unit.Affiliation = null;
        recognizer.AddSymbol(unit);

        Assert.That(added.Wait(EventWaitMs), Is.True, "OnSymbolAdded should fire");
        Assert.That(received, Is.Not.Null);
        TestContext.Out.WriteLine(
            $"sent=<none> sidc={sidc} -> engine returned affiliation=" +
            $"{received.Affiliation?.ToString() ?? "<NULL>"}");

        Assert.That(received.Affiliation, Is.Not.Null,
            "engine-derived affiliation came back NULL - this SDK's enum does not match the " +
            "spelling the engine puts on the wire (defect A16)");
        Assert.That(received.Affiliation, Is.EqualTo(expected));
    }

    [Test]
    public async Task AddUnit_ReturnsStructuredSidc_AndRenders()
    {
        StpSymbol received = null;
        var added = new ManualResetEventSlim(false);

        using var recognizer = await ConnectAsync("ParitySmoke_Add",
            r => r.OnSymbolAdded += (poid, item, isUndo) =>
            {
                if (item is StpSymbol s && s.Type == "unit") { received = s; added.Set(); }
            });

        recognizer.AddSymbol(MakeUnit("SFGPUCI----E---", Affiliation.friend));   // friendly infantry company

        Assert.That(added.Wait(EventWaitMs), Is.True, "OnSymbolAdded should fire for the placed unit");
        Assert.That(received, Is.Not.Null);

        // --- structured SIDC parity (engine -> SDK) ---
        Assert.That(received.Sidc, Is.Not.Null, "engine must deliver a structured sidc object");
        TestContext.Out.WriteLine($"poid={received.Poid}");
        TestContext.Out.WriteLine($"CharlieSIDC(legacy)={received.CharlieSIDC}  DeltaSIDC={received.DeltaSIDC}  SymbolSet={received.SymbolSet}");
        TestContext.Out.WriteLine($"partA={received.Sidc.PartA} partB={received.Sidc.PartB}");
        TestContext.Out.WriteLine($"affiliation={received.Affiliation} desc=\"{received.Description}\" full=\"{received.FullDescription}\"");

        Assert.That(received.CharlieSIDC, Is.Not.Null.And.Not.Empty, "2525C legacy code should be present");
        Assert.That(received.DeltaSIDC, Is.Not.Null.And.Not.Empty, "2525D delta (partA+partB) should be reconstructed from engine parts");
        Assert.That(received.Sidc.PartA, Is.Not.Null.And.Not.Empty, "partA should be derivable from delta");
        Assert.That(received.SymbolSet, Is.Not.Null.And.Not.Empty, "2525D symbol set should be present");
        Assert.That(received.Affiliation, Is.EqualTo(Affiliation.friend));
        Assert.That(received.FullDescription, Is.Not.Null.And.Not.Empty, "engine should supply a (full) description");

        // --- JMSML rendering (Windows + SVG set) ---
        StpRecognizer.JMSSVGPath = SvgPath;
        Bitmap bmp = received.Bitmap(64, 64);
        if (OperatingSystem.IsWindows() && Directory.Exists(SvgPath))
        {
            Assert.That(bmp, Is.Not.Null, "JMSML should render the engine-supplied symbol");
            Assert.That(bmp!.Width, Is.GreaterThan(0));
            TestContext.Out.WriteLine($"rendered bitmap {bmp.Width}x{bmp.Height}");
        }
        else
        {
            TestContext.Out.WriteLine($"render skipped (Windows+SVG set unavailable); Bitmap returned {(bmp is null ? "null" : "a bitmap")}");
        }

        recognizer.Stop();
    }

    /// <summary>
    /// Full lifecycle: add a unit, update it, delete it - confirming the corresponding
    /// events round-trip against the live engine.
    /// </summary>
    [Test]
    public async Task Symbol_Lifecycle_Add_Update_Delete()
    {
        string addedPoid = null;
        var added = new ManualResetEventSlim(false);
        var modified = new ManualResetEventSlim(false);
        var deleted = new ManualResetEventSlim(false);

        using var recognizer = await ConnectAsync("ParitySmoke_Lifecycle",
            r =>
            {
                r.OnSymbolAdded += (poid, item, isUndo) =>
                {
                    if (addedPoid is null && item is StpSymbol s && s.Type == "unit") { addedPoid = poid; added.Set(); }
                };
                r.OnSymbolModified += (poid, item, isUndo) => { if (poid == addedPoid) modified.Set(); };
                r.OnSymbolDeleted += (poid, isUndo) => { if (poid == addedPoid) deleted.Set(); };
            });

        recognizer.AddSymbol(MakeUnit("SFGPUCI----E---", Affiliation.friend, "A1"));   // friendly infantry company (known-good)
        Assert.That(added.Wait(EventWaitMs), Is.True, "OnSymbolAdded should fire");
        Assert.That(addedPoid, Is.Not.Null.And.Not.Empty);
        TestContext.Out.WriteLine($"added poid={addedPoid}");

        // Update: move the symbol and change its designator.
        var update = MakeUnit("SFGPUCI----E---", Affiliation.friend, "A2");
        update.Poid = addedPoid;
        update.Location.Coords = new List<LatLon> { new(59.10, 10.20) };
        update.Location.Centroid = new(59.10, 10.20);
        recognizer.UpdateSymbol(addedPoid, update);
        Assert.That(modified.Wait(EventWaitMs), Is.True, "OnSymbolModified should fire after UpdateSymbol");
        TestContext.Out.WriteLine($"OnSymbolModified fired for {addedPoid}");

        recognizer.DeleteSymbol(addedPoid);
        Assert.That(deleted.Wait(EventWaitMs), Is.True, "OnSymbolDeleted should fire after DeleteSymbol");
        TestContext.Out.WriteLine($"OnSymbolDeleted fired for {addedPoid}");

        recognizer.Stop();
    }
}
