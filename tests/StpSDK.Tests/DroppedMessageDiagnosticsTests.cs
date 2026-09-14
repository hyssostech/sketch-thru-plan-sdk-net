using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace StpSDK.Tests;

/// <summary>
/// A dispatch handler that discards an engine message must say so.
/// </summary>
/// <remarks>
/// <para>
/// 22 handlers return early when a payload is not what they expect. Until
/// recently none of them reported it, and the cost was concrete: the enum
/// forward-compatibility defect presented as a <c>TaskAdded</c> that simply
/// never arrived - no error, no warning, nothing. From outside the SDK that is
/// indistinguishable from the engine not sending the event.
/// </para>
/// <para>
/// The diagnostics were added with no test proving one is ever EMITTED - the
/// suite only proved nothing had broken. These tests close that: they assert
/// the warning actually reaches <c>OnStpMessage</c>, names the handler that
/// dropped, and names the field that was missing. Without them, deleting the
/// whole diagnostic would still leave the suite green.
/// </para>
/// </remarks>
[TestFixture]
public class DroppedMessageDiagnosticsTests
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

    [Test]
    public void TaskAdded_WithNoAlternates_ReportsTheDrop()
    {
        bool dispatched = false;
        _recognizer.OnTaskAdded += (_, _, _, _) => dispatched = true;

        _connector.SimulateMessage(@"{
            ""method"": ""TaskAdded"",
            ""params"": { ""poid"": ""task-1"", ""alternates"": [], ""taskPoids"": [] }
        }");

        Assert.That(dispatched, Is.False, "an empty alternates list is still a drop");

        var warning = _messages.FirstOrDefault(m => m.Level == StpRecognizer.StpMessageLevel.Warning);
        Assert.That(warning.Message, Is.Not.Null, "the drop must be reported, not silent");
        Assert.Multiple(() =>
        {
            Assert.That(warning.Message, Does.Contain("TaskAdded"),
                        "the message must name the event that was lost");
            Assert.That(warning.Message, Does.Contain("alternates"),
                        "the message must name the field that was missing");
            Assert.That(warning.Message, Does.Contain("discarded"));
        });
    }

    [Test]
    public void SymbolModified_WithNoSymbol_ReportsTheDrop()
    {
        bool dispatched = false;
        _recognizer.OnSymbolModified += (_, _, _) => dispatched = true;

        _connector.SimulateMessage(@"{
            ""method"": ""SymbolModified"",
            ""params"": { ""poid"": ""sym-1"" }
        }");

        Assert.That(dispatched, Is.False);

        var warning = _messages.FirstOrDefault(m => m.Level == StpRecognizer.StpMessageLevel.Warning);
        Assert.That(warning.Message, Is.Not.Null, "the drop must be reported, not silent");
        Assert.That(warning.Message, Does.Contain("SymbolModified"));
        Assert.That(warning.Message, Does.Contain("symbol"));
    }

    /// <summary>
    /// The counter-case. A diagnostic that fires on a GOOD message would be
    /// noise, and noise is what gets a channel ignored - which would put us back
    /// where we started.
    /// </summary>
    [Test]
    public void AWellFormedMessage_ReportsNothing()
    {
        bool dispatched = false;
        _recognizer.OnTaskAdded += (_, _, _, _) => dispatched = true;

        _connector.SimulateMessage(@"{
            ""method"": ""TaskAdded"",
            ""params"": {
                ""poid"": ""task-1"",
                ""alternates"": [ { ""fsTYPE"": ""task"", ""poid"": ""task-1"", ""name"": ""Ambush"" } ],
                ""taskPoids"": [""tg-1""],
                ""isUndo"": false
            }
        }");

        Assert.That(dispatched, Is.True, "this message is well formed and must dispatch");
        Assert.That(_messages.Any(m => m.Level == StpRecognizer.StpMessageLevel.Warning), Is.False,
                    "a well-formed message must produce no drop warning");
    }
}
