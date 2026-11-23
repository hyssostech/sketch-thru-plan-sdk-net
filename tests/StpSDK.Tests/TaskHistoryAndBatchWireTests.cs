using Newtonsoft.Json.Linq;
using NUnit.Framework;
using StpSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StpSDK.Tests;

/// <summary>
/// STP-1797: the 5.12 surface of the public contract (sketch-thru-plan-api.json 0.9.11, sdk-js
/// release/5.12 b7a490d) - the task history requests GetTaskHistory / AddTaskComment /
/// EditTaskComment / DeleteTaskComment, the events TaskEventAdded / TaskEventModified /
/// BatchStarted / BatchEnded, and the trailing nullable batchId on the object events. Method names,
/// param names and payloads are the contract's own (its examples where it gives them), so a rename
/// on either side fails here.
/// </summary>
[TestFixture]
public class TaskHistoryRequestWireTests
{
    private MetadataStubConnector _connector;
    private StpRecognizer _recognizer;

    [SetUp]
    public void Setup()
    {
        _connector = new MetadataStubConnector();
        _recognizer = new StpRecognizer(_connector);
    }

    [TearDown]
    public void TearDown()
    {
        _recognizer.Dispose();
        _connector?.Dispose();
    }

    private JObject LastSent() => JObject.Parse(_connector.SentMessages[^1]);

    private static IEnumerable<string> ParamNames(JObject sent) =>
        ((JObject)sent["params"]).Properties().Select(p => p.Name);

