using Newtonsoft.Json.Linq;
using NUnit.Framework;
using StpSDK;
using System;
using System.Collections.Generic;

namespace StpSDK.Tests;

[TestFixture]
public class RecognizerCommandTests
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

    private JObject GetSentMessage(int index = 0)
    {
        Assert.That(_connector.SentMessages, Has.Count.GreaterThan(index));
        return JObject.Parse(_connector.SentMessages[index]);
    }

    #region 1. SendPenDown

    [Test]
    public void SendPenDown_SendsCorrectMethodAndParams()
    {
        var location = new LatLon(59.14, 10.07);
        var timestamp = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        _recognizer.SendPenDown(location, timestamp);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("SendPenDown"));
        Assert.That((double)msg["params"]["location"]["lat"], Is.EqualTo(59.14));
        Assert.That((double)msg["params"]["location"]["lon"], Is.EqualTo(10.07));
        Assert.That(msg["params"]["timestamp"], Is.Not.Null);
    }

    #endregion

    #region 2. SendInk

    [Test]
    public void SendInk_SendsCorrectMethodAndAllParams()
    {
        var pixelBounds = new System.Drawing.Size(800, 600);
        var topLeft = new LatLon(59.27, 9.70);
        var botRight = new LatLon(59.01, 10.44);
        var strokePoints = new List<LatLon>
        {
            new LatLon(59.14, 10.07),
            new LatLon(59.15, 10.08)
        };
        var timeStart = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var timeEnd = new DateTime(2025, 6, 15, 12, 0, 1, DateTimeKind.Utc);
        var intersected = new List<string> { "poid-1", "poid-2" };

        _recognizer.SendInk(pixelBounds, topLeft, botRight, strokePoints, timeStart, timeEnd, intersected);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("SendInk"));
        Assert.That((double)msg["params"]["pixelBoundsWindow"]["Width"], Is.EqualTo(800));
        Assert.That((double)msg["params"]["pixelBoundsWindow"]["Height"], Is.EqualTo(600));
        Assert.That((double)msg["params"]["topLeftGeoMap"]["lat"], Is.EqualTo(59.27));
        Assert.That((double)msg["params"]["topLeftGeoMap"]["lon"], Is.EqualTo(9.70));
        Assert.That((double)msg["params"]["bottomRightGeoMap"]["lat"], Is.EqualTo(59.01));
        Assert.That((double)msg["params"]["bottomRightGeoMap"]["lon"], Is.EqualTo(10.44));
        Assert.That(((JArray)msg["params"]["strokePoints"]).Count, Is.EqualTo(2));
        Assert.That(msg["params"]["timeStrokeStart"], Is.Not.Null);
        Assert.That(msg["params"]["timeStrokeEnd"], Is.Not.Null);
        Assert.That(((JArray)msg["params"]["intersectedPoids"]).Count, Is.EqualTo(2));
        Assert.That((string)msg["params"]["intersectedPoids"][0], Is.EqualTo("poid-1"));
    }

    #endregion

    #region 3. SendSimulatedSpeechRecognition

    [Test]
    public void SendSimulatedSpeechRecognition_SendsSpeechRecognitionMethod()
    {
        var startTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        _recognizer.SendSimulatedSpeechRecognition("attack position", startTime);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("SendSpeechRecognition"));
        Assert.That(((JArray)msg["params"]["recoList"]).Count, Is.EqualTo(1));
        Assert.That((string)msg["params"]["recoList"][0]["text"], Is.EqualTo("attack position"));
        Assert.That((double)msg["params"]["recoList"][0]["confidence"], Is.EqualTo(1.0));
        Assert.That(msg["params"]["startTime"], Is.Not.Null);
    }

    [Test]
    public void SendSimulatedSpeechRecognition_WithNullStartTime_StillSends()
    {
        _recognizer.SendSimulatedSpeechRecognition("defend");

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("SendSpeechRecognition"));
        Assert.That(msg["params"]["startTime"], Is.Not.Null);
    }

    #endregion

    #region 4. SendSpeechRecognition

    [Test]
    public void SendSpeechRecognition_SendsCorrectMethodAndParams()
    {
        var recoList = new List<SpeechRecoItem>
        {
            new SpeechRecoItem("attack", 0.95),
            new SpeechRecoItem("advance", 0.80)
        };
        var startTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var endTime = new DateTime(2025, 6, 15, 12, 0, 2, DateTimeKind.Utc);

        _recognizer.SendSpeechRecognition(recoList, startTime, endTime);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("SendSpeechRecognition"));
        Assert.That(((JArray)msg["params"]["recoList"]).Count, Is.EqualTo(2));
        Assert.That((string)msg["params"]["recoList"][0]["text"], Is.EqualTo("attack"));
        Assert.That((double)msg["params"]["recoList"][0]["confidence"], Is.EqualTo(0.95));
        Assert.That(msg["params"]["startTime"], Is.Not.Null);
        Assert.That(msg["params"]["endTime"], Is.Not.Null);
    }

    [Test]
    public void SendSpeechRecognition_NullEndTime_OmitsEndTime()
    {
        var recoList = new List<SpeechRecoItem> { new SpeechRecoItem("attack", 0.9) };
        var startTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        _recognizer.SendSpeechRecognition(recoList, startTime, null);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("SendSpeechRecognition"));
        // endTime is null so it should be omitted due to NullValueHandling.Ignore
        Assert.That(msg["params"]["endTime"], Is.Null);
    }

    #endregion

    #region 5. SendSpeechRecognitionExt

    [Test]
    public void SendSpeechRecognitionExt_SendsMethodWithAuth()
    {
        var auth = new Auth("user-1");
        var recoList = new List<SpeechRecoItem> { new SpeechRecoItem("hold", 0.85) };
        var startTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var endTime = new DateTime(2025, 6, 15, 12, 0, 3, DateTimeKind.Utc);

        _recognizer.SendSpeechRecognitionExt(auth, recoList, startTime, endTime);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("SendSpeechRecognition"));
        Assert.That((string)msg["params"]["auth"]["identity"], Is.EqualTo("user-1"));
        Assert.That(((JArray)msg["params"]["recoList"]).Count, Is.EqualTo(1));
        Assert.That(msg["params"]["startTime"], Is.Not.Null);
        Assert.That(msg["params"]["endTime"], Is.Not.Null);
    }

    #endregion

    #region 6. SetSpeechListening

    [Test]
    public void SetSpeechListening_SendsCorrectMethodAndParams()
    {
        var auth = new Auth("user-1");

        _recognizer.SetSpeechListening(true, auth);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("SetSpeechListening"));
        Assert.That((bool)msg["params"]["listen"], Is.True);
        Assert.That((string)msg["params"]["auth"]["identity"], Is.EqualTo("user-1"));
    }

    [Test]
    public void SetSpeechListening_NullAuth_OmitsAuth()
    {
        _recognizer.SetSpeechListening(false);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("SetSpeechListening"));
        Assert.That((bool)msg["params"]["listen"], Is.False);
        Assert.That(msg["params"]["auth"], Is.Null);
    }

    #endregion

    #region 7. RecognizeNow

    [Test]
    public void RecognizeNow_SendsCorrectMethodWithAuth()
    {
        var auth = new Auth("user-1");

        _recognizer.RecognizeNow(auth);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("RecognizeNow"));
        Assert.That((string)msg["params"]["auth"]["identity"], Is.EqualTo("user-1"));
    }

    [Test]
    public void RecognizeNow_NullAuth_OmitsAuth()
    {
        _recognizer.RecognizeNow();

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("RecognizeNow"));
        Assert.That(msg["params"]["auth"], Is.Null);
    }

    #endregion

    #region 8. ChangeTimeOut

    [Test]
    public void ChangeTimeOut_SendsCorrectMethodAndTimeout()
    {
        _recognizer.ChangeTimeOut(5.5);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("ChangeTimeOut"));
        Assert.That((double)msg["params"]["timeout"], Is.EqualTo(5.5));
    }

    #endregion

    #region 9. ResetSegmentationTimeout

    [Test]
    public void ResetSegmentationTimeout_SendsCorrectMethodWithAuth()
    {
        var auth = new Auth("user-1");

        _recognizer.ResetSegmentationTimeout(auth);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("ResetSegmentationTimeout"));
        Assert.That((string)msg["params"]["auth"]["identity"], Is.EqualTo("user-1"));
    }

    [Test]
    public void ResetSegmentationTimeout_NullAuth_OmitsAuth()
    {
        _recognizer.ResetSegmentationTimeout();

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("ResetSegmentationTimeout"));
        Assert.That(msg["params"]["auth"], Is.Null);
    }

    #endregion

    #region 10. SendListen

    [Test]
    public void SendListen_SendsCorrectMethodAndParams()
    {
        var time = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        _recognizer.SendListen(ListenMode.on, time);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("SendListen"));
        Assert.That(msg["params"]["mode"], Is.Not.Null);
        Assert.That(msg["params"]["time"], Is.Not.Null);
    }

    [Test]
    public void SendListen_NullTime_UsesCurrentTime()
    {
        _recognizer.SendListen(ListenMode.off);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("SendListen"));
        Assert.That(msg["params"]["time"], Is.Not.Null);
    }

    #endregion

    #region 11. SendAudioCaptureState

    [Test]
    public void SendAudioCaptureState_SendsCorrectMethodAndParams()
    {
        var auth = new Auth("user-1");

        _recognizer.SendAudioCaptureState(true, auth);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("SendAudioCaptureState"));
        Assert.That((bool)msg["params"]["isListening"], Is.True);
        Assert.That((string)msg["params"]["auth"]["identity"], Is.EqualTo("user-1"));
    }

    [Test]
    public void SendAudioCaptureState_NullAuth_OmitsAuth()
    {
        _recognizer.SendAudioCaptureState(false);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("SendAudioCaptureState"));
        Assert.That((bool)msg["params"]["isListening"], Is.False);
        Assert.That(msg["params"]["auth"], Is.Null);
    }

    #endregion

    #region 12. AdvertiseViewport

    [Test]
    public void AdvertiseViewport_SendsCorrectMethodAndCoordinates()
    {
        var topLeft = new LatLon(59.27, 9.70);
        var botRight = new LatLon(59.01, 10.44);

        _recognizer.AdvertiseViewport(topLeft, botRight);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("AdvertiseViewport"));
        Assert.That((double)msg["params"]["topLeft"]["lat"], Is.EqualTo(59.27));
        Assert.That((double)msg["params"]["topLeft"]["lon"], Is.EqualTo(9.70));
        Assert.That((double)msg["params"]["botRight"]["lat"], Is.EqualTo(59.01));
        Assert.That((double)msg["params"]["botRight"]["lon"], Is.EqualTo(10.44));
    }

    #endregion

    #region 13. SetAutoTasking

    [Test]
    public void SetAutoTasking_SendsCorrectMethodAndParams()
    {
        var auth = new Auth("user-1");

        _recognizer.SetAutoTasking(true, auth);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("SetAutoTasking"));
        Assert.That((bool)msg["params"]["isEnabled"], Is.True);
        Assert.That((string)msg["params"]["auth"]["identity"], Is.EqualTo("user-1"));
    }

    [Test]
    public void SetAutoTasking_NullAuth_OmitsAuth()
    {
        _recognizer.SetAutoTasking(false);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("SetAutoTasking"));
        Assert.That((bool)msg["params"]["isEnabled"], Is.False);
        Assert.That(msg["params"]["auth"], Is.Null);
    }

    #endregion

    #region 14. AddSymbol

    [Test]
    public void AddSymbol_SendsCorrectMethodAndSymbol()
    {
        var symbol = new StpSymbol { Type = "unit", Poid = "sym-1", SymbolId = "SFGPUCI----D---" };

        _recognizer.AddSymbol(symbol);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("AddSymbol"));
        Assert.That((string)msg["params"]["symbol"]["poid"], Is.EqualTo("sym-1"));
        // sidc is serialized as a structured object; SymbolId maps to the legacy (2525C) code.
        Assert.That((string)msg["params"]["symbol"]["sidc"]["legacy"], Is.EqualTo("SFGPUCI----D---"));
    }

    #endregion

    #region 15. UpdateSymbol

    [Test]
    public void UpdateSymbol_SendsCorrectMethodPoidAndSymbol()
    {
        var symbol = new StpSymbol { Type = "unit", Poid = "sym-1", SymbolId = "SFGPUCA----D---" };

        _recognizer.UpdateSymbol("sym-1", symbol);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("UpdateSymbol"));
        Assert.That((string)msg["params"]["poid"], Is.EqualTo("sym-1"));
        Assert.That((string)msg["params"]["symbol"]["sidc"]["legacy"], Is.EqualTo("SFGPUCA----D---"));
    }

    #endregion

    #region 16. ChooseAlternate

    [Test]
    public void ChooseAlternate_SendsCorrectMethodPoidAndIndex()
    {
        _recognizer.ChooseAlternate("sym-1", 2);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("ChooseAlternate"));
        Assert.That((string)msg["params"]["poid"], Is.EqualTo("sym-1"));
        Assert.That((int)msg["params"]["nbestIndex"], Is.EqualTo(2));
    }

    #endregion

    #region 17. DeleteSymbol

    [Test]
    public void DeleteSymbol_SendsCorrectMethodAndPoid()
    {
        _recognizer.DeleteSymbol("sym-to-delete");

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("DeleteSymbol"));
        Assert.That((string)msg["params"]["poid"], Is.EqualTo("sym-to-delete"));
    }

    #endregion

    #region 18. AddTask

    [Test]
    public void AddTask_SendsCorrectMethodAndTask()
    {
        var task = new StpTask { Poid = "task-1", Name = "Attack" };

        _recognizer.AddTask(task);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("AddTask"));
        Assert.That((string)msg["params"]["task"]["poid"], Is.EqualTo("task-1"));
        Assert.That((string)msg["params"]["task"]["name"], Is.EqualTo("Attack"));
    }

    #endregion

    #region 19. UpdateTask (single)

    [Test]
    public void UpdateTask_Single_SendsCorrectMethodPoidAndAlternates()
    {
        var task = new StpTask { Poid = "task-1", Name = "Defend" };

        _recognizer.UpdateTask("task-1", task);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("UpdateTask"));
        Assert.That((string)msg["params"]["poid"], Is.EqualTo("task-1"));
        Assert.That(((JArray)msg["params"]["alternates"]).Count, Is.EqualTo(1));
        Assert.That((string)msg["params"]["alternates"][0]["name"], Is.EqualTo("Defend"));
    }

    #endregion

    #region 20. UpdateTask (list)

    [Test]
    public void UpdateTask_List_SendsCorrectMethodPoidAndAlternates()
    {
        var alts = new List<StpTask>
        {
            new StpTask { Poid = "task-1", Name = "Attack" },
            new StpTask { Poid = "task-1", Name = "Defend" }
        };

        _recognizer.UpdateTask("task-1", alts);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("UpdateTask"));
        Assert.That((string)msg["params"]["poid"], Is.EqualTo("task-1"));
        Assert.That(((JArray)msg["params"]["alternates"]).Count, Is.EqualTo(2));
        Assert.That((string)msg["params"]["alternates"][0]["name"], Is.EqualTo("Attack"));
        Assert.That((string)msg["params"]["alternates"][1]["name"], Is.EqualTo("Defend"));
    }

    #endregion

    #region 21. DeleteTask

    [Test]
    public void DeleteTask_SendsCorrectMethodAndPoid()
    {
        _recognizer.DeleteTask("task-to-delete");

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("DeleteTask"));
        Assert.That((string)msg["params"]["poid"], Is.EqualTo("task-to-delete"));
    }

    #endregion

    #region 22. AddTaskOrg

    [Test]
    public void AddTaskOrg_SendsCorrectMethodAndTaskOrg()
    {
        var to = new StpTaskOrg { Poid = "to-1", Name = "TaskOrg1" };

        _recognizer.AddTaskOrg(to);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("AddTaskOrg"));
        Assert.That((string)msg["params"]["taskOrg"]["poid"], Is.EqualTo("to-1"));
        Assert.That((string)msg["params"]["taskOrg"]["name"], Is.EqualTo("TaskOrg1"));
    }

    #endregion

    #region 23. UpdateTaskOrg

    [Test]
    public void UpdateTaskOrg_SendsCorrectMethodPoidAndTaskOrg()
    {
        var to = new StpTaskOrg { Poid = "to-1", Name = "TaskOrg1Updated" };

        _recognizer.UpdateTaskOrg("to-1", to);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("UpdateTaskOrg"));
        Assert.That((string)msg["params"]["poid"], Is.EqualTo("to-1"));
        Assert.That((string)msg["params"]["taskOrg"]["name"], Is.EqualTo("TaskOrg1Updated"));
    }

    #endregion

    #region 24. DeleteTaskOrg

    [Test]
    public void DeleteTaskOrg_SendsCorrectMethodAndPoid()
    {
        _recognizer.DeleteTaskOrg("to-to-delete");

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("DeleteTaskOrg"));
        Assert.That((string)msg["params"]["poid"], Is.EqualTo("to-to-delete"));
    }

    #endregion

    #region 25. AddTaskOrgUnit

    [Test]
    public void AddTaskOrgUnit_SendsCorrectMethodAndUnit()
    {
        var toUnit = new StpTaskOrgUnit { Poid = "tou-1", Name = "Unit1" };

        _recognizer.AddTaskOrgUnit(toUnit);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("AddTaskOrgUnit"));
        Assert.That((string)msg["params"]["toUnit"]["poid"], Is.EqualTo("tou-1"));
        Assert.That((string)msg["params"]["toUnit"]["name"], Is.EqualTo("Unit1"));
    }

    #endregion

    #region 26. UpdateTaskOrgUnit

    [Test]
    public void UpdateTaskOrgUnit_SendsCorrectMethodPoidAndUnit()
    {
        var toUnit = new StpTaskOrgUnit { Poid = "tou-1", Name = "Unit1Updated" };

        _recognizer.UpdateTaskOrgUnit("tou-1", toUnit);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("UpdateTaskOrgUnit"));
        Assert.That((string)msg["params"]["poid"], Is.EqualTo("tou-1"));
        Assert.That((string)msg["params"]["toUnit"]["name"], Is.EqualTo("Unit1Updated"));
    }

    #endregion

    #region 27. DeleteTaskOrgUnit

    [Test]
    public void DeleteTaskOrgUnit_SendsCorrectMethodAndPoid()
    {
        _recognizer.DeleteTaskOrgUnit("tou-to-delete");

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("DeleteTaskOrgUnit"));
        Assert.That((string)msg["params"]["poid"], Is.EqualTo("tou-to-delete"));
    }

    #endregion

    #region 28. AddTaskOrgRelationship

    [Test]
    public void AddTaskOrgRelationship_SendsCorrectMethodAndRelationship()
    {
        var rel = new StpTaskOrgRelationship { Poid = "tor-1", Parent = "parent-1", Child = "child-1" };

        _recognizer.AddTaskOrgRelationship(rel);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("AddTaskOrgRelationship"));
        Assert.That((string)msg["params"]["toRelationship"]["poid"], Is.EqualTo("tor-1"));
        Assert.That((string)msg["params"]["toRelationship"]["parent"], Is.EqualTo("parent-1"));
        Assert.That((string)msg["params"]["toRelationship"]["child"], Is.EqualTo("child-1"));
    }

    #endregion

    #region 29. UpdateTaskOrgRelationship

    [Test]
    public void UpdateTaskOrgRelationship_SendsCorrectMethodPoidAndRelationship()
    {
        var rel = new StpTaskOrgRelationship { Poid = "tor-1", Parent = "parent-2", Child = "child-2" };

        _recognizer.UpdateTaskOrgRelationship("tor-1", rel);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("UpdateTaskOrgRelationship"));
        Assert.That((string)msg["params"]["poid"], Is.EqualTo("tor-1"));
        Assert.That((string)msg["params"]["toRelationship"]["parent"], Is.EqualTo("parent-2"));
        Assert.That((string)msg["params"]["toRelationship"]["child"], Is.EqualTo("child-2"));
    }

    #endregion

    #region 30. DeleteTaskOrgRelationship

    [Test]
    public void DeleteTaskOrgRelationship_SendsCorrectMethodAndPoid()
    {
        _recognizer.DeleteTaskOrgRelationship("tor-to-delete");

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("DeleteTaskOrgRelationship"));
        Assert.That((string)msg["params"]["poid"], Is.EqualTo("tor-to-delete"));
    }

    #endregion

    #region 31. AddCoa

    [Test]
    public void AddCoa_SendsCorrectMethodAndCoa()
    {
        var coa = new StpCoa { Poid = "coa-1", Name = "COA Alpha" };

        _recognizer.AddCoa(coa);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("AddCoa"));
        Assert.That((string)msg["params"]["coa"]["poid"], Is.EqualTo("coa-1"));
        Assert.That((string)msg["params"]["coa"]["name"], Is.EqualTo("COA Alpha"));
    }

    #endregion

    #region 32. UpdateCoa

    [Test]
    public void UpdateCoa_SendsCorrectMethodPoidAndCoa()
    {
        var coa = new StpCoa { Poid = "coa-1", Name = "COA Beta" };

        _recognizer.UpdateCoa("coa-1", coa);

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("UpdateCoa"));
        Assert.That((string)msg["params"]["poid"], Is.EqualTo("coa-1"));
        Assert.That((string)msg["params"]["coa"]["name"], Is.EqualTo("COA Beta"));
    }

    #endregion

    #region 33. DeleteCoa

    [Test]
    public void DeleteCoa_SendsCorrectMethodAndPoid()
    {
        _recognizer.DeleteCoa("coa-to-delete");

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("DeleteCoa"));
        Assert.That((string)msg["params"]["poid"], Is.EqualTo("coa-to-delete"));
    }

    #endregion

    #region 34. SwitchRoleAndCoa

    [Test]
    public void SwitchRoleAndCoa_SendsCorrectMethodRoleAndCoaPoid()
    {
        _recognizer.SwitchRoleAndCoa("FRIENDLY_CO", "coa-42");

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("SwitchRoleAndCoa"));
        Assert.That((string)msg["params"]["role"], Is.EqualTo("FRIENDLY_CO"));
        Assert.That((string)msg["params"]["coaPoid"], Is.EqualTo("coa-42"));
    }

    #endregion

    #region 35. ResetRole

    [Test]
    public void ResetRole_SendsCorrectMethodWithEmptyParams()
    {
        _recognizer.ResetRole();

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("ResetRole"));
        Assert.That(msg["params"], Is.Not.Null);
    }

    #endregion

    #region 36. UndoLastOp

    [Test]
    public void UndoLastOp_SendsCorrectMethodAndPoid()
    {
        _recognizer.UndoLastOp("undo-poid");

        var msg = GetSentMessage();
        Assert.That((string)msg["method"], Is.EqualTo("UndoLastOp"));
        Assert.That((string)msg["params"]["poid"], Is.EqualTo("undo-poid"));
    }

    #endregion
}
