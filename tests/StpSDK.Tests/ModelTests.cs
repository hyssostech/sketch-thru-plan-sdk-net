using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StpSDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StpSDK.Tests;

[TestFixture]
public class StpObjectTests
{
    [Test]
    public void StpObject_DefaultConstructor_CreatesInstance()
    {
        var obj = new StpObject();
        Assert.That(obj, Is.Not.Null);
        Assert.That(obj.Type, Is.Null);
        Assert.That(obj.Poid, Is.Null);
    }

    [Test]
    public void StpObject_TypeProperty_SetAndGet()
    {
        var obj = new StpObject { Type = "unit" };
        Assert.That(obj.Type, Is.EqualTo("unit"));
    }

    [Test]
    public void StpObject_PoidProperty_SetAndGet()
    {
        var obj = new StpObject { Poid = "poid123" };
        Assert.That(obj.Poid, Is.EqualTo("poid123"));
    }

    [Test]
    public void StpObject_Description_DefaultEmpty()
    {
        var obj = new StpObject();
        Assert.That(obj.Description, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Extensions_Null_ByDefault()
    {
        var obj = new StpObject();
        Assert.That(obj.Extensions, Is.Null);
    }

    [Test]
    public void Extensions_SetDictionary_Roundtrips()
    {
        var obj = new StpObject();
        obj.Extensions = new Dictionary<string, object>
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };
        Assert.That(obj.Extensions["key1"], Is.EqualTo("value1"));
        Assert.That(obj.Extensions, Has.Count.EqualTo(2));
    }

    [Test]
    public void StpObject_JsonSerialization_PreservesType()
    {
        var obj = new StpObject
        {
            Type = "unit",
            Poid = "test-poid",
        };
        string json = JsonConvert.SerializeObject(obj);
        Assert.That(json, Does.Contain("unit"));
        Assert.That(json, Does.Contain("test-poid"));
    }

    [Test]
    public void StpObject_JsonSerialization_OmitsExtensionsWhenNull()
    {
        var obj = new StpObject { Type = "unit" };
        string json = JsonConvert.SerializeObject(obj);
        Assert.That(json, Does.Not.Contain("extensions"));
    }

    [Test]
    public void AsTypedObject_Unit_ReturnsStpSymbol()
    {
        var obj = new StpObject { Type = "unit", Poid = "p1" };
        var typed = obj.AsTypedObject();
        Assert.That(typed, Is.InstanceOf<StpSymbol>());
    }

    [Test]
    public void AsTypedObject_Tg_ReturnsStpSymbol()
    {
        var obj = new StpObject { Type = "tg", Poid = "p1" };
        var typed = obj.AsTypedObject();
        Assert.That(typed, Is.InstanceOf<StpSymbol>());
    }

    [Test]
    public void AsTypedObject_Task_ReturnsStpTask()
    {
        var obj = new StpObject { Type = "task", Poid = "p1" };
        var typed = obj.AsTypedObject();
        Assert.That(typed, Is.InstanceOf<StpTask>());
    }

    [Test]
    public void AsTypedObject_TaskOrg_ReturnsStpTaskOrg()
    {
        var obj = new StpObject { Type = "task_org", Poid = "p1" };
        var typed = obj.AsTypedObject();
        Assert.That(typed, Is.InstanceOf<StpTaskOrg>());
    }

    [Test]
    public void AsTypedObject_TaskOrgUnit_ReturnsStpTaskOrgUnit()
    {
        var obj = new StpObject { Type = "task_org_unit", Poid = "p1" };
        var typed = obj.AsTypedObject();
        Assert.That(typed, Is.InstanceOf<StpTaskOrgUnit>());
    }

    [Test]
    public void AsTypedObject_Coa_ReturnsStpCoa()
    {
        var obj = new StpObject { Type = "coa", Poid = "p1" };
        var typed = obj.AsTypedObject();
        Assert.That(typed, Is.InstanceOf<StpCoa>());
    }

    [Test]
    public void AsTypedObject_Unknown_ReturnsSelf()
    {
        var obj = new StpObject { Type = "unknown_type", Poid = "p1" };
        var typed = obj.AsTypedObject();
        Assert.That(typed.GetType(), Is.EqualTo(typeof(StpObject)));
    }
}

[TestFixture]
public class LatLonTests
{
    [Test]
    public void Constructor_Default_ZeroValues()
    {
        var ll = new LatLon();
        Assert.That(ll.Lat, Is.EqualTo(0));
        Assert.That(ll.Lon, Is.EqualTo(0));
    }