    // The GetTaskHistory example result from the contract, verbatim.
    private const string HistoryExample = @"[
      { ""poid"": ""idEV2"", ""entry"": { ""task"": ""idTASK1"", ""utc"": ""2026-10-19T01:00:00Z"", ""kind"": ""comment"", ""origin"": ""user"",
        ""author"": ""Pat"", ""role"": ""s3"", ""source"": ""WebSocketsBridge"", ""text"": ""Hold until the bridge is clear"", ""detail"": ""{}"",
        ""edited"": null, ""deleted"": null } },
      { ""poid"": ""idEV1"", ""entry"": { ""task"": ""idTASK1"", ""utc"": ""2026-10-19T00:58:00Z"", ""kind"": ""edited"", ""origin"": ""system"",
        ""author"": ""Pat"", ""role"": ""s3"", ""source"": ""STP Agent"", ""text"": ""start_time 1 -> 2"",
        ""detail"": ""{\""fields\"":[{\""name\"":\""start_time\"",\""from\"":\""1\"",\""to\"":\""2\""}]}"", ""edited"": null, ""deleted"": null } }
    ]";

    [Test]
    public async Task GetTaskHistory_SendsTaskPoid_AndParsesTheContractExample()
    {
        _connector.NextResponse = HistoryExample;

        var items = await _recognizer.GetTaskHistoryAsync("idTASK1");

        var sent = LastSent();
        Assert.That(sent["method"]?.ToString(), Is.EqualTo("GetTaskHistory"));
        Assert.That(ParamNames(sent), Is.EqualTo(new[] { "taskPoid" }));
        Assert.That(sent["params"]["taskPoid"]?.ToString(), Is.EqualTo("idTASK1"));

        Assert.That(items, Has.Count.EqualTo(2));
        Assert.That(items[0].Poid, Is.EqualTo("idEV2"));
        var e = items[0].Entry;
        Assert.That(e.Task, Is.EqualTo("idTASK1"));
        Assert.That(e.Utc, Is.EqualTo("2026-10-19T01:00:00Z"), "utc is a string on the wire and must not be reformatted");
        Assert.That(e.Kind, Is.EqualTo("comment"));
        Assert.That(e.Origin, Is.EqualTo("user"));
        Assert.That(e.Author, Is.EqualTo("Pat"));
        Assert.That(e.Role, Is.EqualTo("s3"));
        Assert.That(e.Source, Is.EqualTo("WebSocketsBridge"));
        Assert.That(e.Text, Is.EqualTo("Hold until the bridge is clear"));
        Assert.That(e.Detail, Is.EqualTo("{}"));
        Assert.That(e.Edited, Is.Null);
        Assert.That(e.Deleted, Is.Null);
        Assert.That(items[1].Entry.Kind, Is.EqualTo("edited"));
        Assert.That(items[1].Entry.Origin, Is.EqualTo("system"));
        Assert.That(JObject.Parse(items[1].Entry.Detail)["fields"]?[0]?["name"]?.ToString(), Is.EqualTo("start_time"));
    }

    [Test]
    public async Task GetTaskHistory_EmptyAnswer_IsAnEmptyList()
    {
        _connector.NextResponse = "[]";
        Assert.That(await _recognizer.GetTaskHistoryAsync("unknown"), Is.Empty);

        _connector.NextResponse = null;
        Assert.That(await _recognizer.GetTaskHistoryAsync("unknown"), Is.Empty);
    }

    [Test]
    public async Task GetTaskHistory_KeepsAnEntryWithAnUnknownKindAndExtraProperties()
    {
        _connector.NextResponse = @"[{ ""poid"": ""idEV9"", ""entry"": { ""task"": ""t"", ""utc"": ""2027-01-01T00:00:00Z"", ""kind"": ""exported"",
            ""origin"": ""system"", ""author"": null, ""role"": null, ""source"": null, ""text"": null, ""detail"": null,
            ""edited"": null, ""deleted"": null, ""target"": ""c2sim"" } }]";

        var items = await _recognizer.GetTaskHistoryAsync("t");

        Assert.That(items[0].Entry.Kind, Is.EqualTo("exported"));
        Assert.That(items[0].Entry.AdditionalProperties["target"]?.ToString(), Is.EqualTo("c2sim"));
    }

    [Test]
    public async Task AddTaskComment_SendsTaskPoidAndText_AndAnswersTheEntryPoid()
    {
        // The connector hands a JSON string result on as its ToString(): the bare text.
        _connector.NextResponse = "idEV2";

        var entryPoid = await _recognizer.AddTaskCommentAsync("idTASK1", "Hold until the bridge is clear");

        var sent = LastSent();
        Assert.That(sent["method"]?.ToString(), Is.EqualTo("AddTaskComment"));
        Assert.That(ParamNames(sent), Is.EqualTo(new[] { "taskPoid", "text" }));
        Assert.That(sent["params"]["taskPoid"]?.ToString(), Is.EqualTo("idTASK1"));
        Assert.That(sent["params"]["text"]?.ToString(), Is.EqualTo("Hold until the bridge is clear"));
        Assert.That(entryPoid, Is.EqualTo("idEV2"));
    }

    [Test]
    public async Task EditTaskComment_SendsEntryPoidAndText_AndAnswersTrue()
    {
        _connector.NextResponse = "True";

        bool ok = await _recognizer.EditTaskCommentAsync("idEV2", "Hold until 0700");

        var sent = LastSent();
        Assert.That(sent["method"]?.ToString(), Is.EqualTo("EditTaskComment"));
        Assert.That(ParamNames(sent), Is.EqualTo(new[] { "entryPoid", "text" }));
        Assert.That(sent["params"]["entryPoid"]?.ToString(), Is.EqualTo("idEV2"));
        Assert.That(sent["params"]["text"]?.ToString(), Is.EqualTo("Hold until 0700"));
        Assert.That(ok, Is.True);
    }

    [Test]
    public async Task DeleteTaskComment_SendsEntryPoidOnly_AndAnswersTrue()
    {
        _connector.NextResponse = "True";

        bool ok = await _recognizer.DeleteTaskCommentAsync("idEV2");

        var sent = LastSent();
        Assert.That(sent["method"]?.ToString(), Is.EqualTo("DeleteTaskComment"));
        Assert.That(ParamNames(sent), Is.EqualTo(new[] { "entryPoid" }));
        Assert.That(sent["params"]["entryPoid"]?.ToString(), Is.EqualTo("idEV2"));
        Assert.That(ok, Is.True);
    }

    [Test]
    public async Task EditAndDeleteTaskComment_FalseAnswer_IsFalse()
    {
        _connector.NextResponse = "False";
        Assert.That(await _recognizer.EditTaskCommentAsync("idEV2", "x"), Is.False);
        Assert.That(await _recognizer.DeleteTaskCommentAsync("idEV2"), Is.False);
    }
}

