using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using StpSDK.Validation;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace StpSDK.Tests;

/// <summary>
/// STP-1064 against a real engine: one call the engine refuses and one it carries out, each checked
/// before it is sent and explained after it by <see cref="StpCallValidations"/>, from the raw event
/// stream. Category "SmokeTest" is excluded from CI; point STP_SDK_TEST_URL at a container and run
/// with --filter Category=SmokeTest.
/// </summary>
[TestFixture]
[Category("SmokeTest")]
public class LiveCallValidationSmokeTests
{
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(15);

    private sealed class Session : IDisposable
    {
        public readonly StpJsonRpcConnector Connector = new(url: LiveParitySmokeTests.TestStpUrl);
        public readonly StpRecognizer Recognizer;
        private readonly object _gate = new();
        private StpCallWatch? _watch;
        private TaskCompletionSource<bool> _settled = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Session()
        {
            Recognizer = new StpRecognizer(Connector);
            // Handlers make the recognizer subscribe to these events; the watch reads the raw stream.
            Recognizer.OnStpMessage += (level, msg) => TestContext.Out.WriteLine($"[STP {level}] {msg}");
            Recognizer.OnCoaAdded += (_, _, _) => { };
            Connector.OnMessage += OnMessage;
        }

        private void OnMessage(string json)
        {
            var msg = JObject.Parse(json);
            string? name = (string?)msg["method"];
            if (name == null || name == "RequestResponse") return;
            lock (_gate)
            {
                if (_watch != null && _watch.Observe(name, msg["params"], DateTimeOffset.UtcNow)) _settled.TrySetResult(true);
            }
        }

        public async Task<StpCallReport> SendAsync(string method, JObject p)
        {
            var watch = StpCallValidations.Default.Watch(method, p);
            lock (_gate)
            {
                _watch = watch;
                _settled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
            var clock = Stopwatch.StartNew();
            Connector.Send(JsonConvert.SerializeObject(new { method, @params = p }));
            await Task.WhenAny(_settled.Task, Task.Delay(Wait));
            lock (_gate)
            {
                _watch = null;
                return watch.Report(StpCallEnd.Inform(clock.Elapsed, Wait));
            }
        }

        public void Dispose()
        {
            Connector.OnMessage -= OnMessage;
            Recognizer.Dispose();
            Connector.Dispose();
        }
    }

    /// <summary>
    /// HasActiveScenario, read from the raw answer. StpRecognizer.HasActiveScenarioAsync compares the
    /// connector's text with "true", but the connector stringifies the JSON boolean as "True", so it
    /// answers false against a real engine; that defect is outside STP-1064 and is reported separately.
    /// </summary>
    private static async Task<bool> HasActiveScenarioAsync(Session s)
    {
        const int Cookie = 1_064_001;
        string raw = await s.Connector.SendRequestAsync(JsonConvert.SerializeObject(new { method = "HasActiveScenario", @params = new { }, id = Cookie }), Cookie);
        return string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<Session> ConnectWithScenarioAsync()
    {
        var s = new Session();
        string session = "smoke-" + Guid.NewGuid().ToString("N").Substring(0, 12);
        await s.Recognizer.ConnectAndRegisterAsync("CallValidationSmoke", session: session, secondsToRetry: 15);
        await s.Recognizer.CreateNewScenarioAsync("call-validation-smoke");
        await s.Recognizer.JoinScenarioSessionAsync();
        return s;
    }

    [Test]
    public async Task ACallTheEngineCarriesOut_IsDone()
    {
        using var s = await ConnectWithScenarioAsync();
        var p = JObject.FromObject(new { coa = new { fsTYPE = "coa", name = "Validation Smoke " + Guid.NewGuid().ToString("N").Substring(0, 6) } });
        var check = StpCallValidations.Default.Validate("AddCoa", p, new StpCallContext { ActiveScenario = await HasActiveScenarioAsync(s) });
        Assert.That(check.IsRefused, Is.False, check.Reason);

        var report = await s.SendAsync("AddCoa", p);
        TestContext.Out.WriteLine($"{report.Outcome}: {report.Summary}");
        Assert.That(report.Outcome, Is.EqualTo(StpCallOutcome.Done), report.Summary);
    }

    [Test]
    public async Task ACallTheEngineRefuses_IsNotDone_WithTheEnginesReason()
    {
        using var s = await ConnectWithScenarioAsync();
        // The params check cannot see this one (timeout is present, just not a number): the engine refuses it.
        var p = JObject.FromObject(new { timeout = "not a number" });
        var check = StpCallValidations.Default.Validate("ChangeTimeOut", p, new StpCallContext { ActiveScenario = await HasActiveScenarioAsync(s) });
        Assert.That(check.IsRefused, Is.False, check.Reason);

        var report = await s.SendAsync("ChangeTimeOut", p);
        TestContext.Out.WriteLine($"{report.Outcome}: {report.Summary}");
        Assert.That(report.Outcome, Is.EqualTo(StpCallOutcome.NotDone), report.Summary);
        Assert.That(report.Refusal, Does.Contain("'ChangeTimeOut'"));
    }

    [Test]
    public async Task ACallWithoutAScenario_IsRefusedBeforeItIsSent()
    {
        using var s = new Session();
        string session = "smoke-" + Guid.NewGuid().ToString("N").Substring(0, 12);
        await s.Recognizer.ConnectAndRegisterAsync("CallValidationSmoke", session: session, secondsToRetry: 15);
        bool active = await HasActiveScenarioAsync(s);
        Assume.That(active, Is.False, "a fresh session has no scenario");
        var check = StpCallValidations.Default.Validate("AddCoa", JObject.FromObject(new { coa = new { name = "X" } }), new StpCallContext { ActiveScenario = active });
        Assert.That(check.FailedCheck?.Check, Is.EqualTo(StpCheck.Context), check.Reason);
    }
}
