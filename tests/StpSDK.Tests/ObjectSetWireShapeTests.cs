using NUnit.Framework;
using StpSDK;
using System.Threading.Tasks;

namespace StpSDK.Tests;

/// <summary>
/// The engine answers GetScenarioObjectSet / GetTaskOrgObjectSet / GetCoaObjectSet with a BARE
/// ARRAY of objects (WebSocketsBridge StpJsonClient.cs: `result = os?.Objects` and the two
/// List&lt;StpObject&gt; SDK calls), never with {"objects":[...]}. Until 0.4.2-preview the SDK
/// deserialised ObjectSet and threw on every call; the old test oracle had encoded the wrong
/// shape, so the suite stayed green while the live call always failed.
/// </summary>
[TestFixture]
public class ObjectSetWireShapeTests
{
    private MetadataStubConnector _connector = null!;
    private StpRecognizer _recognizer = null!;

    [SetUp]
    public void SetUp()
    {
        _connector = new MetadataStubConnector();
        _recognizer = new StpRecognizer(_connector);
    }

    private const string BareArray = "[{\"fsTYPE\":\"unit\",\"poid\":\"p1\"},{\"fsTYPE\":\"task\",\"poid\":\"p2\"}]";

    [Test]
    public async Task GetScenarioObjectSetContentAsync_BareArray_IsTheEngineShape()
    {
        _connector.NextResponse = BareArray;
        var result = await _recognizer.GetScenarioObjectSetContentAsync();
        Assert.That(result.Objects, Has.Count.EqualTo(2));
        Assert.That(result.Objects[0].Poid, Is.EqualTo("p1"));
        Assert.That(result.Objects[1].Type, Is.EqualTo("task"));
    }

    [Test]
    public async Task GetTaskOrgObjectSetAsync_BareArray_IsTheEngineShape()
    {
        _connector.NextResponse = BareArray;
        var result = await _recognizer.GetTaskOrgObjectSetAsync("to1");
        Assert.That(result.Objects, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetCoaObjectSetAsync_BareArray_IsTheEngineShape()
    {
        _connector.NextResponse = BareArray;
        var result = await _recognizer.GetCoaObjectSetAsync("coa1");
        Assert.That(result.Objects, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task ObjectSetGetters_StillAcceptTheWrappedForm()
    {
        _connector.NextResponse = "{\"objects\":[{\"fsTYPE\":\"unit\",\"poid\":\"p1\"}]}";
        var result = await _recognizer.GetScenarioObjectSetContentAsync();
        Assert.That(result.Objects, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ObjectSetGetters_WhitespaceOrNull_ReturnEmpty()
    {
        _connector.NextResponse = "   ";
        var a = await _recognizer.GetScenarioObjectSetContentAsync();
        _connector.NextResponse = "null";
        var b = await _recognizer.GetCoaObjectSetAsync("x");
        Assert.That(a.Objects, Is.Empty);
        Assert.That(b.Objects, Is.Empty);
    }
}
