using System;
using Newtonsoft.Json;
using NUnit.Framework;

namespace StpSDK.Tests;

/// <summary>
/// Regression tests for the affiliation wire contract (defect A16).
///
/// The engine declares <c>Affiliation</c> in <c>SDK/StpSDK/Models/SymbolIdCode.cs</c> and the
/// WebSocketsBridge serialises enums with <c>.ToString()</c>, so the C# member NAMES reach the
/// JSON wire verbatim. The engine renamed two of them on 2026-07-30/31
/// (<c>assumedfriend</c> -&gt; <c>assumed_friend</c>, <c>suspected</c> -&gt; <c>suspect</c>) and
/// the JS SDK followed in <c>bc3912dd</c>; this SDK did not.
///
/// The failure mode is SILENT: <see cref="StpSymbol.Affiliation"/> is decorated with
/// <c>NullSafeStringEnumConverter</c>, whose ReadJson is <c>try { ... } catch { return null; }</c>,
/// so an unrecognised name is swallowed and the affiliation simply disappears. Nothing threw,
/// nothing logged, and no test asserted an affiliation value - which is why this survived.
/// </summary>
[TestFixture]
public class AffiliationWireTests
{
    // Exactly the base spellings the engine puts on the wire.
    [TestCase("pending")]
    [TestCase("unknown")]
    [TestCase("assumed_friend")]
    [TestCase("friend")]
    [TestCase("neutral")]
    [TestCase("suspect")]
    [TestCase("hostile")]
    public void Affiliation_EngineWireSpelling_SurvivesDeserialisation(string wire)
    {
        string json = $@"{{ ""fsTYPE"": ""unit"", ""poid"": ""p1"", ""affiliation"": ""{wire}"" }}";

        var sym = JsonConvert.DeserializeObject<StpSymbol>(json);

        Assert.That(sym, Is.Not.Null);
        Assert.That(sym.Affiliation, Is.Not.Null,
            $"affiliation '{wire}' was silently dropped to null - NullSafeStringEnumConverter " +
            "swallowed the mismatch instead of surfacing it");
        Assert.That(sym.Affiliation.ToString(), Is.EqualTo(wire),
            $"affiliation '{wire}' did not round-trip to the same wire spelling");
    }

    /// <summary>
    /// Outbound direction: what this SDK sends back must be spelled the way the engine expects.
    /// </summary>
    [Test]
    public void Affiliation_SerialisesBackToTheEngineSpelling()
    {
        // Ordinal 2 rather than the named member, so this file compiles both before and after
        // the rename and can therefore demonstrate the defect as a genuine RED test.
        var sym = new StpSymbol { Type = "unit", Affiliation = (Affiliation)2 };

        string json = JsonConvert.SerializeObject(sym);

        Assert.That(json, Does.Contain("\"assumed_friend\""));
        Assert.That(json, Does.Not.Contain("\"assumedfriend\""));
    }

    /// <summary>
    /// The same 2026-07-30/31 rename also moved `echelon` and four `modifier` members.
    /// </summary>
    [TestCase("echelon", "army_group")]
    [TestCase("modifier", "feint_dummy")]
    [TestCase("modifier", "feint_dummy_hq")]
    [TestCase("modifier", "feint_dummy_task_force")]
    [TestCase("modifier", "feint_dummy_task_force_hq")]
    [TestCase("modifier", "installation")]
    [TestCase("modifier", "task_force")]
    public void SymbologyEnums_EngineWireSpelling_SurviveDeserialisation(string prop, string wire)
    {
        string json = $@"{{ ""fsTYPE"": ""unit"", ""poid"": ""p1"", ""{prop}"": ""{wire}"" }}";

        var sym = JsonConvert.DeserializeObject<StpSymbol>(json);
        object actual = prop == "echelon" ? sym.Echelon : (object)sym.Modifier;

        Assert.That(actual, Is.Not.Null, $"{prop} '{wire}' was silently dropped to null");
        Assert.That(actual.ToString(), Is.EqualTo(wire),
            $"{prop} '{wire}' did not round-trip to the same wire spelling");
    }

    /// <summary>
    /// A full sweep of every symbology enum against the engine (2026-09-04) found three MORE
    /// drifted beyond the ones the changelog named. Same silent failure: the engine emits a
    /// member this SDK does not declare, and NullSafeStringEnumConverter nulls it.
    /// These cases pin the members that were missing.
    /// </summary>
    [TestCase("CANALIZE")]
    [TestCase("CONTAIN")]
    [TestCase("CONTROL")]
    [TestCase("COUNTERRECONNAISSANCE")]
    [TestCase("DEMONSTRATING")]
    [TestCase("DISENGAGE")]
    [TestCase("EXFILTRATE")]
    [TestCase("INTERDICT")]
    [TestCase("ISOLATE")]
    [TestCase("REDUCE")]
    [TestCase("SUPPRESS")]
    public void TaskWhat_EngineMember_IsDeclared(string wire)
    {
        Assert.That(Enum.TryParse<TaskWhat>(wire, out var parsed), Is.True,
            $"engine TaskWhat '{wire}' is not declared here - it would arrive as null");
        Assert.That(parsed.ToString(), Is.EqualTo(wire));
    }

    [TestCase("non_military_sea")]
    [TestCase("non_submarine_subsurface")]
    [TestCase("sof_naval")]
    [TestCase("sof_support")]
    public void Branch_EngineMember_IsDeclared(string wire)
    {
        Assert.That(Enum.TryParse<Branch>(wire, out var parsed), Is.True,
            $"engine Branch '{wire}' is not declared here - it would arrive as null");
        Assert.That(parsed.ToString(), Is.EqualTo(wire));
    }

    [Test]
    public void CodingScheme_Mapping_IsDeclared()
    {
        Assert.That(Enum.TryParse<CodingScheme>("mapping", out var parsed), Is.True,
            "engine CodingScheme 'mapping' is not declared here - it would arrive as null");
        Assert.That(parsed.ToString(), Is.EqualTo("mapping"));
    }

    /// <summary>
    /// The engine deliberately KEPT the old spellings for the exercise variants, so these must
    /// NOT be "corrected" to match the base members.
    /// </summary>
    [TestCase("exerciseassumedfriend")]
    [TestCase("exercisesuspected")]
    public void Affiliation_ExerciseVariants_KeepTheOldSpelling(string wire)
    {
        string json = $@"{{ ""fsTYPE"": ""unit"", ""poid"": ""p1"", ""affiliation"": ""{wire}"" }}";

        var sym = JsonConvert.DeserializeObject<StpSymbol>(json);

        Assert.That(sym.Affiliation, Is.Not.Null, $"exercise affiliation '{wire}' was dropped");
        Assert.That(sym.Affiliation.ToString(), Is.EqualTo(wire));
    }
}
