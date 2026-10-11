#nullable enable
using System;
using System.Collections.Generic;

namespace StpSDK.Validation;

/// <summary>What a call did, as V10 (diff intended vs actual) judges it from the event stream.</summary>
public enum StpCallOutcome
{
    /// <summary>Everything the call asked for arrived (or a request with no promised event was answered with success).</summary>
    Done,
    /// <summary>Some of it arrived; <see cref="StpCallReport.Missing"/> names the rest.</summary>
    Partial,
    /// <summary>Refused, or handled (answered, acknowledged) without the expected events.</summary>
    NotDone,
    /// <summary>The engine did something else than asked (e.g. TaskModified instead of TaskAdded).</summary>
    Other,
    /// <summary>No answer and no event within the wait (a timeout, a closed connection).</summary>
    Unknown,
    /// <summary>Nothing can confirm it: no event is promised, or a filed engine issue holds the events back.</summary>
    Unverifiable,
}

/// <summary>How a call goes on the wire.</summary>
public enum StpCallWire
{
    /// <summary>Fire-and-forget: no answer.</summary>
    Inform,
    /// <summary>A request the engine answers (success or refusal).</summary>
    Request,
}

/// <summary>How the call itself ended, as the caller saw it.</summary>
public sealed class StpCallEnd
{
    private StpCallEnd(StpCallWire wire, bool answered, string? refused, string? failure, TimeSpan waited, TimeSpan timeout)
    {
        Wire = wire;
        Answered = answered;
        Refused = refused;
        Failure = failure;
        Waited = waited;
        Timeout = timeout;
    }

    /// <summary>Inform or request.</summary>
    public StpCallWire Wire { get; }

    /// <summary>A request the engine answered with success.</summary>
    public bool Answered { get; }

    /// <summary>A request the engine refused (success:false), with its reason.</summary>
    public string? Refused { get; }

    /// <summary>A request that got no answer (timeout, closed connection), with what happened.</summary>
    public string? Failure { get; }

    /// <summary>How long the caller waited for events after sending.</summary>
    public TimeSpan Waited { get; }

    /// <summary>How long the caller was prepared to wait for the expected events.</summary>
    public TimeSpan Timeout { get; }

    /// <summary>A fire-and-forget call, sent.</summary>
    public static StpCallEnd Inform(TimeSpan waited, TimeSpan timeout) =>
        new(StpCallWire.Inform, false, null, null, waited, timeout);

    /// <summary>A request the engine answered with success.</summary>
    public static StpCallEnd AnsweredRequest(TimeSpan waited, TimeSpan timeout) =>
        new(StpCallWire.Request, true, null, null, waited, timeout);

    /// <summary>A request the engine refused (success:false; <see cref="StpException"/>), with its reason.</summary>
    public static StpCallEnd RefusedRequest(string reason, TimeSpan waited, TimeSpan timeout) =>
        new(StpCallWire.Request, false, reason ?? throw new ArgumentNullException(nameof(reason)), null, waited, timeout);

    /// <summary>A request that got no answer: a timeout, a closed connection. The outcome is then unknown, unless events say otherwise.</summary>
    public static StpCallEnd FailedRequest(string failure, TimeSpan waited, TimeSpan timeout) =>
        new(StpCallWire.Request, false, null, failure ?? throw new ArgumentNullException(nameof(failure)), waited, timeout);
}

/// <summary>What an observed event was to the call.</summary>
public enum StpEventRole
{
    /// <summary>It proves part of what the call asked for.</summary>
    Expected,
    /// <summary>A further event on an object an expected event already matched (AddTask's TaskModified after TaskAdded).</summary>
    FollowOn,
    /// <summary>It shows the engine handled the call without proving it was done (InkProcessed).</summary>
    Ack,
    /// <summary>It shows the engine did something else than asked.</summary>
    Other,
    /// <summary>The engine's refusal of the call (an StpMessage naming it).</summary>
    Refusal,
}

