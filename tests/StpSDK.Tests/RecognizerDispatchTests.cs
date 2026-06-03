using NUnit.Framework;
using StpSDK;
using System;
using System.Collections.Generic;

namespace StpSDK.Tests;

[TestFixture]
public class RecognizerDispatchTests
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

    #region 1. SymbolAdded

    [Test]
    public void SymbolAdded_DispatchesPrimaryAndAlternates()
    {
        string receivedPoid = null;
        StpItem receivedSymbol = null;
        bool receivedIsUndo = false;

        _recognizer.OnSymbolAdded += (poid, symbol, isUndo) =>
        {
            receivedPoid = poid;
            receivedSymbol = symbol;
            receivedIsUndo = isUndo;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""SymbolAdded"",
            ""params"": {
                ""alternates"": [
                    { ""fsTYPE"": ""unit"", ""poid"": ""sym-1"", ""sidc"": ""SFGPUCI----D---"", ""affiliation"": ""friend"" },
                    { ""fsTYPE"": ""unit"", ""poid"": ""sym-1"", ""sidc"": ""SFGPUCA----D---"", ""affiliation"": ""friend"" },
                    { ""fsTYPE"": ""unit"", ""poid"": ""sym-1"", ""sidc"": ""SFGPUCR----D---"", ""affiliation"": ""friend"" }
                ],
                ""isUndo"": true
            }
        }");

        Assert.That(receivedPoid, Is.EqualTo("sym-1"));
        Assert.That(receivedSymbol, Is.Not.Null);
        Assert.That(receivedSymbol.Poid, Is.EqualTo("sym-1"));
        Assert.That(receivedSymbol.Alternates, Has.Count.EqualTo(2));
        Assert.That(receivedIsUndo, Is.True);
    }

    #endregion

    #region 2. SymbolModified

    [Test]
    public void SymbolModified_DispatchesPoidAndSymbol()
    {
        string receivedPoid = null;
        StpItem receivedSymbol = null;
        bool receivedIsUndo = false;

        _recognizer.OnSymbolModified += (poid, symbol, isUndo) =>
        {
            receivedPoid = poid;
            receivedSymbol = symbol;
            receivedIsUndo = isUndo;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""SymbolModified"",
            ""params"": {
                ""poid"": ""sym-42"",
                ""symbol"": { ""fsTYPE"": ""unit"", ""poid"": ""sym-42"", ""sidc"": ""SFGPUCI----D---"" },
                ""isUndo"": true
            }
        }");

        Assert.That(receivedPoid, Is.EqualTo("sym-42"));
        Assert.That(receivedSymbol, Is.Not.Null);
        Assert.That(receivedSymbol.Poid, Is.EqualTo("sym-42"));
        Assert.That(receivedIsUndo, Is.True);
    }

    #endregion

    #region 3. SymbolDeleted

    [Test]
    public void SymbolDeleted_DispatchesPoidAndIsUndo()
    {
        string receivedPoid = null;
        bool receivedIsUndo = false;

        _recognizer.OnSymbolDeleted += (poid, isUndo) =>
        {
            receivedPoid = poid;
            receivedIsUndo = isUndo;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""SymbolDeleted"",
            ""params"": { ""poid"": ""sym-99"", ""isUndo"": true }
        }");

        Assert.That(receivedPoid, Is.EqualTo("sym-99"));
        Assert.That(receivedIsUndo, Is.True);
    }

    #endregion

    #region 4. SymbolReport

    [Test]
    public void SymbolReport_DispatchesPoidAndSymbol()
    {
        string receivedPoid = null;
        StpItem receivedSymbol = null;

        _recognizer.OnSymbolReport += (poid, symbol) =>
        {
            receivedPoid = poid;
            receivedSymbol = symbol;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""SymbolReport"",
            ""params"": {
                ""poid"": ""sym-report-1"",
                ""symbol"": { ""fsTYPE"": ""tg"", ""poid"": ""sym-report-1"", ""sidc"": ""GFGPGLB----K---"" }
            }
        }");

        Assert.That(receivedPoid, Is.EqualTo("sym-report-1"));
        Assert.That(receivedSymbol, Is.Not.Null);
        Assert.That(receivedSymbol.Poid, Is.EqualTo("sym-report-1"));
    }

    #endregion

    #region 5. SymbolEdited (fires OnSymbolEdited and OnSymbolEditedExt)

    [Test]
    public void SymbolEdited_FiresBothBasicAndExtEvents()
    {
        string basicOperation = null;
        Location basicLocation = null;
        string extOperation = null;
        Location extLocation = null;
        Dictionary<string, string> extProperties = null;

        _recognizer.OnSymbolEdited += (operation, location) =>
        {
            basicOperation = operation;
            basicLocation = location;
        };

        _recognizer.OnSymbolEditedExt += (operation, location, properties) =>
        {
            extOperation = operation;
            extLocation = location;
            extProperties = properties;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""SymbolEdited"",
            ""params"": {
                ""operation"": ""move"",
                ""location"": {
                    ""fsTYPE"": ""location"",
                    ""shape"": ""point"",
                    ""coords"": [{ ""lat"": 59.14, ""lon"": 10.07 }],
                    ""centroid"": { ""lat"": 59.14, ""lon"": 10.07 }
                },
                ""properties"": { ""poid"": ""sym-5"", ""designation"": ""Alpha"" }
            }
        }");

        Assert.That(basicOperation, Is.EqualTo("move"));
        Assert.That(basicLocation, Is.Not.Null);
        Assert.That(basicLocation.Shape, Is.EqualTo("point"));
        Assert.That(basicLocation.Coords, Has.Count.EqualTo(1));

        Assert.That(extOperation, Is.EqualTo("move"));
        Assert.That(extLocation, Is.Not.Null);
        Assert.That(extProperties, Is.Not.Null);
        Assert.That(extProperties["poid"], Is.EqualTo("sym-5"));
        Assert.That(extProperties["designation"], Is.EqualTo("Alpha"));
    }

    #endregion

    #region 6. TaskAdded

    [Test]
    public void TaskAdded_DispatchesWithAlternatesAndTaskPoids()
    {
        string receivedPoid = null;
        StpTask receivedTask = null;
        List<string> receivedTaskPoids = null;
        bool receivedIsUndo = false;

        _recognizer.OnTaskAdded += (poid, task, taskPoids, isUndo) =>
        {
            receivedPoid = poid;
            receivedTask = task;
            receivedTaskPoids = taskPoids;
            receivedIsUndo = isUndo;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""TaskAdded"",
            ""params"": {
                ""poid"": ""task-1"",
                ""alternates"": [
                    { ""fsTYPE"": ""task"", ""poid"": ""task-1"", ""name"": ""Attack"", ""what"": ""ATTACK"" },
                    { ""fsTYPE"": ""task"", ""poid"": ""task-1"", ""name"": ""Assault"", ""what"": ""AMBUSH"" }
                ],
                ""taskPoids"": [""tg-1"", ""tg-2"", ""tg-3""],
                ""isUndo"": false
            }
        }");

        Assert.That(receivedPoid, Is.EqualTo("task-1"));
        Assert.That(receivedTask, Is.Not.Null);
        Assert.That(receivedTask.Name, Is.EqualTo("Attack"));
        Assert.That(receivedTask.Alternates, Has.Count.EqualTo(1));
        Assert.That(receivedTaskPoids, Has.Count.EqualTo(3));
        Assert.That(receivedTaskPoids[0], Is.EqualTo("tg-1"));
        Assert.That(receivedIsUndo, Is.False);
    }

    #endregion

    #region 7. TaskModified

    [Test]
    public void TaskModified_DispatchesWithAlternatesAndTaskPoids()
    {
        string receivedPoid = null;
        StpTask receivedTask = null;
        List<string> receivedTaskPoids = null;
        bool receivedIsUndo = false;

        _recognizer.OnTaskModified += (poid, task, taskPoids, isUndo) =>
        {
            receivedPoid = poid;
            receivedTask = task;
            receivedTaskPoids = taskPoids;
            receivedIsUndo = isUndo;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""TaskModified"",
            ""params"": {
                ""poid"": ""task-mod-1"",
                ""alternates"": [
                    { ""fsTYPE"": ""task"", ""poid"": ""task-mod-1"", ""name"": ""Defend"", ""how"": ""DEFEND"" }
                ],
                ""taskPoids"": [""tg-10""],
                ""isUndo"": true
            }
        }");

        Assert.That(receivedPoid, Is.EqualTo("task-mod-1"));
        Assert.That(receivedTask, Is.Not.Null);
        Assert.That(receivedTask.Name, Is.EqualTo("Defend"));
        Assert.That(receivedTaskPoids, Has.Count.EqualTo(1));
        Assert.That(receivedIsUndo, Is.True);
    }

    #endregion

    #region 8. TaskDeleted

    [Test]
    public void TaskDeleted_DispatchesPoidAndIsUndo()
    {
        string receivedPoid = null;
        bool receivedIsUndo = false;

        _recognizer.OnTaskDeleted += (poid, isUndo) =>
        {
            receivedPoid = poid;
            receivedIsUndo = isUndo;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""TaskDeleted"",
            ""params"": { ""poid"": ""task-del-1"", ""isUndo"": true }
        }");

        Assert.That(receivedPoid, Is.EqualTo("task-del-1"));
        Assert.That(receivedIsUndo, Is.True);
    }

    #endregion

    #region 9. TaskOrgAdded

    [Test]
    public void TaskOrgAdded_DispatchesTaskOrg()
    {
        string receivedPoid = null;
        StpTaskOrg receivedTaskOrg = null;
        bool receivedIsUndo = false;

        _recognizer.OnTaskOrgAdded += (poid, taskOrg, isUndo) =>
        {
            receivedPoid = poid;
            receivedTaskOrg = taskOrg;
            receivedIsUndo = isUndo;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""TaskOrgAdded"",
            ""params"": {
                ""poid"": ""to-1"",
                ""taskOrg"": { ""fsTYPE"": ""task_org"", ""poid"": ""to-1"", ""name"": ""1st Brigade TO"", ""affiliation"": ""FRIENDLY"" },
                ""isUndo"": false
            }
        }");

        Assert.That(receivedPoid, Is.EqualTo("to-1"));
        Assert.That(receivedTaskOrg, Is.Not.Null);
        Assert.That(receivedTaskOrg.Name, Is.EqualTo("1st Brigade TO"));
        Assert.That(receivedIsUndo, Is.False);
    }

    #endregion

    #region 10. TaskOrgModified

    [Test]
    public void TaskOrgModified_DispatchesTaskOrg()
    {
        string receivedPoid = null;
        StpTaskOrg receivedTaskOrg = null;
        bool receivedIsUndo = false;

        _recognizer.OnTaskOrgModified += (poid, taskOrg, isUndo) =>
        {
            receivedPoid = poid;
            receivedTaskOrg = taskOrg;
            receivedIsUndo = isUndo;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""TaskOrgModified"",
            ""params"": {
                ""poid"": ""to-mod-1"",
                ""taskOrg"": { ""fsTYPE"": ""task_org"", ""poid"": ""to-mod-1"", ""name"": ""Updated TO"" },
                ""isUndo"": true
            }
        }");

        Assert.That(receivedPoid, Is.EqualTo("to-mod-1"));
        Assert.That(receivedTaskOrg, Is.Not.Null);
        Assert.That(receivedTaskOrg.Name, Is.EqualTo("Updated TO"));
        Assert.That(receivedIsUndo, Is.True);
    }

    #endregion

    #region 11. TaskOrgDeleted

    [Test]
    public void TaskOrgDeleted_DispatchesPoidAndIsUndo()
    {
        string receivedPoid = null;
        bool receivedIsUndo = false;

        _recognizer.OnTaskOrgDeleted += (poid, isUndo) =>
        {
            receivedPoid = poid;
            receivedIsUndo = isUndo;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""TaskOrgDeleted"",
            ""params"": { ""poid"": ""to-del-1"", ""isUndo"": false }
        }");

        Assert.That(receivedPoid, Is.EqualTo("to-del-1"));
        Assert.That(receivedIsUndo, Is.False);
    }

    #endregion

    #region 12. TaskOrgUnitAdded

    [Test]
    public void TaskOrgUnitAdded_DispatchesUnit()
    {
        string receivedPoid = null;
        StpTaskOrgUnit receivedUnit = null;
        bool receivedIsUndo = false;

        _recognizer.OnTaskOrgUnitAdded += (poid, unit, isUndo) =>
        {
            receivedPoid = poid;
            receivedUnit = unit;
            receivedIsUndo = isUndo;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""TaskOrgUnitAdded"",
            ""params"": {
                ""poid"": ""tou-1"",
                ""toUnit"": { ""fsTYPE"": ""task_org_unit"", ""poid"": ""tou-1"", ""name"": ""Alpha Company"", ""unitType"": ""infantry"" },
                ""isUndo"": false
            }
        }");

        Assert.That(receivedPoid, Is.EqualTo("tou-1"));
        Assert.That(receivedUnit, Is.Not.Null);
        Assert.That(receivedUnit.Name, Is.EqualTo("Alpha Company"));
        Assert.That(receivedUnit.UnitType, Is.EqualTo("infantry"));
        Assert.That(receivedIsUndo, Is.False);
    }

    #endregion

    #region 13. TaskOrgUnitModified

    [Test]
    public void TaskOrgUnitModified_DispatchesUnit()
    {
        string receivedPoid = null;
        StpTaskOrgUnit receivedUnit = null;
        bool receivedIsUndo = false;

        _recognizer.OnTaskOrgUnitModified += (poid, unit, isUndo) =>
        {
            receivedPoid = poid;
            receivedUnit = unit;
            receivedIsUndo = isUndo;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""TaskOrgUnitModified"",
            ""params"": {
                ""poid"": ""tou-mod-1"",
                ""toUnit"": { ""fsTYPE"": ""task_org_unit"", ""poid"": ""tou-mod-1"", ""name"": ""Bravo Company"" },
                ""isUndo"": true
            }
        }");

        Assert.That(receivedPoid, Is.EqualTo("tou-mod-1"));
        Assert.That(receivedUnit, Is.Not.Null);
        Assert.That(receivedUnit.Name, Is.EqualTo("Bravo Company"));
        Assert.That(receivedIsUndo, Is.True);
    }

    #endregion

    #region 14. TaskOrgUnitDeleted

    [Test]
    public void TaskOrgUnitDeleted_DispatchesPoidAndIsUndo()
    {
        string receivedPoid = null;
        bool receivedIsUndo = false;

        _recognizer.OnTaskOrgUnitDeleted += (poid, isUndo) =>
        {
            receivedPoid = poid;
            receivedIsUndo = isUndo;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""TaskOrgUnitDeleted"",
            ""params"": { ""poid"": ""tou-del-1"", ""isUndo"": true }
        }");

        Assert.That(receivedPoid, Is.EqualTo("tou-del-1"));
        Assert.That(receivedIsUndo, Is.True);
    }

    #endregion

    #region 15. TaskOrgRelationshipAdded

    [Test]
    public void TaskOrgRelationshipAdded_DispatchesRelationship()
    {
        string receivedPoid = null;
        StpTaskOrgRelationship receivedRel = null;
        bool receivedIsUndo = false;

        _recognizer.OnTaskOrgRelationshipAdded += (poid, rel, isUndo) =>
        {
            receivedPoid = poid;
            receivedRel = rel;
            receivedIsUndo = isUndo;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""TaskOrgRelationshipAdded"",
            ""params"": {
                ""poid"": ""tor-1"",
                ""toRelationship"": { ""fsTYPE"": ""task_org_relationship"", ""poid"": ""tor-1"", ""parent"": ""unit-A"", ""child"": ""unit-B"", ""relationship"": ""attached"" },
                ""isUndo"": false
            }
        }");

        Assert.That(receivedPoid, Is.EqualTo("tor-1"));
        Assert.That(receivedRel, Is.Not.Null);
        Assert.That(receivedRel.Parent, Is.EqualTo("unit-A"));
        Assert.That(receivedRel.Child, Is.EqualTo("unit-B"));
        Assert.That(receivedRel.Relationship, Is.EqualTo(CommandRelationship.attached));
        Assert.That(receivedIsUndo, Is.False);
    }

    #endregion

    #region 16. TaskOrgRelationshipModified

    [Test]
    public void TaskOrgRelationshipModified_DispatchesRelationship()
    {
        string receivedPoid = null;
        StpTaskOrgRelationship receivedRel = null;
        bool receivedIsUndo = false;

        _recognizer.OnTaskOrgRelationshipModified += (poid, rel, isUndo) =>
        {
            receivedPoid = poid;
            receivedRel = rel;
            receivedIsUndo = isUndo;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""TaskOrgRelationshipModified"",
            ""params"": {
                ""poid"": ""tor-mod-1"",
                ""toRelationship"": { ""fsTYPE"": ""task_org_relationship"", ""poid"": ""tor-mod-1"", ""parent"": ""unit-X"", ""child"": ""unit-Y"", ""relationship"": ""opcon"" },
                ""isUndo"": true
            }
        }");

        Assert.That(receivedPoid, Is.EqualTo("tor-mod-1"));
        Assert.That(receivedRel, Is.Not.Null);
        Assert.That(receivedRel.Parent, Is.EqualTo("unit-X"));
        Assert.That(receivedRel.Child, Is.EqualTo("unit-Y"));
        Assert.That(receivedRel.Relationship, Is.EqualTo(CommandRelationship.opcon));
        Assert.That(receivedIsUndo, Is.True);
    }

    #endregion

    #region 17. TaskOrgRelationshipDeleted

    [Test]
    public void TaskOrgRelationshipDeleted_DispatchesPoidAndIsUndo()
    {
        string receivedPoid = null;
        bool receivedIsUndo = false;

        _recognizer.OnTaskOrgRelationshipDeleted += (poid, isUndo) =>
        {
            receivedPoid = poid;
            receivedIsUndo = isUndo;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""TaskOrgRelationshipDeleted"",
            ""params"": { ""poid"": ""tor-del-1"", ""isUndo"": false }
        }");

        Assert.That(receivedPoid, Is.EqualTo("tor-del-1"));
        Assert.That(receivedIsUndo, Is.False);
    }

    #endregion

    #region 18. TaskOrgSwitched

    [Test]
    public void TaskOrgSwitched_DispatchesTaskOrg()
    {
        StpTaskOrg receivedTaskOrg = null;

        _recognizer.OnTaskOrgSwitched += (taskOrg) =>
        {
            receivedTaskOrg = taskOrg;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""TaskOrgSwitched"",
            ""params"": {
                ""taskOrg"": { ""fsTYPE"": ""task_org"", ""poid"": ""to-switched-1"", ""name"": ""2nd Brigade TO"" }
            }
        }");

        Assert.That(receivedTaskOrg, Is.Not.Null);
        Assert.That(receivedTaskOrg.Poid, Is.EqualTo("to-switched-1"));
        Assert.That(receivedTaskOrg.Name, Is.EqualTo("2nd Brigade TO"));
    }

    #endregion

    #region 19. CoaAdded

    [Test]
    public void CoaAdded_DispatchesCoa()
    {
        string receivedPoid = null;
        StpCoa receivedCoa = null;
        bool receivedIsUndo = false;

        _recognizer.OnCoaAdded += (poid, coa, isUndo) =>
        {
            receivedPoid = poid;
            receivedCoa = coa;
            receivedIsUndo = isUndo;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""CoaAdded"",
            ""params"": {
                ""poid"": ""coa-1"",
                ""coa"": { ""fsTYPE"": ""coa"", ""poid"": ""coa-1"", ""name"": ""COA Alpha"", ""affiliation"": ""FRIENDLY"" },
                ""isUndo"": false
            }
        }");

        Assert.That(receivedPoid, Is.EqualTo("coa-1"));
        Assert.That(receivedCoa, Is.Not.Null);
        Assert.That(receivedCoa.Name, Is.EqualTo("COA Alpha"));
        Assert.That(receivedIsUndo, Is.False);
    }

    #endregion

    #region 20. CoaModified

    [Test]
    public void CoaModified_DispatchesCoa()
    {
        string receivedPoid = null;
        StpCoa receivedCoa = null;
        bool receivedIsUndo = false;

        _recognizer.OnCoaModified += (poid, coa, isUndo) =>
        {
            receivedPoid = poid;
            receivedCoa = coa;
            receivedIsUndo = isUndo;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""CoaModified"",
            ""params"": {
                ""poid"": ""coa-mod-1"",
                ""coa"": { ""fsTYPE"": ""coa"", ""poid"": ""coa-mod-1"", ""name"": ""COA Beta Updated"" },
                ""isUndo"": true
            }
        }");

        Assert.That(receivedPoid, Is.EqualTo("coa-mod-1"));
        Assert.That(receivedCoa, Is.Not.Null);
        Assert.That(receivedCoa.Name, Is.EqualTo("COA Beta Updated"));
        Assert.That(receivedIsUndo, Is.True);
    }

    #endregion

    #region 21. CoaDeleted

    [Test]
    public void CoaDeleted_DispatchesPoidAndIsUndo()
    {
        string receivedPoid = null;
        bool receivedIsUndo = false;

        _recognizer.OnCoaDeleted += (poid, isUndo) =>
        {
            receivedPoid = poid;
            receivedIsUndo = isUndo;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""CoaDeleted"",
            ""params"": { ""poid"": ""coa-del-1"", ""isUndo"": true }
        }");

        Assert.That(receivedPoid, Is.EqualTo("coa-del-1"));
        Assert.That(receivedIsUndo, Is.True);
    }

    #endregion

    #region 22. CoaSwitched

    [Test]
    public void CoaSwitched_DispatchesCoa()
    {
        StpCoa receivedCoa = null;

        _recognizer.OnCoaSwitched += (coa) =>
        {
            receivedCoa = coa;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""CoaSwitched"",
            ""params"": {
                ""coa"": { ""fsTYPE"": ""coa"", ""poid"": ""coa-switched-1"", ""name"": ""COA Gamma"" }
            }
        }");

        Assert.That(receivedCoa, Is.Not.Null);
        Assert.That(receivedCoa.Poid, Is.EqualTo("coa-switched-1"));
        Assert.That(receivedCoa.Name, Is.EqualTo("COA Gamma"));
    }

    #endregion

    #region 23. RoleSwitched

    [Test]
    public void RoleSwitched_DispatchesRole()
    {
        string receivedRole = null;

        _recognizer.OnRoleSwitched += (role) =>
        {
            receivedRole = role;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""RoleSwitched"",
            ""params"": { ""role"": ""FRIENDLY_CO"" }
        }");

        Assert.That(receivedRole, Is.EqualTo("FRIENDLY_CO"));
    }

    #endregion

    #region 24. AutoTaskingSwitched

    [Test]
    public void AutoTaskingSwitched_DispatchesIsEnabled()
    {
        bool? receivedIsEnabled = null;

        _recognizer.OnAutoTaskingSwitched += (isEnabled) =>
        {
            receivedIsEnabled = isEnabled;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""AutoTaskingSwitched"",
            ""params"": { ""isEnabled"": true }
        }");

        Assert.That(receivedIsEnabled, Is.Not.Null);
        Assert.That(receivedIsEnabled.Value, Is.True);
    }

    [Test]
    public void AutoTaskingSwitched_DispatchesFalse()
    {
        bool? receivedIsEnabled = null;

        _recognizer.OnAutoTaskingSwitched += (isEnabled) =>
        {
            receivedIsEnabled = isEnabled;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""AutoTaskingSwitched"",
            ""params"": { ""isEnabled"": false }
        }");

        Assert.That(receivedIsEnabled, Is.Not.Null);
        Assert.That(receivedIsEnabled.Value, Is.False);
    }

    #endregion

    #region 25. StpMessage

    [Test]
    public void StpMessage_DispatchesLevelAndMessage()
    {
        StpRecognizer.StpMessageLevel? receivedLevel = null;
        string receivedMsg = null;

        _recognizer.OnStpMessage += (level, msg) =>
        {
            receivedLevel = level;
            receivedMsg = msg;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""StpMessage"",
            ""params"": { ""message"": ""Something went wrong"", ""level"": ""Error"" }
        }");

        Assert.That(receivedLevel, Is.EqualTo(StpRecognizer.StpMessageLevel.Error));
        Assert.That(receivedMsg, Is.EqualTo("Something went wrong"));
    }

    [Test]
    public void StpMessage_DispatchesWarningLevel()
    {
        StpRecognizer.StpMessageLevel? receivedLevel = null;
        string receivedMsg = null;

        _recognizer.OnStpMessage += (level, msg) =>
        {
            receivedLevel = level;
            receivedMsg = msg;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""StpMessage"",
            ""params"": { ""message"": ""Caution advised"", ""level"": ""Warning"" }
        }");

        Assert.That(receivedLevel, Is.EqualTo(StpRecognizer.StpMessageLevel.Warning));
        Assert.That(receivedMsg, Is.EqualTo("Caution advised"));
    }

    #endregion

    #region 26. Command (fires OnCommand and OnCommandExt)

    [Test]
    public void Command_FiresBothBasicAndExtEvents()
    {
        string basicOperation = null;
        Location basicLocation = null;
        string extOperation = null;
        Location extLocation = null;
        Dictionary<string, string> extProperties = null;

        _recognizer.OnCommand += (operation, location) =>
        {
            basicOperation = operation;
            basicLocation = location;
        };

        _recognizer.OnCommandExt += (operation, location, properties) =>
        {
            extOperation = operation;
            extLocation = location;
            extProperties = properties;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""Command"",
            ""params"": {
                ""operation"": ""undo"",
                ""location"": {
                    ""fsTYPE"": ""location"",
                    ""shape"": ""area"",
                    ""coords"": [
                        { ""lat"": 59.0, ""lon"": 10.0 },
                        { ""lat"": 59.1, ""lon"": 10.1 },
                        { ""lat"": 59.2, ""lon"": 10.2 }
                    ],
                    ""centroid"": { ""lat"": 59.1, ""lon"": 10.1 }
                },
                ""properties"": { ""scope"": ""all"", ""count"": ""3"" }
            }
        }");

        Assert.That(basicOperation, Is.EqualTo("undo"));
        Assert.That(basicLocation, Is.Not.Null);
        Assert.That(basicLocation.Shape, Is.EqualTo("area"));
        Assert.That(basicLocation.Coords, Has.Count.EqualTo(3));

        Assert.That(extOperation, Is.EqualTo("undo"));
        Assert.That(extLocation, Is.Not.Null);
        Assert.That(extProperties, Is.Not.Null);
        Assert.That(extProperties["scope"], Is.EqualTo("all"));
        Assert.That(extProperties["count"], Is.EqualTo("3"));
    }

    #endregion

    #region 27. MapOperation (fires OnMapOperation and OnMapOperationExt)

    [Test]
    public void MapOperation_FiresBothBasicAndExtEvents()
    {
        string basicOperation = null;
        Location basicLocation = null;
        string extOperation = null;
        Location extLocation = null;
        Dictionary<string, string> extProperties = null;

        _recognizer.OnMapOperation += (operation, location) =>
        {
            basicOperation = operation;
            basicLocation = location;
        };

        _recognizer.OnMapOperationExt += (operation, location, properties) =>
        {
            extOperation = operation;
            extLocation = location;
            extProperties = properties;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""MapOperation"",
            ""params"": {
                ""operation"": ""zoom"",
                ""location"": {
                    ""fsTYPE"": ""location"",
                    ""shape"": ""point"",
                    ""coords"": [{ ""lat"": 40.0, ""lon"": -74.0 }],
                    ""centroid"": { ""lat"": 40.0, ""lon"": -74.0 }
                },
                ""properties"": { ""level"": ""5"", ""direction"": ""in"" }
            }
        }");

        Assert.That(basicOperation, Is.EqualTo("zoom"));
        Assert.That(basicLocation, Is.Not.Null);
        Assert.That(basicLocation.Shape, Is.EqualTo("point"));

        Assert.That(extOperation, Is.EqualTo("zoom"));
        Assert.That(extLocation, Is.Not.Null);
        Assert.That(extProperties, Is.Not.Null);
        Assert.That(extProperties["level"], Is.EqualTo("5"));
        Assert.That(extProperties["direction"], Is.EqualTo("in"));
    }

    #endregion

    #region 28. SpeechRecognized

    [Test]
    public void SpeechRecognized_DispatchesPhrases()
    {
        List<string> receivedPhrases = null;

        _recognizer.OnSpeechRecognized += (phrases) =>
        {
            receivedPhrases = phrases;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""SpeechRecognized"",
            ""params"": { ""phrases"": [""attack position"", ""attack position alpha"", ""attack""] }
        }");

        Assert.That(receivedPhrases, Is.Not.Null);
        Assert.That(receivedPhrases, Has.Count.EqualTo(3));
        Assert.That(receivedPhrases[0], Is.EqualTo("attack position"));
        Assert.That(receivedPhrases[1], Is.EqualTo("attack position alpha"));
        Assert.That(receivedPhrases[2], Is.EqualTo("attack"));
    }

    #endregion

    #region 29. SpeechParsed

    [Test]
    public void SpeechParsed_DispatchesParsedAlternates()
    {
        List<SpeechRecoItem> receivedItems = null;

        _recognizer.OnSpeechParsed += (parsedAlternates) =>
        {
            receivedItems = parsedAlternates;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""SpeechParsed"",
            ""params"": {
                ""parsedAlternates"": [
                    { ""text"": ""friendly infantry platoon"", ""confidence"": 0.95 },
                    { ""text"": ""friendly infantry company"", ""confidence"": 0.72 }
                ]
            }
        }");

        Assert.That(receivedItems, Is.Not.Null);
        Assert.That(receivedItems, Has.Count.EqualTo(2));
        Assert.That(receivedItems[0].Text, Is.EqualTo("friendly infantry platoon"));
        Assert.That(receivedItems[0].Confidence, Is.EqualTo(0.95).Within(0.001));
        Assert.That(receivedItems[1].Text, Is.EqualTo("friendly infantry company"));
        Assert.That(receivedItems[1].Confidence, Is.EqualTo(0.72).Within(0.001));
    }

    #endregion

    #region 30. AudioCapture

    [Test]
    public void AudioCapture_DispatchesIsListeningTrue()
    {
        bool? receivedIsListening = null;

        _recognizer.OnListeningStateChanged += (isListening) =>
        {
            receivedIsListening = isListening;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""AudioCapture"",
            ""params"": { ""isListening"": true }
        }");

        Assert.That(receivedIsListening, Is.Not.Null);
        Assert.That(receivedIsListening.Value, Is.True);
    }

    [Test]
    public void AudioCapture_DispatchesIsListeningFalse()
    {
        bool? receivedIsListening = null;

        _recognizer.OnListeningStateChanged += (isListening) =>
        {
            receivedIsListening = isListening;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""AudioCapture"",
            ""params"": { ""isListening"": false }
        }");

        Assert.That(receivedIsListening, Is.Not.Null);
        Assert.That(receivedIsListening.Value, Is.False);
    }

    #endregion

    #region 31. Listen

    [Test]
    public void Listen_DispatchesAuthModeAndTime()
    {
        Auth receivedAuth = null;
        ListenMode? receivedMode = null;
        DateTime? receivedTime = null;

        _recognizer.OnListen += (auth, mode, time) =>
        {
            receivedAuth = auth;
            receivedMode = mode;
            receivedTime = time;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""Listen"",
            ""params"": {
                ""auth"": { ""fsTYPE"": ""auth"", ""identity"": ""user-123"" },
                ""mode"": ""on"",
                ""time"": ""2024-01-15T10:30:00Z""
            }
        }");

        Assert.That(receivedAuth, Is.Not.Null);
        Assert.That(receivedAuth.Identity, Is.EqualTo("user-123"));
        Assert.That(receivedMode, Is.EqualTo(ListenMode.on));
        Assert.That(receivedTime, Is.Not.Null);
        Assert.That(receivedTime.Value.Year, Is.EqualTo(2024));
        Assert.That(receivedTime.Value.Month, Is.EqualTo(1));
        Assert.That(receivedTime.Value.Day, Is.EqualTo(15));
    }

    [Test]
    public void Listen_DispatchesOffMode()
    {
        ListenMode? receivedMode = null;

        _recognizer.OnListen += (auth, mode, time) =>
        {
            receivedMode = mode;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""Listen"",
            ""params"": {
                ""auth"": { ""fsTYPE"": ""auth"", ""identity"": ""user-456"" },
                ""mode"": ""off"",
                ""time"": ""2024-06-01T08:00:00Z""
            }
        }");

        Assert.That(receivedMode, Is.EqualTo(ListenMode.off));
    }

    #endregion

    #region 32. SketchRecognized

    [Test]
    public void SketchRecognized_DispatchesSketchList()
    {
        List<SketchRecoResult> receivedList = null;

        _recognizer.OnSketchRecognized += (sketchList) =>
        {
            receivedList = sketchList;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""SketchRecognized"",
            ""params"": {
                ""sketchList"": [
                    { ""type"": ""Line"", ""confidence"": 0.9 },
                    { ""type"": ""Area"", ""confidence"": 0.7 }
                ]
            }
        }");

        Assert.That(receivedList, Is.Not.Null);
        Assert.That(receivedList, Has.Count.EqualTo(2));
        Assert.That(receivedList[0].Type, Is.EqualTo(SketchClass.Line));
        Assert.That(receivedList[0].Confidence, Is.EqualTo(0.9).Within(0.001));
        Assert.That(receivedList[1].Type, Is.EqualTo(SketchClass.Area));
        Assert.That(receivedList[1].Confidence, Is.EqualTo(0.7).Within(0.001));
    }

    #endregion

    #region 33. SketchIntegrated

    [Test]
    public void SketchIntegrated_Dispatches()
    {
        bool fired = false;

        _recognizer.OnSketchIntegrated += () =>
        {
            fired = true;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""SketchIntegrated"",
            ""params"": {}
        }");

        Assert.That(fired, Is.True);
    }

    #endregion

    #region 34. SketchDiscarded

    [Test]
    public void SketchDiscarded_Dispatches()
    {
        bool fired = false;

        _recognizer.OnSketchDiscarded += () =>
        {
            fired = true;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""SketchDiscarded"",
            ""params"": {}
        }");

        Assert.That(fired, Is.True);
    }

    #endregion

    #region 35. SpeechIntegrated

    [Test]
    public void SpeechIntegrated_Dispatches()
    {
        bool fired = false;

        _recognizer.OnSpeechIntegrated += () =>
        {
            fired = true;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""SpeechIntegrated"",
            ""params"": {}
        }");

        Assert.That(fired, Is.True);
    }

    #endregion

    #region 36. SpeechDiscarded

    [Test]
    public void SpeechDiscarded_Dispatches()
    {
        bool fired = false;

        _recognizer.OnSpeechDiscarded += () =>
        {
            fired = true;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""SpeechDiscarded"",
            ""params"": {}
        }");

        Assert.That(fired, Is.True);
    }

    #endregion

    #region 37. PenDown

    [Test]
    public void PenDown_DispatchesTimeAndCoord()
    {
        DateTime? receivedTime = null;
        LatLon receivedCoord = null;

        _recognizer.OnPenDown += (time, coord) =>
        {
            receivedTime = time;
            receivedCoord = coord;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""PenDown"",
            ""params"": {
                ""time"": ""2024-03-20T14:30:00Z"",
                ""coord"": { ""lat"": 59.14, ""lon"": 10.07 }
            }
        }");

        Assert.That(receivedTime, Is.Not.Null);
        Assert.That(receivedTime.Value.Year, Is.EqualTo(2024));
        Assert.That(receivedTime.Value.Month, Is.EqualTo(3));
        Assert.That(receivedTime.Value.Day, Is.EqualTo(20));
        Assert.That(receivedCoord, Is.Not.Null);
        Assert.That(receivedCoord.Lat, Is.EqualTo(59.14).Within(0.001));
        Assert.That(receivedCoord.Lon, Is.EqualTo(10.07).Within(0.001));
    }

    #endregion

    #region 38. PenUp

    [Test]
    public void PenUp_DispatchesTimeAndCoord()
    {
        DateTime? receivedTime = null;
        LatLon receivedCoord = null;

        _recognizer.OnPenUp += (time, coord) =>
        {
            receivedTime = time;
            receivedCoord = coord;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""PenUp"",
            ""params"": {
                ""time"": ""2024-03-20T14:31:05Z"",
                ""coord"": { ""lat"": 59.20, ""lon"": 10.15 }
            }
        }");

        Assert.That(receivedTime, Is.Not.Null);
        Assert.That(receivedTime.Value.Year, Is.EqualTo(2024));
        Assert.That(receivedCoord, Is.Not.Null);
        Assert.That(receivedCoord.Lat, Is.EqualTo(59.20).Within(0.001));
        Assert.That(receivedCoord.Lon, Is.EqualTo(10.15).Within(0.001));
    }

    #endregion

    #region 39. InkProcessed

    [Test]
    public void InkProcessed_Dispatches()
    {
        bool fired = false;

        _recognizer.OnInkProcessed += () =>
        {
            fired = true;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""InkProcessed"",
            ""params"": {}
        }");

        Assert.That(fired, Is.True);
    }

    #endregion

    #region 40. NewScenario

    [Test]
    public void NewScenario_Dispatches()
    {
        bool fired = false;

        _recognizer.OnNewScenario += () =>
        {
            fired = true;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""NewScenario"",
            ""params"": {}
        }");

        Assert.That(fired, Is.True);
    }

    #endregion

    #region 41. Shutdown

    [Test]
    public void Shutdown_Dispatches()
    {
        bool fired = false;

        _recognizer.OnShutdown += () =>
        {
            fired = true;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""Shutdown"",
            ""params"": {}
        }");

        Assert.That(fired, Is.True);
    }

    #endregion
}
