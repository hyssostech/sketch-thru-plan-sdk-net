using Newtonsoft.Json;
using NUnit.Framework;
using StpSDK;
using System.Collections.Generic;
using System.Linq;

namespace StpSDK.Tests;

[TestFixture]
public class StpSymbolDescriptionTests
{
    [Test]
    public void DesignatorDescription_EmptyWhenNoDesignators()
    {
        var sym = new StpSymbol();
        Assert.That(sym.DesignatorDescription, Is.EqualTo(string.Empty));
    }

    [Test]
    public void DesignatorDescription_Designator1Only()
    {
        var sym = new StpSymbol { Designator1 = "1-1" };
        Assert.That(sym.DesignatorDescription, Is.EqualTo("1-1"));
    }

    [Test]
    public void DesignatorDescription_Designator1AndUnitParent()
    {
        var sym = new StpSymbol { Designator1 = "A", UnitParent = "3ID" };
        Assert.That(sym.DesignatorDescription, Is.EqualTo("A/3ID"));
    }

    [Test]
    public void DesignatorDescription_UnitParentOnly_IncludesSlashParent()
    {
        var sym = new StpSymbol { UnitParent = "3ID" };
        Assert.That(sym.DesignatorDescription, Is.EqualTo("/3ID"));
    }

    [Test]
    public void FullDescription_EmptyWhenCompleteLanguageIsNull()
    {
        var sym = new StpSymbol();
        Assert.That(sym.FullDescription, Is.EqualTo(string.Empty));
    }

    [Test]
    public void FullDescription_StripsLeadingPresent()
    {
        var sym = new StpSymbol { CompleteLanguage = "present friendly infantry" };
        Assert.That(sym.FullDescription, Does.Not.StartWith("present"));
        Assert.That(sym.FullDescription, Does.Contain("infantry"));
    }

    [Test]
    public void FullDescription_AppendsDesignatorIfNotPresent()
    {
        var sym = new StpSymbol
        {
            CompleteLanguage = "friendly infantry",
            Designator1 = "1-1"
        };
        Assert.That(sym.FullDescription, Does.EndWith("1-1"));
    }

    [Test]
    public void FullDescription_DoesNotDuplicateDesignator()
    {
        var sym = new StpSymbol
        {
            CompleteLanguage = "friendly infantry 1-1",
            Designator1 = "1-1"
        };
        int count = sym.FullDescription.Split("1-1").Length - 1;
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void Description_RemovesFriendly()
    {
        var sym = new StpSymbol { CompleteLanguage = "friendly armor battalion" };
        Assert.That(sym.Description, Does.Not.Contain("friendly"));
        Assert.That(sym.Description, Does.Contain("armor"));
    }

    [Test]
    public void Description_RemovesTrailingDesignator()
    {
        var sym = new StpSymbol
        {
            CompleteLanguage = "friendly armor battalion",
            Designator1 = "2-69"
        };
        Assert.That(sym.Description, Does.Not.Contain("2-69"));
    }

    [Test]
    public void ShortDescription_UsesDesignatorIfPresent()
    {
        var sym = new StpSymbol
        {
            CompleteLanguage = "friendly infantry",
            Designator1 = "A"
        };
        Assert.That(sym.ShortDescription, Is.EqualTo("A"));
    }

    [Test]
    public void ShortDescription_FallsBackToUppercaseDescription()
    {
        var sym = new StpSymbol { CompleteLanguage = "armor battalion" };
        Assert.That(sym.ShortDescription, Is.EqualTo(sym.Description.ToUpperInvariant()));
    }

    [Test]
    public void DesigPlusDescription_CombinesBoth()
    {
        var sym = new StpSymbol
        {
            CompleteLanguage = "friendly infantry",
            Designator1 = "A",
            UnitParent = "3ID"
        };
        Assert.That(sym.DesigPlusDescription, Does.StartWith("A/3ID"));
        Assert.That(sym.DesigPlusDescription, Does.Contain("infantry"));
    }

    [Test]
    public void DesigPlusDescription_StripsStatusPrefix()
    {
        var sym = new StpSymbol
        {
            CompleteLanguage = "present infantry",
            Designator1 = "B"
        };
        Assert.That(sym.DesigPlusDescription, Does.Not.Contain("present"));
    }

    [Test]
    public void IsHq_TrueForHqModifier()
    {
        var sym = new StpSymbol { Modifier = Modifier.hq };
        Assert.That(sym.IsHq, Is.True);
    }

    [Test]
    public void IsHq_TrueForDummyHq()
    {
        var sym = new StpSymbol { Modifier = Modifier.dummy_hq };
        Assert.That(sym.IsHq, Is.True);
    }

    [Test]
    public void IsHq_TrueForTaskForceHq()
    {
        var sym = new StpSymbol { Modifier = Modifier.task_force_hq };
        Assert.That(sym.IsHq, Is.True);
    }

    [Test]
    public void IsHq_FalseForNone()
    {
        var sym = new StpSymbol { Modifier = Modifier.none };
        Assert.That(sym.IsHq, Is.False);
    }

    [Test]
    public void IsHq_FalseForDummy()
    {
        var sym = new StpSymbol { Modifier = Modifier.dummy };
        Assert.That(sym.IsHq, Is.False);
    }

    [Test]
    public void SymbolDesignation_UnitType_ReturnsShortDescription()
    {
        var sym = new StpSymbol
        {
            Type = "unit",
            CompleteLanguage = "friendly armor",
            Designator1 = "1-69"
        };
        Assert.That(sym.SymbolDesignation, Is.EqualTo("1-69"));
    }

    [Test]
    public void SymbolDesignation_TgType_ReturnsDesignator1()
    {
        var sym = new StpSymbol
        {
            Type = "tg",
            Designator1 = "PL ALPHA"
        };
        Assert.That(sym.SymbolDesignation, Is.EqualTo("PL ALPHA"));
    }

    [Test]
    public void SymbolDesignation_TgType_NoDesignator_ReturnsNull()
    {
        var sym = new StpSymbol { Type = "tg" };
        Assert.That(sym.SymbolDesignation, Is.Null);
    }
}

[TestFixture]
public class StpItemAlternatesTests
{
    [Test]
    public void Alternates_DefaultIsNull()
    {
        var item = new StpItem();
        Assert.That(item.Alternates, Is.Null);
    }

