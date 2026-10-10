using Newtonsoft.Json.Linq;
using NUnit.Framework;
using StpSDK;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StpSDK.Tests;

/// <summary>
/// STP-1067. The ObjectSet-taking requests must send the objects as an <c>objects</c> ARRAY -
/// the property the engine reads - not as a serialized string under <c>content</c>.
/// </summary>
/// <remarks>
/// <para>
/// The engine bridge deserialises each of these methods into a DTO whose only property is
/// <c>[JsonProperty("objects")] List&lt;StpObject&gt; Objects</c> (WebSocketsBridge
/// <c>JsonMessage.cs</c>: LoadNewScenarioFromObjectSet, ImportPlanDataFromObjectSet,
/// SyncScenarioSessionFromObjectSet, ImportTaskOrgFromObjectSet, ImportCoaFromObjectSet). The
/// public contract (sketch-thru-plan-api.json) and the JS SDK agree: one param, <c>objects</c>,
/// an array. This SDK sent <c>{"content": "&lt;ObjectSet serialized to a string&gt;"}</c>, so
/// the engine saw <c>objects</c> = null and <c>new ObjectSet(null)</c> threw - every call was
/// refused.
/// </para>
/// <para>
/// The previous tests in RecognizerMetadataTests asserted <c>params.content</c>, i.e. they
/// encoded the SDK's own wrong shape as the oracle (the same failure ObjectSetWireShapeTests
/// records for the getters). These assert what the engine reads.
/// </para>
/// </remarks>
[TestFixture]
public class ObjectSetRequestWireTests
{
    private MetadataStubConnector _connector = null!;
    private StpRecognizer _recognizer = null!;

    [SetUp]
    public void SetUp()
    {
        _connector = new MetadataStubConnector();
        _recognizer = new StpRecognizer(_connector);
    }

    [TearDown]
    public void TearDown()
    {
        _recognizer?.Dispose();
        _connector?.Dispose();
    }

    private static ObjectSet TwoObjects() => new ObjectSet(new List<StpObject>
    {
        new StpObject { Type = "unit", Poid = "p1" },
        new StpObject { Type = "task", Poid = "p2" },
    });

    private static IEnumerable<TestCaseData> Methods()
    {
        yield return new TestCaseData("LoadNewScenarioFromObjectSet",
            (Func<StpRecognizer, ObjectSet, Task>)((r, os) => r.LoadNewScenarioAsync(os)))
            .SetArgDisplayNames("LoadNewScenarioAsync");
        yield return new TestCaseData("ImportPlanDataFromObjectSet",
            (Func<StpRecognizer, ObjectSet, Task>)((r, os) => r.ImportSTPDataAsync(os)))
            .SetArgDisplayNames("ImportSTPDataAsync");
        yield return new TestCaseData("SyncScenarioSessionFromObjectSet",
            (Func<StpRecognizer, ObjectSet, Task>)((r, os) => r.SyncScenarioSessionAsync(os)))
            .SetArgDisplayNames("SyncScenarioSessionAsync");
        yield return new TestCaseData("ImportTaskOrgFromObjectSet",
            (Func<StpRecognizer, ObjectSet, Task>)((r, os) => r.ImportTaskOrgAsync(os)))
            .SetArgDisplayNames("ImportTaskOrgAsync");
        yield return new TestCaseData("ImportCoaFromObjectSet",
            (Func<StpRecognizer, ObjectSet, Task>)((r, os) => r.ImportCoaAsync(os)))
            .SetArgDisplayNames("ImportCoaAsync");
    }

    [TestCaseSource(nameof(Methods))]
    public async Task SendsTheObjectsArrayTheEngineReads(string wireMethod, Func<StpRecognizer, ObjectSet, Task> call)
    {
        await call(_recognizer, TwoObjects());

        var json = JObject.Parse(_connector.SentMessages[^1]);
        Assert.That(json["method"]?.ToString(), Is.EqualTo(wireMethod));

        var p = json["params"] as JObject;
        Assert.That(p, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(p!.Property("content"), Is.Null,
                        "the engine DTO has no 'content' property - anything sent there is ignored");
            Assert.That(p.Property("objects"), Is.Not.Null,
                        "the engine DTO reads 'objects'");
            Assert.That(p["objects"]?.Type, Is.EqualTo(JTokenType.Array),
                        "objects is a List<StpObject> on the engine: an array, not a serialized string");
        });

        var objects = (JArray)p!["objects"]!;
        Assert.That(objects, Has.Count.EqualTo(2));
        Assert.That(objects[0]["fsTYPE"]?.ToString(), Is.EqualTo("unit"));
        Assert.That(objects[0]["poid"]?.ToString(), Is.EqualTo("p1"));
        Assert.That(objects[1]["fsTYPE"]?.ToString(), Is.EqualTo("task"));
        Assert.That(objects[1]["poid"]?.ToString(), Is.EqualTo("p2"));
    }

    [TestCaseSource(nameof(Methods))]
    public async Task EmptySet_SendsAnEmptyArray(string wireMethod, Func<StpRecognizer, ObjectSet, Task> call)
    {
        await call(_recognizer, new ObjectSet());

        var json = JObject.Parse(_connector.SentMessages[^1]);
        Assert.That(json["method"]?.ToString(), Is.EqualTo(wireMethod));
        Assert.That(json["params"]?["objects"]?.Type, Is.EqualTo(JTokenType.Array));
        Assert.That((JArray)json["params"]!["objects"]!, Is.Empty);
    }
}
