#nullable enable
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StpSDK.Validation;

/// <summary>An STP event as V10 sees it: its name, its params and when it arrived.</summary>
public sealed class StpEvent
{
    /// <summary>An event.</summary>
    public StpEvent(string name, JToken? @params, DateTimeOffset at)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Params = @params;
        At = at;
    }

    /// <summary>The event name (the JSON-RPC method, e.g. SymbolAdded).</summary>
    public string Name { get; }

    /// <summary>The event's params; null when the engine sent none.</summary>
    public JToken? Params { get; }

    /// <summary>When it arrived.</summary>
    public DateTimeOffset At { get; }
}

/// <summary>One thing a call should produce: one event, one of several, or one per item of a param array.</summary>
public sealed class StpExpectedItem
{
    internal StpExpectedItem(string label, IReadOnlyList<ResolvedMatch> alternatives)
    {
        Label = label;
        Alternatives = alternatives;
    }

    /// <summary>What is expected, for a reader, e.g. <c>CoaAdded where coa.name = "COA 1"</c>.</summary>
    public string Label { get; }

    internal IReadOnlyList<ResolvedMatch> Alternatives { get; }

    internal bool Matches(StpEvent e) => Alternatives.Any(a => a.Matches(e));
}

/// <summary>An event match with its values resolved against the call's params.</summary>
internal sealed class ResolvedMatch
{
    public ResolvedMatch(string @event, IReadOnlyList<KeyValuePair<string, JToken>> match)
    {
        Event = @event;
        Match = match;
    }

    public string Event { get; }
    public IReadOnlyList<KeyValuePair<string, JToken>> Match { get; }

    public bool Matches(StpEvent e) =>
        string.Equals(Event, e.Name, StringComparison.Ordinal) && Match.All(m => StpJsonPath.Same(StpJsonPath.Get(e.Params, m.Key), m.Value));

    public string Label =>
        Match.Count > 0 ? $"{Event} where {string.Join(" and ", Match.Select(m => $"{m.Key} = {StpJsonPath.Show(m.Value)}"))}" : Event;
}

/// <summary>
/// V10, diff intended vs actual, for one call: fed every event that arrives after the call is sent,
/// it matches them against what the call should produce, then reports done, partial, not_done,
/// other, unknown or unverifiable. It holds no timer: stop feeding it when <see cref="Observe(StpEvent)"/>
/// returns true (settled) or when the wait is over, then call <see cref="Report"/>.
/// Not thread-safe: feed it from one thread (or under a lock).
/// </summary>
public sealed class StpCallWatch
{
    /// <summary>Unrelated events a report names, at most.</summary>
    public const int MaxAlsoSeen = 20;

    private readonly string _method;
    private readonly StpMethodRules _rules;
    private readonly IReadOnlyList<ResolvedMatch> _others;
    private readonly string _refusalEvent;
    private readonly string _refusalText;
    private readonly HashSet<string>? _knownIssues;
    private readonly Dictionary<int, StpEvent> _fulfilled = new();
    private readonly List<Seen> _seen = new();
    private readonly List<StpEventRef> _alsoSeen = new();
    private int _unrelated;
    private string? _refusal;

    internal StpCallWatch(string method, StpMethodRules rules, JObject @params, StpRefusalMessage refusalMessage, IEnumerable<string>? knownIssues)
    {
        _method = method;
        _rules = rules;
        Items = ExpectedItems(rules, @params);
        _others = rules.Intent.Other.Select(o => Resolve(o, @params, null)).ToList();
        _refusalEvent = refusalMessage.Event;
        _refusalText = refusalMessage.For(method);
        _knownIssues = knownIssues == null ? null : new HashSet<string>(knownIssues, StringComparer.Ordinal);
    }

    /// <summary>What the call is expected to produce, per the rules and the call's params; empty when nothing is promised.</summary>
    public IReadOnlyList<StpExpectedItem> Items { get; }

    /// <summary>True once every expected item arrived; false while some are missing, or when none are expected.</summary>
    public bool IsComplete => Items.Count > 0 && _fulfilled.Count == Items.Count;

    /// <summary>Whether waiting longer can change the outcome: false once complete or refused.</summary>
    public bool IsSettled => IsComplete || _refusal != null;

    /// <summary>Feeds one event; returns <see cref="IsSettled"/>.</summary>
    public bool Observe(string name, JToken? @params, DateTimeOffset at) => Observe(new StpEvent(name, @params, at));