    [Test]
    public void Alternates_CanBeSetAndRetrieved()
    {
        var primary = new StpSymbol { Poid = "p1", Type = "unit" };
        var alt = new StpSymbol { Poid = "p1", Type = "unit", SymbolId = "ALT" };
        primary.Alternates = new List<StpItem> { alt };

        Assert.That(primary.Alternates, Has.Count.EqualTo(1));
        Assert.That(((StpSymbol)primary.Alternates[0]).SymbolId, Is.EqualTo("ALT"));
    }
}

[TestFixture]
public class StpObjectAsTypedObjectTests
{
    [Test]
    public void AsTypedObject_Unit_ReturnsStpSymbol()
    {
        var obj = new StpObject { Type = "unit", Poid = "u1" };
        var typed = obj.AsTypedObject();
        Assert.That(typed, Is.InstanceOf<StpSymbol>());
        Assert.That(typed.Poid, Is.EqualTo("u1"));
    }

    [Test]
    public void AsTypedObject_Tg_ReturnsStpSymbol()
    {
        var obj = new StpObject { Type = "tg", Poid = "tg1" };
        var typed = obj.AsTypedObject();
        Assert.That(typed, Is.InstanceOf<StpSymbol>());
    }

    [Test]
    public void AsTypedObject_Mootw_ReturnsStpSymbol()
    {
        var obj = new StpObject { Type = "mootw", Poid = "m1" };
        var typed = obj.AsTypedObject();
        Assert.That(typed, Is.InstanceOf<StpSymbol>());
    }

    [Test]
    public void AsTypedObject_Task_ReturnsStpTask()
    {
        var obj = new StpObject { Type = "task", Poid = "t1" };
        var typed = obj.AsTypedObject();
        Assert.That(typed, Is.InstanceOf<StpTask>());
    }

    [Test]
    public void AsTypedObject_TaskOrg_ReturnsStpTaskOrg()
    {
        var obj = new StpObject { Type = "task_org", Poid = "to1" };
        var typed = obj.AsTypedObject();
        Assert.That(typed, Is.InstanceOf<StpTaskOrg>());
    }

    [Test]
    public void AsTypedObject_TaskOrgUnit_ReturnsStpTaskOrgUnit()
    {
        var obj = new StpObject { Type = "task_org_unit", Poid = "tou1" };
        var typed = obj.AsTypedObject();
        Assert.That(typed, Is.InstanceOf<StpTaskOrgUnit>());
    }

    [Test]
    public void AsTypedObject_TaskOrgRelationship_ReturnsStpTaskOrgRelationship()
    {
        var obj = new StpObject { Type = "task_org_relationship", Poid = "tor1" };
        var typed = obj.AsTypedObject();
        Assert.That(typed, Is.InstanceOf<StpTaskOrgRelationship>());
    }

    [Test]
    public void AsTypedObject_Coa_ReturnsStpCoa()
    {
        var obj = new StpObject { Type = "coa", Poid = "c1" };
        var typed = obj.AsTypedObject();
        Assert.That(typed, Is.InstanceOf<StpCoa>());
    }

