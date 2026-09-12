using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using StpSDK;

namespace StpSDK.Tests;

/// <summary>
/// STP-763. ConnectAsync accepted a CancellationToken AND a secondsToRetry and
/// honoured neither: it built a linked CTS, gave it a deadline, and never passed
/// it to anything, so both branches were identical. With IsReconnectionEnabled
/// set, Start() retries indefinitely - so a connect to an unreachable engine
/// hung the caller forever, with no error and no diagnostic. Measured before the
/// fix: a 30-second token was still blocked when the process was killed at 240s.
///
/// These are UNIT tests - they need no engine, only a port with nothing on it,
/// deliberately high and outside both the user's 95xx range and the container
/// range this project tests against.
///
/// The ceilings below are generous on purpose. The distinction being tested is
/// "bounded" versus "unbounded", not a precise duration, and a tight wall-clock
/// assert on a shared CI runner is a bet rather than a test. Against the
/// unfixed code these do not merely exceed the ceiling - they never return at
/// all, which is what [Timeout] converts into a clean failure.
/// </summary>
[TestFixture]
public class ConnectorCancellationTests
{
    // Nothing is bound here. If something ever is, these tests get slower, not
    // wrong - the assertion is on the bound, not on the failure mode.
    private const string Unreachable = "ws://localhost:59987";

    [Test]
    [Timeout(60_000)]
    public async Task ConnectAsync_Unreachable_HonoursSecondsToRetry()
    {
        var connector = new StpJsonRpcConnector(url: Unreachable);
        var sw = Stopwatch.StartNew();

        bool connected = await connector.ConnectAsync(Unreachable, secondsToRetry: 3);

        sw.Stop();
        Assert.That(connected, Is.False, "an unreachable endpoint must not report connected");
        Assert.That(sw.Elapsed, Is.LessThan(TimeSpan.FromSeconds(30)),
            $"secondsToRetry:3 must bound the attempt; it took {sw.Elapsed}");
    }

    [Test]
    [Timeout(60_000)]
    public async Task ConnectAsync_Unreachable_HonoursCallerCancellationToken()
    {
        var connector = new StpJsonRpcConnector(url: Unreachable);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var sw = Stopwatch.StartNew();

        // secondsToRetry deliberately 0: this pins the TOKEN path specifically,
        // which the old code ignored even when it pretended to build one.
        bool connected = await connector.ConnectAsync(Unreachable, secondsToRetry: 0, ct: cts.Token);

        sw.Stop();
        Assert.That(connected, Is.False, "a cancelled connect must not report connected");
        Assert.That(sw.Elapsed, Is.LessThan(TimeSpan.FromSeconds(30)),
            $"the caller's token must bound the attempt; it took {sw.Elapsed}");
    }

    [Test]
    [Timeout(60_000)]
    public async Task ConnectAsync_AlreadyCancelledToken_ReturnsPromptly()
    {
        var connector = new StpJsonRpcConnector(url: Unreachable);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var sw = Stopwatch.StartNew();

        bool connected = await connector.ConnectAsync(Unreachable, secondsToRetry: 0, ct: cts.Token);

        sw.Stop();
        Assert.That(connected, Is.False);
        Assert.That(sw.Elapsed, Is.LessThan(TimeSpan.FromSeconds(15)),
            $"an already-cancelled token must not start a long attempt; it took {sw.Elapsed}");
    }
}
