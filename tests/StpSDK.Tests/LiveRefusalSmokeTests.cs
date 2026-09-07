using NUnit.Framework;
using StpSDK;
using System;
using System.Threading.Tasks;

namespace StpSDK.Tests;

/// <summary>
/// End-to-end proof that a refused call is legible THROUGH THIS SDK, against a live engine.
/// Unit tests pin the connector's own behaviour; only this shows the engine's reason text
/// surviving the whole path. Point STP_SDK_TEST_URL at a container (never the user's own
/// 9599) and run with --filter Category=SmokeTest.
/// </summary>
[TestFixture]
[Category("SmokeTest")]
public class LiveRefusalSmokeTests
{
    private static string Url =>
        Environment.GetEnvironmentVariable("STP_SDK_TEST_URL") ?? "ws://localhost:9599";

    [Test]
    public async Task UndispatchedMethod_ThrowsWithAReadableMessage()
    {
        var connector = new StpJsonRpcConnector(url: Url);
        var recognizer = new StpRecognizer(connector);
        await recognizer.ConnectAndRegisterAsync("RefusalSmoke");

        // CONTROL: a dispatched request must work, or a blanket failure would look like a pass.
        bool active = await recognizer.HasActiveScenarioAsync();
        Assert.That(active, Is.TypeOf<bool>(), "control: a dispatched request must answer");

        // GetCoaContent has no dispatch arm on any release line.
        var ex = Assert.ThrowsAsync<StpException>(async () => await recognizer.GetCoaContentAsync("coa-1"));

        Assert.That(ex.Message, Is.Not.Empty, "a refusal must not surface as an empty message");
        Assert.That(ex.Message, Does.Contain("GetCoaContent").Or.Contain("No handler").IgnoreCase);
        TestContext.WriteLine("refusal message: " + ex.Message);
    }
}