    [Test]
    public void Constructor_WithValues_SetsProperties()
    {
        var ll = new LatLon(38.89, -77.03);
        Assert.That(ll.Lat, Is.EqualTo(38.89));
        Assert.That(ll.Lon, Is.EqualTo(-77.03));
    }

    [Test]
    public void JsonSerialization_Roundtrip()
    {
        var ll = new LatLon(38.89, -77.03);
        string json = JsonConvert.SerializeObject(ll);
        var deserialized = JsonConvert.DeserializeObject<LatLon>(json);
        Assert.That(deserialized.Lat, Is.EqualTo(38.89));
        Assert.That(deserialized.Lon, Is.EqualTo(-77.03));
    }

    [Test]
    public void Equals_SameValues_ReturnsTrue()
    {
        var a = new LatLon(38.89, -77.03);
        var b = new LatLon(38.89, -77.03);
        Assert.That(a.Equals(b), Is.True);
    }

    [Test]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var a = new LatLon(38.89, -77.03);
        var b = new LatLon(38.90, -77.03);
        Assert.That(a.Equals(b), Is.False);
    }
}

[TestFixture]
public class LocationTests
{
    [Test]
    public void DefaultConstructor_CreatesInstance()
    {
        var loc = new Location();
        Assert.That(loc, Is.Not.Null);
    }

    [Test]
    public void JsonRoundtrip_PreservesCoords()
    {
        var loc = new Location
        {
            Type = "point",
            Shape = "point",
            Coords = new List<LatLon> { new LatLon(59.14, 10.07) },
            Centroid = new LatLon(59.14, 10.07)
        };
        string json = JsonConvert.SerializeObject(loc);
        var deserialized = JsonConvert.DeserializeObject<Location>(json);
        Assert.That(deserialized.Coords, Has.Count.EqualTo(1));
        Assert.That(deserialized.Centroid.Lat, Is.EqualTo(59.14));
    }
}

[TestFixture]
public class StpSymbolModelTests
{
    [Test]
    public void DefaultConstructor_CreatesInstance()
    {
        var sym = new StpSymbol();
        Assert.That(sym, Is.Not.Null);
    }

    [Test]
    public void SymbolId_SetAndGet()
    {
        var sym = new StpSymbol { SymbolId = "SFGPUCI----D---" };
        Assert.That(sym.SymbolId, Is.EqualTo("SFGPUCI----D---"));
    }

    [Test]
    public void Sidc_StructuredEngineObject_PopulatesDeltaCharlieAndSymbolSet()
    {
        // The engine sends sidc as an object: 2525D parts + symbolSet + 2525C legacy
        // (matching the JS SDK and the WebSocketsBridge). Delta is reconstructed from the parts.
        string json = @"{
            ""fsTYPE"": ""unit"",
            ""poid"": ""p1"",
            ""sidc"": { ""partA"": ""1003100000"", ""partB"": ""1211000000"", ""symbolSet"": ""10"", ""legacy"": ""SFGPUCI----D---"" }
        }";
        var sym = JsonConvert.DeserializeObject<StpSymbol>(json);

        Assert.That(sym.DeltaSIDC, Is.EqualTo("10031000001211000000"));   // partA + partB
        Assert.That(sym.Sidc.PartA, Is.EqualTo("1003100000"));
        Assert.That(sym.Sidc.PartB, Is.EqualTo("1211000000"));
        Assert.That(sym.CharlieSIDC, Is.EqualTo("SFGPUCI----D---"));
        Assert.That(sym.SymbolSet, Is.EqualTo("10"));
        Assert.That(sym.SymbolId, Is.EqualTo("SFGPUCI----D---"));         // convenience = legacy