    [Test]
    public void AsTypedObject_UnknownType_ReturnsSelf()
    {
        var obj = new StpObject { Type = "something_else", Poid = "x1" };
        var typed = obj.AsTypedObject();
        Assert.That(typed, Is.SameAs(obj));
    }

    [Test]
    public void AsTypedObject_NullType_ReturnsSelf()
    {
        var obj = new StpObject { Poid = "n1" };
        var typed = obj.AsTypedObject();
        Assert.That(typed, Is.SameAs(obj));
    }
}

[TestFixture]
public class ObjectSetGetTypedObjectsTests
{
    [Test]
    public void GetTypedObjects_ConvertsEachByType()
    {
        var os = new ObjectSet(new List<StpObject>
        {
            new StpObject { Type = "unit", Poid = "u1" },
            new StpObject { Type = "task", Poid = "t1" },
            new StpObject { Type = "coa", Poid = "c1" },
            new StpObject { Type = "other", Poid = "o1" }
        });

        var typed = os.GetTypedObjects();
        Assert.That(typed, Has.Count.EqualTo(4));
        Assert.That(typed[0], Is.InstanceOf<StpSymbol>());
        Assert.That(typed[1], Is.InstanceOf<StpTask>());
        Assert.That(typed[2], Is.InstanceOf<StpCoa>());
        Assert.That(typed[3], Is.InstanceOf<StpObject>());
    }

    [Test]
    public void GetTypedObjects_EmptyList_ReturnsEmpty()
    {
        var os = new ObjectSet();
        var typed = os.GetTypedObjects();
        Assert.That(typed, Is.Empty);
    }

    [Test]
    public void ObjectSet_Enumerable_IteratesObjects()
    {
        var os = new ObjectSet(new List<StpObject>
        {
            new StpObject { Type = "unit", Poid = "a" },
            new StpObject { Type = "unit", Poid = "b" }
        });

        var poids = os.Select(o => o.Poid).ToList();
        Assert.That(poids, Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public void ObjectSet_Count_MatchesObjectsList()
    {
        var os = new ObjectSet(new List<StpObject>
        {
            new StpObject { Poid = "1" },
            new StpObject { Poid = "2" },
            new StpObject { Poid = "3" }
        });
        Assert.That(os.Count, Is.EqualTo(3));
    }

    [Test]
    public void ObjectSet_JsonRoundtrip_PreservesObjects()
    {
        var os = new ObjectSet(new List<StpObject>
        {
            new StpObject { Type = "unit", Poid = "u1" },
            new StpObject { Type = "task", Poid = "t1" }
        });

        var json = JsonConvert.SerializeObject(os);
        var deserialized = JsonConvert.DeserializeObject<ObjectSet>(json);

        Assert.That(deserialized.Count, Is.EqualTo(2));
        Assert.That(deserialized.Objects[0].Poid, Is.EqualTo("u1"));
        Assert.That(deserialized.Objects[1].Type, Is.EqualTo("task"));
    }
}

[TestFixture]
public class LocationModelTests
{
    [Test]
    public void Location_JsonRoundtrip()
    {
        var loc = new Location
        {
            Type = "point",
            Shape = "point",
            Coords = new List<LatLon> { new LatLon(59.14, 10.07) },
            Centroid = new LatLon(59.14, 10.07)
        };

        var json = JsonConvert.SerializeObject(loc);
        var deserialized = JsonConvert.DeserializeObject<Location>(json);

        Assert.That(deserialized.Type, Is.EqualTo("point"));
        Assert.That(deserialized.Coords, Has.Count.EqualTo(1));
        Assert.That(deserialized.Coords[0].Lat, Is.EqualTo(59.14));
        Assert.That(deserialized.Centroid.Lon, Is.EqualTo(10.07));
    }

    [Test]
    public void Location_MultipleCoords_Line()
    {
        var loc = new Location
        {
            Type = "line",
            Shape = "line",
            Coords = new List<LatLon>
            {
                new LatLon(59.0, 10.0),
                new LatLon(59.1, 10.1),
                new LatLon(59.2, 10.2)
            }
        };

        var json = JsonConvert.SerializeObject(loc);
        var deserialized = JsonConvert.DeserializeObject<Location>(json);

        Assert.That(deserialized.Coords, Has.Count.EqualTo(3));
    }
}

[TestFixture]
public class ConvertToTranscriptionTests
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
    }

    [Test]
    public void Null_ReturnsEmpty()
    {
        var result = _recognizer.ConvertToTranscription(null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void WhitespaceOnly_ReturnsEmpty()
    {
        var result = _recognizer.ConvertToTranscription("   ");
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ValidInput_ReturnsSingleItemWithConfidence1()
    {
        var result = _recognizer.ConvertToTranscription("friendly infantry");
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Text, Is.EqualTo("friendly infantry"));
        Assert.That(result[0].Confidence, Is.EqualTo(1.0));
    }
}