    /// <summary>Feeds one event; returns <see cref="IsSettled"/>.</summary>
    public bool Observe(StpEvent e)
    {
        if (e == null) throw new ArgumentNullException(nameof(e));
        if (string.Equals(e.Name, _refusalEvent, StringComparison.Ordinal))
        {
            string? message = StpJsonPath.AsString(StpJsonPath.Get(e.Params, "message"));
            if (message != null && message.IndexOf(_refusalText, StringComparison.Ordinal) >= 0)
            {
                _refusal ??= message;
                _seen.Add(new Seen(e, StpEventRole.Refusal, -1));
                return IsSettled;
            }
        }
        int idx = -1;
        for (int i = 0; i < Items.Count; i++)
        {
            if (!_fulfilled.ContainsKey(i) && Items[i].Matches(e))
            {
                idx = i;
                break;
            }
        }
        if (idx != -1)
        {
            _fulfilled[idx] = e;
            _seen.Add(new Seen(e, StpEventRole.Expected, idx));
        }
        else if (_others.Any(o => o.Matches(e)) || Items.Any(item => item.Matches(e)))
        {
            // something else, or a second event for an item already satisfied: decided at report time
            _seen.Add(new Seen(e, StpEventRole.Other, -1));
        }
        else if (_rules.Intent.Acks.Contains(e.Name))
        {
            _seen.Add(new Seen(e, StpEventRole.Ack, -1));
        }
        else
        {
            _unrelated++;
            if (_alsoSeen.Count < MaxAlsoSeen) _alsoSeen.Add(new StpEventRef(e.Name, EventPoid(e.Params)));
        }
        return IsSettled;
    }

    /// <summary>The outcome, given how the call itself ended.</summary>
    public StpCallReport Report(StpCallEnd end)
    {
        if (end == null) throw new ArgumentNullException(nameof(end));
        var doneIdx = _fulfilled.Keys.OrderBy(i => i).ToList();
        var missing = Items.Where((_, i) => !_fulfilled.ContainsKey(i)).Select(i => i.Label).ToList();
        var donePoids = new HashSet<string>(_fulfilled.Values.Select(f => EventPoid(f.Params)).Where(p => p != null)!, StringComparer.Ordinal);
        var observed = new List<StpReportedEvent>();
        var unexpected = new List<StpEventRef>();
        foreach (var s in _seen)
        {
            string? poid = EventPoid(s.Event.Params);
            switch (s.Role)
            {
                case StpEventRole.Expected:
                    observed.Add(new StpReportedEvent(s.Event, poid, StpEventRole.Expected, Items[s.Item].Label, null));
                    break;
                case StpEventRole.Refusal:
                    observed.Add(new StpReportedEvent(s.Event, poid, StpEventRole.Refusal, null,
                        StpJsonPath.AsString(StpJsonPath.Get(s.Event.Params, "message"))));
                    break;
                case StpEventRole.Ack:
                    observed.Add(new StpReportedEvent(s.Event, poid, StpEventRole.Ack, null, null));
                    break;
                default:
                    bool followOn = poid != null && donePoids.Contains(poid);
                    observed.Add(new StpReportedEvent(s.Event, poid, followOn ? StpEventRole.FollowOn : StpEventRole.Other, null, null));
                    if (!followOn) unexpected.Add(new StpEventRef(s.Event.Name, poid));
                    break;
            }
        }
        var facts = new ReportFacts(end)
        {
            Refusal = end.Refused ?? _refusal,
            Missing = missing,
            DoneCount = doneIdx.Count,
            Done = string.Join("; ", doneIdx.Select(WithPoid)),
            Unexpected = unexpected,
            Observed = observed,
            Issue = OpenIssue(),
        };
        var (outcome, summary) = Items.Count == 0 ? NothingPromised(facts) : Judge(facts);
        return new StpCallReport(outcome, summary, end)
        {
            Expected = Items.Select(i => i.Label).ToList(),
            Observed = observed,
            Missing = missing,
            Unexpected = unexpected,
            Refusal = facts.Refusal,
            KnownIssue = outcome == StpCallOutcome.Unverifiable ? facts.Issue?.Issue : null,
            UnrelatedEvents = _unrelated,
            AlsoSeen = _alsoSeen.ToList(),
        };
    }