        // Round-trips back out as an object with delta/legacy/symbolSet (matching the JS SDK)
        var jo = JObject.Parse(JsonConvert.SerializeObject(sym));
        Assert.That((string)jo["sidc"]["delta"], Is.EqualTo("10031000001211000000"));
        Assert.That((string)jo["sidc"]["legacy"], Is.EqualTo("SFGPUCI----D---"));
        Assert.That((string)jo["sidc"]["symbolSet"], Is.EqualTo("10"));
    }

    [Test]
    public void Affiliation_SetAndGet()
    {
        var sym = new StpSymbol { Affiliation = Affiliation.friend };
        Assert.That(sym.Affiliation, Is.EqualTo(Affiliation.friend));
    }

    [Test]
    public void Echelon_SetAndGet()
    {
        var sym = new StpSymbol { Echelon = Echelon.brigade };
        Assert.That(sym.Echelon, Is.EqualTo(Echelon.brigade));
    }

    [Test]
    public void Alternates_DefaultNull()
    {
        var sym = new StpSymbol();
        Assert.That(sym.Alternates, Is.Null);
    }

    [Test]
    public void DesignatorDescription_CombinesDesignatorAndParent()
    {
        var sym = new StpSymbol { Designator1 = "1", UnitParent = "2ID" };
        Assert.That(sym.DesignatorDescription, Is.EqualTo("1/2ID"));
    }

    [Test]
    public void DesignatorDescription_NoParent_JustDesignator()
    {
        var sym = new StpSymbol { Designator1 = "A" };
        Assert.That(sym.DesignatorDescription, Is.EqualTo("A"));
    }

    [Test]
    public void ShortDescription_UsesDesignator_WhenAvailable()
    {
        var sym = new StpSymbol { Designator1 = "1", UnitParent = "2ID" };
        Assert.That(sym.ShortDescription, Is.EqualTo("1/2ID"));
    }

    [Test]
    public void IsHq_True_ForHqModifier()
    {
        var sym = new StpSymbol { Modifier = Modifier.hq };
        Assert.That(sym.IsHq, Is.True);
    }

    [Test]
    public void IsHq_False_ForNoModifier()
    {
        var sym = new StpSymbol();
        Assert.That(sym.IsHq, Is.False);
    }

    [Test]
    public void JsonSerialization_ContainsTypeAndPoid()
    {
        var sym = new StpSymbol
        {
            Type = "unit",
            Poid = "test-poid",
            SymbolId = "SFGPUCI----D---"
        };
        string json = JsonConvert.SerializeObject(sym);
        Assert.That(json, Does.Contain("unit"));
        Assert.That(json, Does.Contain("test-poid"));
    }

    [Test]
    public void JsonDeserialization_PopulatesEnums()
    {
        string json = @"{
            ""fsTYPE"": ""unit"",
            ""poid"": ""p1"",
            ""sidc"": ""SFGPUCI----D---"",
            ""affiliation"": ""friend"",
            ""echelon"": ""brigade"",
            ""status"": ""present""
        }";
        var sym = JsonConvert.DeserializeObject<StpSymbol>(json);
        Assert.That(sym!.Affiliation, Is.EqualTo(Affiliation.friend));
        Assert.That(sym.Echelon, Is.EqualTo(Echelon.brigade));
        Assert.That(sym.Status, Is.EqualTo(Status.present));
    }

    [Test]
    public void JsonDeserialization_InvalidEnum_DoesNotThrow()
    {
        string json = @"{
            ""fsTYPE"": ""unit"",
            ""poid"": ""p1"",
            ""affiliation"": ""not_a_real_affiliation""
        }";
        Assert.DoesNotThrow(() => JsonConvert.DeserializeObject<StpSymbol>(json));
    }

    [Test]
    public void JsonDeserialization_NullEnum_DoesNotThrow()
    {
        string json = @"{
            ""fsTYPE"": ""unit"",
            ""poid"": ""p1"",
            ""affiliation"": null
        }";
        var sym = JsonConvert.DeserializeObject<StpSymbol>(json);
        Assert.That(sym!.Affiliation, Is.Null);
    }

    [Test]
    public void Extensions_JsonRoundtrip()
    {
        string json = @"{
            ""fsTYPE"": ""unit"",
            ""poid"": ""p1"",
            ""extensions"": {
                ""appId"": ""testClient"",
                ""priority"": 5
            }
        }";
        var sym = JsonConvert.DeserializeObject<StpSymbol>(json);
        Assert.That(sym!.Extensions, Is.Not.Null);
        Assert.That(sym.Extensions["appId"].ToString(), Is.EqualTo("testClient"));

        string reJson = JsonConvert.SerializeObject(sym);
        Assert.That(reJson, Does.Contain("testClient"));
    }
}

[TestFixture]
public class StpTaskModelTests
{
    [Test]
    public void DefaultConstructor_CreatesInstance()
    {
        var task = new StpTask();
        Assert.That(task, Is.Not.Null);
        Assert.That(task.Type, Is.EqualTo("task"));
    }

    [Test]
    public void TaskProperties_SetAndGet()
    {
        var task = new StpTask
        {
            Name = "Block",
            What = TaskWhat.BLOCK,
            How = TaskHow.DEFEND,
            Why = TaskWhy.DENY,
        };
        Assert.That(task.Name, Is.EqualTo("Block"));
        Assert.That(task.What, Is.EqualTo(TaskWhat.BLOCK));
        Assert.That(task.How, Is.EqualTo(TaskHow.DEFEND));
        Assert.That(task.Why, Is.EqualTo(TaskWhy.DENY));
    }