[TestFixture]
public class TaskHistoryAndBatchEventWireTests
{
    private StubConnector _connector;
    private StpRecognizer _recognizer;
    private readonly List<string> _warnings = new();

    [SetUp]
    public void Setup()
    {
        _connector = new StubConnector();
        _recognizer = new StpRecognizer(_connector);
        _warnings.Clear();
        _recognizer.OnStpMessage += (level, msg) => { if (level == StpRecognizer.StpMessageLevel.Warning) _warnings.Add(msg); };
    }

    [TearDown]
    public void TearDown()
    {
        _recognizer.Dispose();
        _connector?.Dispose();
    }

    #region TaskEventAdded / TaskEventModified

    [Test]
    public void TaskEventAdded_DispatchesTheContractExample()
    {
        string poid = null, batchId = "unset";
        TaskHistoryEntry entry = null;
        _recognizer.OnTaskEventAdded += (p, e, b) => { poid = p; entry = e; batchId = b; };

        // AddTaskComment's x-stpEventsObserved, verbatim.
        _connector.SimulateMessage(@"{ ""method"": ""TaskEventAdded"", ""params"": {
            ""poid"": ""idEV2"",
            ""entry"": { ""task"": ""idTASK1"", ""utc"": ""2026-10-19T01:00:00Z"", ""kind"": ""comment"", ""origin"": ""user"",
              ""author"": ""Pat"", ""role"": ""s3"", ""source"": ""WebSocketsBridge"", ""text"": ""Hold until the bridge is clear"",
              ""detail"": ""{}"", ""edited"": null, ""deleted"": null },
            ""batchId"": null } }");

