using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace StpSDK.Tests;

/// <summary>
/// STP-1092, against a real engine: the text <see cref="StpRecognizer.GetCoaContentAsync"/> and
/// <see cref="StpRecognizer.GetTaskOrgContentAsync"/> return is the text the matching import takes
/// back. Before the fix both getters ran JToken.Parse on the engine's object-set text and threw on
/// every real answer; the unit tests fed JSON, a shape the engine never sends. Category "SmokeTest"
/// is excluded from CI; run with a live engine: dotnet test --filter Category=SmokeTest
/// </summary>
[TestFixture]
[Category("SmokeTest")]
public class LiveContentRoundTripSmokeTests
{
    private static string NewPoid() => "idSMOKE" + Guid.NewGuid().ToString("N").Substring(0, 20).ToUpperInvariant();

    private static async Task<StpRecognizer> ConnectWithScenarioAsync()
    {
        var recognizer = new StpRecognizer(new StpJsonRpcConnector(url: LiveParitySmokeTests.TestStpUrl));
        recognizer.OnStpMessage += (level, msg) => TestContext.Out.WriteLine($"[STP {level}] {msg}");
        string session = "smoke-" + Guid.NewGuid().ToString("N").Substring(0, 12);
        await recognizer.ConnectAndRegisterAsync("ContentRoundTripSmoke", session: session, secondsToRetry: 15);
        await recognizer.CreateNewScenarioAsync("content-round-trip");
        await recognizer.JoinScenarioSessionAsync();
        return recognizer;
    }

    [Test]
    public async Task CoaContent_RoundTrips_ThroughDeleteAndImport()
    {
        var recognizer = await ConnectWithScenarioAsync();
        string poid = NewPoid();
        string seed = $"object_set([\n[fsTYPE: coa,poid: {poid},name: 'Smoke COA',type: friend,creator_role: 'S3']\n])\n";
        Assert.That(await recognizer.ImportCoaContentAsync(seed), Is.EqualTo(poid));

        string content = await recognizer.GetCoaContentAsync(poid);
        TestContext.Out.WriteLine(content);
        Assert.That(content, Does.StartWith("object_set(").And.Contain(poid).And.Contain("Smoke COA"));

        await recognizer.DeleteCoaAsync(poid);
        Assert.That(await recognizer.ImportCoaContentAsync(content), Is.EqualTo(poid), "the returned text must import back");
        Assert.That(await recognizer.GetCoaContentAsync(poid), Does.Contain("Smoke COA"));
    }

    [Test]
    public async Task TaskOrgContent_ImportsBack()
    {
        var recognizer = await ConnectWithScenarioAsync();
        string poid = NewPoid();
        string seed = $"object_set([\n[fsTYPE: task_org,poid: {poid},name: 'Smoke TO',affiliation: friend]\n])\n";
        Assert.That(await recognizer.ImportTaskOrgContentAsync(seed), Is.EqualTo(poid));

        string content = await recognizer.GetTaskOrgContentAsync(poid, default);
        TestContext.Out.WriteLine(content);
        Assert.That(content, Does.StartWith("object_set(").And.Contain(poid).And.Contain("Smoke TO"));

        Assert.That(await recognizer.ImportTaskOrgContentAsync(content), Is.EqualTo(poid), "the returned text must import back");
    }
}
