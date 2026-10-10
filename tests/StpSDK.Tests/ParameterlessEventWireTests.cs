using NUnit.Framework;
using StpSDK;
using System.Collections.Generic;
using System.Linq;

namespace StpSDK.Tests;

/// <summary>
/// STP-1067. Events that carry no payload arrive with the <c>params</c> key ABSENT, and must
/// still fire.
/// </summary>
/// <remarks>
/// <para>
/// The engine bridge relays a payload-less event with <c>SendEvent(name, null)</c>
/// (WebSocketsBridge <c>StpJsonClient.cs</c>: InkProcessed - relayed from both SketchIntegrated
/// and SketchDiscarded - SpeechDiscarded and NewScenario), and <c>JsonMessage.WriteJson</c>
/// writes the <c>params</c> property only when it is non-null. So the wire carries
/// <c>{"method":"NewScenario"}</c> - no <c>params</c> at all, not <c>"params":{}</c>. The public
/// contract says the same (sketch-thru-plan-api.json, <c>x-stpEvents</c>: "On the wire the params
/// key is ABSENT").
/// </para>
/// <para>
/// The dispatcher returned early whenever <c>params</c> was missing, so OnNewScenario,
/// OnInkProcessed and OnSpeechDiscarded never fired against a real engine. The existing dispatch
/// tests (RecognizerDispatchTests 33-41) fed <c>"params": {}</c>, a shape the engine never
/// sends, which is why the suite was green. Those tests stay - <c>{}</c> must keep working - and
/// these pin the shape the engine actually emits.
/// </para>
/// </remarks>
[TestFixture]
public class ParameterlessEventWireTests
{
    private StubConnector _connector;
    private StpRecognizer _recognizer;
    private List<(StpRecognizer.StpMessageLevel Level, string Message)> _messages;

    [SetUp]
    public void Setup()
    {
        _connector = new StubConnector();
        _recognizer = new StpRecognizer(_connector);
        _messages = new List<(StpRecognizer.StpMessageLevel, string)>();
        _recognizer.OnStpMessage += (level, message) => _messages.Add((level, message));
    }

    [TearDown]
    public void TearDown()
    {
        _recognizer.Dispose();
        _connector?.Dispose();
    }

    /// <summary>Subscribe to the named payload-less event; the returned func reports whether it fired.</summary>
    private System.Func<bool> Subscribe(string method)
    {
        bool fired = false;
        switch (method)
        {
            case "InkProcessed": _recognizer.OnInkProcessed += () => fired = true; break;
            case "SpeechDiscarded": _recognizer.OnSpeechDiscarded += () => fired = true; break;
            case "NewScenario": _recognizer.OnNewScenario += () => fired = true; break;
            case "SketchIntegrated": _recognizer.OnSketchIntegrated += () => fired = true; break;
            case "SketchDiscarded": _recognizer.OnSketchDiscarded += () => fired = true; break;
            case "SpeechIntegrated": _recognizer.OnSpeechIntegrated += () => fired = true; break;
            case "Shutdown": _recognizer.OnShutdown += () => fired = true; break;
            default: Assert.Fail($"test does not know event {method}"); break;
        }
        return () => fired;
    }

    // The first three are the events the engine bridge actually relays with no params today.
    // The other four are payload-less in this SDK too; the bridge does not relay them at present,
    // but if it ever does it will be through the same SendEvent(name, null) path.
    [TestCase("InkProcessed")]
    [TestCase("SpeechDiscarded")]
    [TestCase("NewScenario")]
    [TestCase("SketchIntegrated")]
    [TestCase("SketchDiscarded")]
    [TestCase("SpeechIntegrated")]
    [TestCase("Shutdown")]
    public void ParamsKeyAbsent_StillFires(string method)
    {
        var fired = Subscribe(method);

        // Byte-for-byte what JsonMessage.WriteJson produces for SendEvent(method, null).
        _connector.SimulateMessage("{\"method\":\"" + method + "\"}");

        Assert.That(fired(), Is.True, $"{method} with the params key absent is the engine's shape and must fire");
        Assert.That(_messages.Where(m => m.Level is StpRecognizer.StpMessageLevel.Error or StpRecognizer.StpMessageLevel.Warning), Is.Empty,
                    "a payload-less event is not a malformed message");
    }

    [TestCase("InkProcessed")]
    [TestCase("SpeechDiscarded")]
    [TestCase("NewScenario")]
    public void ParamsExplicitNull_StillFires(string method)
    {
        var fired = Subscribe(method);

        _connector.SimulateMessage("{\"method\":\"" + method + "\",\"params\":null}");

        Assert.That(fired(), Is.True);
    }

    [TestCase("InkProcessed")]
    [TestCase("SpeechDiscarded")]
    [TestCase("NewScenario")]
    public void ParamsEmptyObject_StillFires(string method)
    {
        var fired = Subscribe(method);

        _connector.SimulateMessage("{\"method\":\"" + method + "\",\"params\":{}}");

        Assert.That(fired(), Is.True, "the {} shape the older tests used must keep working");
    }

    /// <summary>
    /// The other side of the fix: an event that NEEDS a payload and arrives without one is still
    /// discarded - but it now says so, as every other discard in the dispatcher does since 0.6.0.
    /// It used to vanish in silence.
    /// </summary>
    [Test]
    public void PayloadEvent_WithParamsAbsent_IsDiscardedAndReported()
    {
        bool fired = false;
        _recognizer.OnSymbolAdded += (_, _, _) => fired = true;

        _connector.SimulateMessage("{\"method\":\"SymbolAdded\"}");

        Assert.That(fired, Is.False);
        var warning = _messages.FirstOrDefault(m => m.Level == StpRecognizer.StpMessageLevel.Warning);
        Assert.That(warning.Message, Is.Not.Null, "the drop must be reported, not silent");
        Assert.Multiple(() =>
        {
            Assert.That(warning.Message, Does.Contain("SymbolAdded"));
            Assert.That(warning.Message, Does.Contain("params"));
            Assert.That(warning.Message, Does.Contain("discarded"));
        });
    }

    /// <summary>An event this build does not know stays a Debug note, params or not.</summary>
    [Test]
    public void UnknownEvent_WithParamsAbsent_IsNotAWarning()
    {
        _connector.SimulateMessage("{\"method\":\"SomeFutureEvent\"}");

        Assert.That(_messages.Where(m => m.Level is StpRecognizer.StpMessageLevel.Error or StpRecognizer.StpMessageLevel.Warning), Is.Empty);
        Assert.That(_messages.Any(m => m.Level == StpRecognizer.StpMessageLevel.Debug
                                       && m.Message.Contains("SomeFutureEvent")), Is.True);
    }
}