        Assert.That(poid, Is.EqualTo("idEV2"));
        Assert.That(batchId, Is.Null);
        Assert.That(entry.Task, Is.EqualTo("idTASK1"));
        Assert.That(entry.Utc, Is.EqualTo("2026-10-19T01:00:00Z"), "a date-shaped string must arrive exactly as sent");
        Assert.That(entry.Kind, Is.EqualTo("comment"));
        Assert.That(entry.Origin, Is.EqualTo("user"));
        Assert.That(entry.Author, Is.EqualTo("Pat"));
        Assert.That(entry.Text, Is.EqualTo("Hold until the bridge is clear"));
        Assert.That(entry.Detail, Is.EqualTo("{}"));
        Assert.That(entry.Edited, Is.Null);
        Assert.That(entry.Deleted, Is.Null);
    }

    [Test]
    public void TaskEventAdded_CarriesBatchId_AndKeepsDateShapedTextVerbatim()
    {
        string batchId = null;
        TaskHistoryEntry entry = null;
        _recognizer.OnTaskEventAdded += (p, e, b) => { entry = e; batchId = b; };

        _connector.SimulateMessage(@"{ ""method"": ""TaskEventAdded"", ""params"": {
            ""poid"": ""idEV3"",
            ""entry"": { ""task"": ""idTASK1"", ""utc"": ""2026-10-19T01:00:00.123+02:00"", ""kind"": ""comment"", ""origin"": ""user"",
              ""author"": ""Pat"", ""role"": ""s3"", ""source"": ""WebSocketsBridge"", ""text"": ""2026-10-20T07:00:00Z"",
              ""detail"": ""{}"", ""edited"": null, ""deleted"": null },
            ""batchId"": ""batch-7"" } }");

        Assert.That(batchId, Is.EqualTo("batch-7"));
        Assert.That(entry.Utc, Is.EqualTo("2026-10-19T01:00:00.123+02:00"));
        Assert.That(entry.Text, Is.EqualTo("2026-10-20T07:00:00Z"));
    }

    [Test]
    public void TaskEventModified_DispatchesTheTombstoneExample()
    {
        string poid = null, batchId = "unset";
        TaskHistoryEntry entry = null;
        _recognizer.OnTaskEventModified += (p, e, b) => { poid = p; entry = e; batchId = b; };

        // DeleteTaskComment's x-stpEventsObserved, verbatim.
        _connector.SimulateMessage(@"{ ""method"": ""TaskEventModified"", ""params"": {
            ""poid"": ""idEV2"",
            ""entry"": { ""task"": ""idTASK1"", ""utc"": ""2026-10-19T01:00:00Z"", ""kind"": ""comment"", ""origin"": ""user"",
              ""author"": ""Pat"", ""role"": ""s3"", ""source"": ""WebSocketsBridge"", ""text"": null, ""detail"": null,
              ""edited"": null, ""deleted"": ""2026-10-19T01:10:00Z"" },
            ""batchId"": null } }");

        Assert.That(poid, Is.EqualTo("idEV2"));
        Assert.That(batchId, Is.Null);
        Assert.That(entry.Text, Is.Null);
        Assert.That(entry.Detail, Is.Null);
        Assert.That(entry.Deleted, Is.EqualTo("2026-10-19T01:10:00Z"));
    }

    [Test]
    public void TaskEventModified_EditExample_KeepsRevisionsDetail()
    {
        TaskHistoryEntry entry = null;
        _recognizer.OnTaskEventModified += (p, e, b) => entry = e;

        _connector.SimulateMessage(@"{ ""method"": ""TaskEventModified"", ""params"": {
            ""poid"": ""idEV2"",
            ""entry"": { ""task"": ""idTASK1"", ""utc"": ""2026-10-19T01:00:00Z"", ""kind"": ""comment"", ""origin"": ""user"",
              ""author"": ""Pat"", ""role"": ""s3"", ""source"": ""WebSocketsBridge"", ""text"": ""Hold until 0700"",
              ""detail"": ""{\""revisions\"":[{\""text\"":\""Hold until the bridge is clear\"",\""utc\"":\""2026-10-19T01:00:00Z\""}]}"",
              ""edited"": ""2026-10-19T01:05:00Z"", ""deleted"": null },
            ""batchId"": null } }");

        Assert.That(entry.Text, Is.EqualTo("Hold until 0700"));
        Assert.That(entry.Edited, Is.EqualTo("2026-10-19T01:05:00Z"));
        Assert.That(entry.Detail, Is.EqualTo("{\"revisions\":[{\"text\":\"Hold until the bridge is clear\",\"utc\":\"2026-10-19T01:00:00Z\"}]}"));
    }

    [TestCase("TaskEventAdded")]
    [TestCase("TaskEventModified")]
    public void TaskEvent_WithoutEntry_IsDroppedWithAWarning(string method)
    {
        bool fired = false;
        _recognizer.OnTaskEventAdded += (p, e, b) => fired = true;
        _recognizer.OnTaskEventModified += (p, e, b) => fired = true;

        _connector.SimulateMessage($@"{{ ""method"": ""{method}"", ""params"": {{ ""poid"": ""idEV2"" }} }}");

        Assert.That(fired, Is.False);
        Assert.That(_warnings, Has.Some.Contains("HandleTaskEvent"));
    }

    #endregion

    #region BatchStarted / BatchEnded

    [Test]
    public void BatchStarted_DispatchesEveryContractParam()
    {
        string batchId = null, kind = null, origin = null;
        int expected = 0;
        bool isOwn = false;
        _recognizer.OnBatchStarted += (b, k, e, o, or) => { batchId = b; kind = k; expected = e; isOwn = o; origin = or; };

        _connector.SimulateMessage(@"{ ""method"": ""BatchStarted"", ""params"": {
            ""batchId"": ""batch-7"", ""kind"": ""load"", ""expected"": 42, ""isOwn"": true, ""origin"": ""live"" } }");

        Assert.That(batchId, Is.EqualTo("batch-7"));
        Assert.That(kind, Is.EqualTo("load"));
        Assert.That(expected, Is.EqualTo(42));
        Assert.That(isOwn, Is.True);
        Assert.That(origin, Is.EqualTo("live"));
    }

    [Test]
    public void BatchStarted_UnsaidExpected_IsMinusOne()
    {
        int expected = 0;
        string origin = null;
        _recognizer.OnBatchStarted += (b, k, e, o, or) => { expected = e; origin = or; };

        _connector.SimulateMessage(@"{ ""method"": ""BatchStarted"", ""params"": {
            ""batchId"": ""batch-8"", ""kind"": ""join"", ""expected"": -1, ""isOwn"": false, ""origin"": ""replay"" } }");

        Assert.That(expected, Is.EqualTo(-1));
        Assert.That(origin, Is.EqualTo("replay"));
    }

    [Test]
    public void BatchEnded_DispatchesEveryContractParam()
    {
        string batchId = null, kind = null, status = null, origin = null;
        int informs = -5;
        bool isOwn = true;
        _recognizer.OnBatchEnded += (b, k, s, i, o, or) => { batchId = b; kind = k; status = s; informs = i; isOwn = o; origin = or; };

        _connector.SimulateMessage(@"{ ""method"": ""BatchEnded"", ""params"": {
            ""batchId"": ""batch-7"", ""kind"": ""import"", ""status"": ""abandoned"", ""informs"": 17, ""isOwn"": false, ""origin"": ""live"" } }");

        Assert.That(batchId, Is.EqualTo("batch-7"));
        Assert.That(kind, Is.EqualTo("import"));
        Assert.That(status, Is.EqualTo("abandoned"));
        Assert.That(informs, Is.EqualTo(17));
        Assert.That(isOwn, Is.False);
        Assert.That(origin, Is.EqualTo("live"));
    }

    [TestCase("BatchStarted")]
    [TestCase("BatchEnded")]
    public void Batch_WithoutBatchId_IsDroppedWithAWarning(string method)
    {
        bool fired = false;
        _recognizer.OnBatchStarted += (b, k, e, o, or) => fired = true;
        _recognizer.OnBatchEnded += (b, k, s, i, o, or) => fired = true;

        _connector.SimulateMessage($@"{{ ""method"": ""{method}"", ""params"": {{ ""kind"": ""load"" }} }}");

        Assert.That(fired, Is.False);
        Assert.That(_warnings, Has.Some.Contains("HandleBatch"));
    }

    #endregion

    #region Registration

    [Test]
    public async Task NewEvents_AreSubscribedByTheirContractNames()
    {
        _recognizer.OnTaskEventAdded += (p, e, b) => { };
        _recognizer.OnTaskEventModified += (p, e, b) => { };
        _recognizer.OnBatchStarted += (b, k, e, o, or) => { };
        _recognizer.OnBatchEnded += (b, k, s, i, o, or) => { };

        await _recognizer.RegisterAsync("test");

        Assert.That(_connector.LastSolvables, Is.SupersetOf(new[] { "TaskEventAdded", "TaskEventModified", "BatchStarted", "BatchEnded" }));
    }

    #endregion
}