    private StpUnverifiableRule? OpenIssue()
    {
        var issue = _rules.Intent.Unverifiable;
        if (issue == null) return null;
        return _knownIssues == null || _knownIssues.Contains(issue.Issue) ? issue : null;
    }

    private (StpCallOutcome, string) NothingPromised(ReportFacts f)
    {
        string m = _method;
        if (f.Refusal != null) return (StpCallOutcome.NotDone, $"Not done: STP refused {m}: {f.Refusal}");
        if (f.Unexpected.Count > 0) return (StpCallOutcome.Other, $"Did something else: {m} promises no event, but STP sent {List(f.Unexpected)}.");
        if (f.End.Failure != null) return (StpCallOutcome.Unknown, $"Unknown outcome: {f.End.Failure}");
        if (f.End.Wire == StpCallWire.Request && f.End.Answered)
        {
            return (StpCallOutcome.Done, $"Done: STP answered {m} with success. The contract promises no event for it, so the answer is the confirmation.");
        }
        string acked = f.Acked ? $" (it did acknowledge it: {AckList(f.Observed)})" : "";
        return (StpCallOutcome.Unverifiable,
            $"Unverifiable: STP sends no answer to {m} and the contract promises no event for it, so nothing confirms that " +
            $"the engine applied it{acked}. No refusal arrived within {Seconds(Math.Round(f.End.Waited.TotalMilliseconds) / 1000)} s. " +
            "Check the effect with a read if it matters.");
    }

    private (StpCallOutcome, string) Judge(ReportFacts f)
    {
        string m = _method;
        string t = Seconds(f.End.Timeout.TotalSeconds);
        string missing = string.Join("; ", f.Missing);
        if (f.Missing.Count == 0) return (StpCallOutcome.Done, $"Done: {f.Done}.");
        if (f.DoneCount > 0)
        {
            return (StpCallOutcome.Partial,
                $"Partly done - missing {f.Missing.Count} of {Items.Count}: {missing}. Done: {f.Done}." +
                (f.Refusal != null ? $" STP refused the rest: {f.Refusal}" : $" The rest did not arrive within {t} s."));
        }
        if (f.Refusal != null) return (StpCallOutcome.NotDone, $"Not done: STP refused {m}: {f.Refusal}");
        if (f.Unexpected.Count > 0) return (StpCallOutcome.Other, $"Did something else: instead of {missing}, STP sent {List(f.Unexpected)}.");
        if (f.Issue != null)
        {
            return (StpCallOutcome.Unverifiable,
                $"Unverifiable ({f.Issue.Issue}): {f.Issue.Reason}, so the event stream cannot confirm {m}. Expected " +
                $"{missing}; none arrived within {t} s. Check the effect with a read (e.g. GetPoidObject, GetTaskOrgObjectSet).");
        }
        if (f.End.Failure != null) return (StpCallOutcome.Unknown, $"Unknown outcome: {f.End.Failure} No expected event arrived either ({missing}).");
        if (f.End.Answered || f.Acked)
        {
            string how = f.End.Answered ? "answered success" : $"acknowledged it: {AckList(f.Observed)}";
            return (StpCallOutcome.NotDone, $"Not done: STP took {m} ({how}) but {missing} did not arrive within {t} s.");
        }
        return (StpCallOutcome.Unknown,
            $"Unknown outcome: nothing at all came back for {m} within {t} s - no {missing}, no refusal. STP " +
            "ignores a call it cannot apply without saying so, or it may still be busy: check with a read before relying on it.");
    }

    private string WithPoid(int index)
    {
        string label = Items[index].Label;
        // As the reference evaluator does: the poid of the first item with this label.
        int first = -1;
        for (int i = 0; i < Items.Count && first == -1; i++)
        {
            if (Items[i].Label == label) first = i;
        }
        string? poid = _fulfilled.TryGetValue(first, out var e) ? EventPoid(e.Params) : null;
        return poid != null && label.IndexOf(poid, StringComparison.Ordinal) < 0 ? $"{label} (poid {poid})" : label;
    }

    private static string List(IEnumerable<StpEventRef> events) =>
        string.Join(", ", events.Select(e => e.Poid != null ? $"{e.Event} for {e.Poid}" : e.Event));

