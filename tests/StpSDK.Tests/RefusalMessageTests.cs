using Newtonsoft.Json.Linq;
using NUnit.Framework;
using StpSDK;
using System;
using System.Reflection;

namespace StpSDK.Tests;

/// <summary>
/// A refused request must arrive with something a developer can read.
///
/// The engine answers a method it cannot dispatch with
/// <c>RequestResponse { success: false, result: null }</c>. The connector built its exception with
/// <c>Result?.ToString() ?? "Request failed"</c>, but <c>Result</c> is a <see cref="JToken"/>: a
/// JSON null deserialises to a token of type Null, NOT to a C# null, so the fallback never fired
/// and <c>ToString()</c> on that token returns the empty string. Every refusal therefore surfaced
/// as an StpException with an EMPTY message - the failure mode that hid nineteen undispatched
/// methods for months.
/// </summary>
[TestFixture]
public class RefusalMessageTests
{
    // The connector's message pump is private; drive it the way a socket would.
    private static string BuildRefusal(string resultJson) =>
        "{\"method\":\"RequestResponse\",\"params\":{\"cookie\":1,\"success\":false,\"result\":" + resultJson + "}}";

    private static string MessageFor(string resultJson)
    {
        // Never connects: the message pump is driven directly, so the url is inert. It must NOT
        // be the default 9599, which is a port the user's own STP binds.
        var connector = new StpJsonRpcConnector(null, "ws://127.0.0.1:1/");
        var pending = typeof(StpJsonRpcConnector)
            .GetField("_pendingRequests", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(connector);
        var tcs = new System.Threading.Tasks.TaskCompletionSource<string>();
        pending.GetType().GetMethod("TryAdd").Invoke(pending, new object[] { 1, tcs });

        typeof(StpJsonRpcConnector)
            .GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(connector, new object[] { BuildRefusal(resultJson) });

        var ex = Assert.ThrowsAsync<StpException>(async () => await tcs.Task);
        return ex.Message;
    }

    [Test]
    public void RefusalWithJsonNullResult_HasAReadableMessage()
    {
        string message = MessageFor("null");

        Assert.That(message, Is.Not.Empty, "a refused request must not surface as an empty exception message");
        Assert.That(message, Does.Contain("refused").IgnoreCase.Or.Contain("no result").IgnoreCase);
    }

    [Test]
    public void RefusalWithAnEngineReason_KeepsTheEngineText()
    {
        // Engines from 2026-09 onward name the method in the result; that text must win.
        string message = MessageFor("\"No handler for method 'AddCoa' - the WebSocketsBridge does not dispatch it\"");

        Assert.That(message, Does.Contain("AddCoa"));
        Assert.That(message, Does.Contain("No handler"));
    }

    [Test]
    public void RefusalWithAnAbsentResult_HasAReadableMessage()
    {
        string message = MessageFor("null").Trim();

        Assert.That(message.Length, Is.GreaterThan(10));
    }
}