/// <summary>
/// The trailing nullable batchId on the 18 object events that carry it in contract 0.9.11. It is
/// surfaced through additive On&lt;Event&gt;Ext events, so the existing delegates - and every
/// subscriber and sample written against them - are unchanged.
/// </summary>
[TestFixture]
public class ObjectEventBatchIdWireTests
{
    private StubConnector _connector;
    private StpRecognizer _recognizer;

    [SetUp]
    public void Setup()
    {
        _connector = new StubConnector();
        _recognizer = new StpRecognizer(_connector);
    }

    [TearDown]
    public void TearDown()
    {
        _recognizer.Dispose();
        _connector?.Dispose();
    }

    private const string Loc = @"""location"": { ""fsTYPE"": ""location"", ""shape"": ""point"", ""coords"": [{ ""lat"": 1, ""lon"": 2 }], ""centroid"": { ""lat"": 1, ""lon"": 2 } }";

    public record Case(string Method, string Params, Action<StpRecognizer, Action> AttachBase, Action<StpRecognizer, Action<string>> AttachExt)
    {
        public override string ToString() => Method;
    }

    public static IEnumerable<Case> Cases()
    {
        string sym = @"{ ""fsTYPE"": ""unit"", ""poid"": ""p1"", ""sidc"": ""SFGPUCI----D---"", ""affiliation"": ""friend"", " + Loc + " }";
        string task = @"{ ""fsTYPE"": ""task"", ""poid"": ""p1"", ""name"": ""Attack"" }";
        string to = @"{ ""fsTYPE"": ""task_org"", ""poid"": ""p1"", ""name"": ""TO"" }";
        string unit = @"{ ""fsTYPE"": ""task_org_unit"", ""poid"": ""p1"" }";
        string rel = @"{ ""fsTYPE"": ""task_org_relationship"", ""poid"": ""p1"" }";
        string coa = @"{ ""fsTYPE"": ""coa"", ""poid"": ""p1"", ""name"": ""COA 1"" }";
        string del = @"""poid"": ""p1"", ""isUndo"": false";