    private static string AckList(IEnumerable<StpReportedEvent> observed) =>
        string.Join(", ", observed.Where(o => o.As == StpEventRole.Ack).Select(o => o.Event).Distinct());

    /// <summary>A number of seconds as the reports show it (shortest form: 0.3, 8).</summary>
    private static string Seconds(double seconds) => seconds.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    // ---- expected items ----------------------------------------------------------------------

    internal static IReadOnlyList<StpExpectedItem> ExpectedItems(StpMethodRules rules, JObject @params)
    {
        var items = new List<StpExpectedItem>();
        foreach (var exp in rules.Intent.Expect)
        {
            if (exp.ForEach == null)
            {
                var resolved = exp.Alternatives.Select(a => Resolve(a, @params, null)).ToList();
                items.Add(new StpExpectedItem(string.Join(" or ", resolved.Select(r => r.Label)), resolved));
                continue;
            }
            if (StpJsonPath.Get(@params, exp.ForEach) is not JArray list) continue;
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (!exp.Where.All(w => Passes(w, item))) continue;
                var resolved = exp.Alternatives.Select(a => Resolve(a, @params, item)).ToList();
                var what = new[] { StpJsonPath.Get(item, "fsTYPE"), StpJsonPath.Get(item, "poid") }
                    .Select(StpJsonPath.AsString).Where(s => s != null);
                string whatText = string.Join(" ", what);
                items.Add(new StpExpectedItem(
                    $"{exp.ForEach}[{i}]{(whatText.Length > 0 ? $" ({whatText})" : "")}: {string.Join(" or ", resolved.Select(r => r.Label))}",
                    resolved));
            }
        }
        return items;
    }

    private static bool Passes(StpItemFilter filter, JToken item)
    {
        var v = StpJsonPath.Get(item, filter.Path);
        if (filter.In != null && !filter.In.Any(x => StpJsonPath.Same(x, v))) return false;
        return filter.NotIn == null || !filter.NotIn.Any(x => StpJsonPath.Same(x, v));
    }

    private static ResolvedMatch Resolve(StpEventMatchRule m, JObject @params, JToken? item)
    {
        var match = new List<KeyValuePair<string, JToken>>();
        foreach (var pair in m.Match)
        {
            var r = pair.Value;
            JToken? v = r.Source switch
            {
                StpValueSource.Param => StpJsonPath.Get(@params, r.Path!),
                StpValueSource.Item => StpJsonPath.Get(item, r.Path!),
                _ => r.Value,
            };
            if (!StpJsonPath.IsAbsent(v)) match.Add(new KeyValuePair<string, JToken>(pair.Key, v!)); // an absent param does not constrain the match
        }
        return new ResolvedMatch(m.Event, match);
    }

    /// <summary>The poid an event is about: its own poid, its first alternate's, or its object's.</summary>
    internal static string? EventPoid(JToken? @params)
    {
        var direct = StpJsonPath.Get(@params, "poid");
        if (StpJsonPath.IsAbsent(direct)) direct = StpJsonPath.Get(@params, "alternates.0.poid");
        string? poid = StpJsonPath.AsString(direct);
        if (poid != null) return poid;
        foreach (string key in new[] { "coa", "taskOrg", "toUnit", "toRelationship", "symbol" })
        {
            string? p = StpJsonPath.AsString(StpJsonPath.Get(@params, key + ".poid"));
            if (p != null) return p;
        }
        return null;
    }

    private readonly struct Seen
    {
        public Seen(StpEvent e, StpEventRole role, int item)
        {
            Event = e;
            Role = role;
            Item = item;
        }

        public StpEvent Event { get; }
        public StpEventRole Role { get; }
        public int Item { get; }
    }

    private sealed class ReportFacts
    {
        public ReportFacts(StpCallEnd end) => End = end;

        public StpCallEnd End { get; }
        public string? Refusal { get; set; }
        public IReadOnlyList<string> Missing { get; set; } = Array.Empty<string>();
        public int DoneCount { get; set; }
        public string Done { get; set; } = "";
        public IReadOnlyList<StpEventRef> Unexpected { get; set; } = Array.Empty<StpEventRef>();
        public IReadOnlyList<StpReportedEvent> Observed { get; set; } = Array.Empty<StpReportedEvent>();
        public StpUnverifiableRule? Issue { get; set; }
        public bool Acked => Observed.Any(o => o.As == StpEventRole.Ack);
    }
}