    [Test]
    public void Description_StripsUnknownOrder()
    {
        var task = new StpTask();
        task.Description = "attack in order to unknown";
        Assert.That(task.Description, Does.Not.Contain("in order to unknown"));
        Assert.That(task.Description, Is.EqualTo("attack"));
    }

    [Test]
    public void Description_ReplacesUnderscores()
    {
        var task = new StpTask();
        task.Description = "movement_to_contact";
        Assert.That(task.Description, Is.EqualTo("movement to contact"));
    }

    [Test]
    public void IsConfirmed_True_WhenUiStatusConfirmed()
    {
        var task = new StpTask { UiStatus = "confirmed" };
        Assert.That(task.IsConfirmed, Is.True);
    }

    [Test]
    public void IsConfirmed_False_Otherwise()
    {
        var task = new StpTask { UiStatus = "pending" };
        Assert.That(task.IsConfirmed, Is.False);
    }

    [Test]
    public void JsonSerialization_Roundtrip()
    {
        var task = new StpTask
        {
            Poid = "task-poid",
            Name = "Attack",
            What = TaskWhat.BLOCK,
            How = TaskHow.ATTACK,
            Who = "unit-poid-1",
            StartTime = 100,
            EndTime = 200
        };
        string json = JsonConvert.SerializeObject(task);
        var deserialized = JsonConvert.DeserializeObject<StpTask>(json);
        Assert.That(deserialized!.Poid, Is.EqualTo("task-poid"));
        Assert.That(deserialized.What, Is.EqualTo(TaskWhat.BLOCK));
        Assert.That(deserialized.How, Is.EqualTo(TaskHow.ATTACK));
        Assert.That(deserialized.Who, Is.EqualTo("unit-poid-1"));
    }

    [Test]
    public void TaskEnums_AllValues_Defined()
    {
        Assert.That(Enum.GetValues(typeof(TaskWhat)).Length, Is.GreaterThan(10));
        Assert.That(Enum.GetValues(typeof(TaskHow)).Length, Is.GreaterThan(10));
        Assert.That(Enum.GetValues(typeof(TaskWhy)).Length, Is.GreaterThan(5));
    }

    [Test]
    public void CommandRelationship_Values_Defined()
    {
        Assert.That(Enum.GetValues(typeof(CommandRelationship)).Length, Is.GreaterThanOrEqualTo(10));
    }
}

[TestFixture]
public class StpCoaTests
{
    [Test]
    public void DefaultConstructor_CreatesInstance()
    {
        var coa = new StpCoa();
        Assert.That(coa, Is.Not.Null);
        Assert.That(coa.Type, Is.EqualTo("coa"));
    }

    [Test]
    public void JsonRoundtrip()
    {
        var coa = new StpCoa { Name = "COA1", Affiliation = "FRIENDLY" };
        string json = JsonConvert.SerializeObject(coa);
        var deserialized = JsonConvert.DeserializeObject<StpCoa>(json);
        Assert.That(deserialized!.Name, Is.EqualTo("COA1"));
    }
}

[TestFixture]
public class StpTaskOrgTests
{
    [Test]
    public void StpTaskOrg_DefaultConstructor()
    {
        var to = new StpTaskOrg();
        Assert.That(to, Is.Not.Null);
        Assert.That(to.Type, Is.EqualTo("task_org"));
    }

    [Test]
    public void StpTaskOrgUnit_DefaultConstructor()
    {
        var tou = new StpTaskOrgUnit();
        Assert.That(tou, Is.Not.Null);
        Assert.That(tou.Type, Is.EqualTo("task_org_unit"));
    }

    [Test]
    public void StpTaskOrgUnit_Properties()
    {
        var tou = new StpTaskOrgUnit { Name = "1/2ID" };
        Assert.That(tou.Name, Is.EqualTo("1/2ID"));
    }

    [Test]
    public void StpTaskOrgRelationship_DefaultConstructor()
    {
        var rel = new StpTaskOrgRelationship();
        Assert.That(rel, Is.Not.Null);
        Assert.That(rel.Type, Is.EqualTo("task_org_relationship"));
    }

    [Test]
    public void StpTaskOrgRelationship_JsonRoundtrip()
    {
        var rel = new StpTaskOrgRelationship
        {
            Parent = "parent-poid",
            Child = "child-poid",
            Relationship = CommandRelationship.organic,
            IsMTOE = true
        };
        string json = JsonConvert.SerializeObject(rel);
        var deserialized = JsonConvert.DeserializeObject<StpTaskOrgRelationship>(json);
        Assert.That(deserialized!.Parent, Is.EqualTo("parent-poid"));
        Assert.That(deserialized.Child, Is.EqualTo("child-poid"));
        Assert.That(deserialized.Relationship, Is.EqualTo(CommandRelationship.organic));
        Assert.That(deserialized.IsMTOE, Is.True);
    }
}