/// <summary>An event, by name and the poid it is about.</summary>
public sealed class StpEventRef
{
    internal StpEventRef(string @event, string? poid)
    {
        Event = @event;
        Poid = poid;
    }

    /// <summary>The event name.</summary>
    public string Event { get; }

    /// <summary>The poid it is about, when it names one.</summary>
    public string? Poid { get; }

    /// <inheritdoc/>
    public override string ToString() => Poid != null ? $"{Event} for {Poid}" : Event;
}

/// <summary>An event that bore on the call, and what it was to it.</summary>
public sealed class StpReportedEvent
{
    internal StpReportedEvent(StpEvent e, string? poid, StpEventRole role, string? fulfils, string? message)
    {
        Event = e.Name;
        Poid = poid;
        At = e.At;
        As = role;
        Fulfils = fulfils;
        Message = message;
    }

    /// <summary>The event name.</summary>
    public string Event { get; }

    /// <summary>The poid it is about, when it names one.</summary>
    public string? Poid { get; }

    /// <summary>When it arrived.</summary>
    public DateTimeOffset At { get; }

    /// <summary>What it was to the call.</summary>
    public StpEventRole As { get; }

    /// <summary>The expected item it satisfied (<see cref="StpEventRole.Expected"/>).</summary>
    public string? Fulfils { get; }

    /// <summary>The refusal's message (<see cref="StpEventRole.Refusal"/>).</summary>
    public string? Message { get; }
}

/// <summary>The V10 report for one call: the outcome, one line for the reader, and the evidence.</summary>
public sealed class StpCallReport
{
    internal StpCallReport(StpCallOutcome outcome, string summary, StpCallEnd end)
    {
        Outcome = outcome;
        Summary = summary;
        End = end;
    }

    /// <summary>The outcome.</summary>
    public StpCallOutcome Outcome { get; }

    /// <summary>
    /// True when the call did not do what it asked, or nobody knows: <see cref="StpCallOutcome.NotDone"/>,
    /// <see cref="StpCallOutcome.Other"/> or <see cref="StpCallOutcome.Unknown"/>.
    /// </summary>
    public bool IsError => Outcome == StpCallOutcome.NotDone || Outcome == StpCallOutcome.Other || Outcome == StpCallOutcome.Unknown;

    /// <summary>One line for the reader; a partial outcome leads with what is missing.</summary>
    public string Summary { get; }

    /// <summary>What the call was expected to produce.</summary>
    public IReadOnlyList<string> Expected { get; internal set; } = Array.Empty<string>();

    /// <summary>The events that bore on the call, in arrival order.</summary>
    public IReadOnlyList<StpReportedEvent> Observed { get; internal set; } = Array.Empty<StpReportedEvent>();

    /// <summary>The expected items that did not arrive.</summary>
    public IReadOnlyList<string> Missing { get; internal set; } = Array.Empty<string>();

    /// <summary>Events that show the engine did something else.</summary>
    public IReadOnlyList<StpEventRef> Unexpected { get; internal set; } = Array.Empty<StpEventRef>();

    /// <summary>The engine's refusal (a request answered success:false, or a refusal StpMessage).</summary>
    public string? Refusal { get; internal set; }

    /// <summary>The filed engine issue that makes the outcome unverifiable.</summary>
    public string? KnownIssue { get; internal set; }

    /// <summary>How many events matched nothing the call expected (another client's work, or a side effect).</summary>
    public int UnrelatedEvents { get; internal set; }

    /// <summary>The first of those (at most <see cref="StpCallWatch.MaxAlsoSeen"/>): after a destructive call, its side effects.</summary>
    public IReadOnlyList<StpEventRef> AlsoSeen { get; internal set; } = Array.Empty<StpEventRef>();

    /// <summary>How the call ended.</summary>
    public StpCallEnd End { get; }

    /// <inheritdoc/>
    public override string ToString() => $"{Outcome}: {Summary}";
}