        yield return new Case("SymbolAdded", $@"""alternates"": [{sym}], ""isUndo"": false",
            (r, a) => r.OnSymbolAdded += (p, s, u) => a(), (r, a) => r.OnSymbolAddedExt += (p, s, u, b) => a(b));
        yield return new Case("SymbolModified", $@"""poid"": ""p1"", ""symbol"": {sym}, ""isUndo"": false",
            (r, a) => r.OnSymbolModified += (p, s, u) => a(), (r, a) => r.OnSymbolModifiedExt += (p, s, u, b) => a(b));
        yield return new Case("SymbolDeleted", del,
            (r, a) => r.OnSymbolDeleted += (p, u) => a(), (r, a) => r.OnSymbolDeletedExt += (p, u, b) => a(b));

        yield return new Case("TaskAdded", $@"""poid"": ""p1"", ""alternates"": [{task}], ""taskPoids"": [""p1""], ""isUndo"": false",
            (r, a) => r.OnTaskAdded += (p, t, l, u) => a(), (r, a) => r.OnTaskAddedExt += (p, t, l, u, b) => a(b));
        yield return new Case("TaskModified", $@"""poid"": ""p1"", ""alternates"": [{task}], ""taskPoids"": [""p1""], ""isUndo"": false",
            (r, a) => r.OnTaskModified += (p, t, l, u) => a(), (r, a) => r.OnTaskModifiedExt += (p, t, l, u, b) => a(b));
        yield return new Case("TaskDeleted", del,
            (r, a) => r.OnTaskDeleted += (p, u) => a(), (r, a) => r.OnTaskDeletedExt += (p, u, b) => a(b));

        yield return new Case("TaskOrgAdded", $@"""poid"": ""p1"", ""taskOrg"": {to}, ""isUndo"": false",
            (r, a) => r.OnTaskOrgAdded += (p, t, u) => a(), (r, a) => r.OnTaskOrgAddedExt += (p, t, u, b) => a(b));
        yield return new Case("TaskOrgModified", $@"""poid"": ""p1"", ""taskOrg"": {to}, ""isUndo"": false",
            (r, a) => r.OnTaskOrgModified += (p, t, u) => a(), (r, a) => r.OnTaskOrgModifiedExt += (p, t, u, b) => a(b));
        yield return new Case("TaskOrgDeleted", del,
            (r, a) => r.OnTaskOrgDeleted += (p, u) => a(), (r, a) => r.OnTaskOrgDeletedExt += (p, u, b) => a(b));