[TestFixture]
public class ObjectSetTests
{
    [Test]
    public void DefaultConstructor_CreatesEmpty()
    {
        var os = new ObjectSet();
        Assert.That(os.Count, Is.EqualTo(0));
    }

    [Test]
    public void Constructor_WithList_PopulatesObjects()
    {
        var objs = new List<StpObject>
        {
            new StpObject { Type = "unit", Poid = "poid1" },
            new StpObject { Type = "task", Poid = "poid2" }
        };
        var os = new ObjectSet(objs);
        Assert.That(os.Count, Is.EqualTo(2));
    }

    [Test]
    public void GetEnumerator_IteratesAll()
    {
        var objs = new List<StpObject>
        {
            new StpObject { Type = "unit", Poid = "p1" },
            new StpObject { Type = "task", Poid = "p2" }
        };
        var os = new ObjectSet(objs);
        int count = 0;
        foreach (var obj in os) count++;
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public void GetTypedObjects_CastsCorrectly()
    {
        var objs = new List<StpObject>
        {
            new StpObject { Type = "unit", Poid = "p1" },
            new StpObject { Type = "task", Poid = "p2" },
            new StpObject { Type = "coa", Poid = "p3" }
        };
        var os = new ObjectSet(objs);
        var typed = os.GetTypedObjects();
        Assert.That(typed[0], Is.InstanceOf<StpSymbol>());
        Assert.That(typed[1], Is.InstanceOf<StpTask>());
        Assert.That(typed[2], Is.InstanceOf<StpCoa>());
    }

    [Test]
    public void JsonRoundtrip()
    {
        var os = new ObjectSet(new List<StpObject>
        {
            new StpObject { Type = "unit", Poid = "p1" }
        });
        string json = JsonConvert.SerializeObject(os);
        var deserialized = JsonConvert.DeserializeObject<ObjectSet>(json);
        Assert.That(deserialized!.Count, Is.EqualTo(1));
    }
}

[TestFixture]
public class DISCodeTests
{
    [Test]
    public void DefaultConstructor_CreatesInstance()
    {
        var dis = new DISCode();
        Assert.That(dis, Is.Not.Null);
    }

    [Test]
    public void Equality_SameValues_AreEqual()
    {
        var a = new DISCode { Kind = 1, Domain = 1, Country = "225", Category = 1 };
        var b = new DISCode { Kind = 1, Domain = 1, Country = "225", Category = 1 };
        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new DISCode { Kind = 1, Domain = 1 };
        var b = new DISCode { Kind = 2, Domain = 1 };
        Assert.That(a, Is.Not.EqualTo(b));
    }
}

[TestFixture]
public class SupportingTypesTests
{
    [Test]
    public void SpeechRecoItem_ConstructorSetsValues()
    {
        var item = new SpeechRecoItem("hello", 0.95, 1.0, 2.5);
        Assert.That(item.Text, Is.EqualTo("hello"));
        Assert.That(item.Confidence, Is.EqualTo(0.95));
        Assert.That(item.StartSec, Is.EqualTo(1.0));
        Assert.That(item.EndSec, Is.EqualTo(2.5));
    }

    [Test]
    public void SketchRecoResult_ConstructorSetsValues()
    {
        var result = new SketchRecoResult(SketchClass.Line, 0.8);
        Assert.That(result.Type, Is.EqualTo(SketchClass.Line));
        Assert.That(result.Confidence, Is.EqualTo(0.8));
    }

    // Size_Constructor test removed: the SDK no longer defines its own Size type;
    // the public API (SendInk) uses System.Drawing.Size for compatibility.

    [Test]
    public void Interval_Constructor()
    {
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 1, 2);
        var interval = new Interval(start, end);
        Assert.That(interval.Start, Is.EqualTo(start));
        Assert.That(interval.End, Is.EqualTo(end));
    }

    [Test]
    public void PlanningScenario_JsonRoundtrip()
    {
        var ps = new PlanningScenario { Name = "Test Scenario", IsValid = true };
        string json = JsonConvert.SerializeObject(ps);
        var deserialized = JsonConvert.DeserializeObject<PlanningScenario>(json);
        Assert.That(deserialized!.Name, Is.EqualTo("Test Scenario"));
        Assert.That(deserialized.IsValid, Is.True);
    }
}