        yield return new Case("TaskOrgUnitAdded", $@"""poid"": ""p1"", ""toUnit"": {unit}, ""isUndo"": false",
            (r, a) => r.OnTaskOrgUnitAdded += (p, t, u) => a(), (r, a) => r.OnTaskOrgUnitAddedExt += (p, t, u, b) => a(b));
        yield return new Case("TaskOrgUnitModified", $@"""poid"": ""p1"", ""toUnit"": {unit}, ""isUndo"": false",
            (r, a) => r.OnTaskOrgUnitModified += (p, t, u) => a(), (r, a) => r.OnTaskOrgUnitModifiedExt += (p, t, u, b) => a(b));
        yield return new Case("TaskOrgUnitDeleted", del,
            (r, a) => r.OnTaskOrgUnitDeleted += (p, u) => a(), (r, a) => r.OnTaskOrgUnitDeletedExt += (p, u, b) => a(b));

        yield return new Case("TaskOrgRelationshipAdded", $@"""poid"": ""p1"", ""toRelationship"": {rel}, ""isUndo"": false",
            (r, a) => r.OnTaskOrgRelationshipAdded += (p, t, u) => a(), (r, a) => r.OnTaskOrgRelationshipAddedExt += (p, t, u, b) => a(b));
        yield return new Case("TaskOrgRelationshipModified", $@"""poid"": ""p1"", ""toRelationship"": {rel}, ""isUndo"": false",
            (r, a) => r.OnTaskOrgRelationshipModified += (p, t, u) => a(), (r, a) => r.OnTaskOrgRelationshipModifiedExt += (p, t, u, b) => a(b));
        yield return new Case("TaskOrgRelationshipDeleted", del,
            (r, a) => r.OnTaskOrgRelationshipDeleted += (p, u) => a(), (r, a) => r.OnTaskOrgRelationshipDeletedExt += (p, u, b) => a(b));

        yield return new Case("CoaAdded", $@"""poid"": ""p1"", ""coa"": {coa}, ""isUndo"": false",
            (r, a) => r.OnCoaAdded += (p, c, u) => a(), (r, a) => r.OnCoaAddedExt += (p, c, u, b) => a(b));
        yield return new Case("CoaModified", $@"""poid"": ""p1"", ""coa"": {coa}, ""isUndo"": false",
            (r, a) => r.OnCoaModified += (p, c, u) => a(), (r, a) => r.OnCoaModifiedExt += (p, c, u, b) => a(b));
        yield return new Case("CoaDeleted", del,
            (r, a) => r.OnCoaDeleted += (p, u) => a(), (r, a) => r.OnCoaDeletedExt += (p, u, b) => a(b));
    }

    [TestCaseSource(nameof(Cases))]
    public void BatchId_ReachesTheExtEvent_AndTheExistingEventStillFires(Case c)
    {
        bool baseFired = false;
        string batchId = "unset";
        c.AttachBase(_recognizer, () => baseFired = true);
        c.AttachExt(_recognizer, b => batchId = b);

        _connector.SimulateMessage($@"{{ ""method"": ""{c.Method}"", ""params"": {{ {c.Params}, ""batchId"": ""batch-7"" }} }}");

        Assert.That(baseFired, Is.True, "the existing delegate must keep firing");
        Assert.That(batchId, Is.EqualTo("batch-7"));
    }

    [TestCaseSource(nameof(Cases))]
    public void BatchId_NullOrAbsent_IsNull(Case c)
    {
        var seen = new List<string>();
        c.AttachExt(_recognizer, b => seen.Add(b));

        _connector.SimulateMessage($@"{{ ""method"": ""{c.Method}"", ""params"": {{ {c.Params}, ""batchId"": null }} }}");
        _connector.SimulateMessage($@"{{ ""method"": ""{c.Method}"", ""params"": {{ {c.Params} }} }}");

        Assert.That(seen, Is.EqualTo(new string[] { null, null }));
    }

    [TestCaseSource(nameof(Cases))]
    public async Task ExtHandlerAlone_SubscribesTheEvent(Case c)
    {
        c.AttachExt(_recognizer, b => { });

        await _recognizer.RegisterAsync("test");

        Assert.That(_connector.LastSolvables, Is.EqualTo(new[] { c.Method }));
    }
}
