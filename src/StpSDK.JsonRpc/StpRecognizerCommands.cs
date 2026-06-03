using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StpSDK;

public partial class StpRecognizer
{
    #region Pen and Ink

    public void SendPenDown(LatLon location, DateTime timestamp)
    {
        Send("SendPenDown", new
        {
            location = location,
            timestamp = timestamp
        });
    }

    public void SendInk(
        System.Drawing.Size pixelBoundsWindow,
        LatLon topLeftGeoMap,
        LatLon bottomRightGeoMap,
        List<LatLon> strokePoints,
        DateTime timeStrokeStart,
        DateTime timeStrokeEnd,
        List<string> intersectedPoids)
    {
        Send("SendInk", new
        {
            // Preserve the { Width, Height } wire shape independent of the parameter type.
            pixelBoundsWindow = new { pixelBoundsWindow.Width, pixelBoundsWindow.Height },
            topLeftGeoMap = topLeftGeoMap,
            bottomRightGeoMap = bottomRightGeoMap,
            strokePoints = strokePoints,
            timeStrokeStart = timeStrokeStart,
            timeStrokeEnd = timeStrokeEnd,
            intersectedPoids = intersectedPoids
        });
    }

    #endregion

    #region Speech

    public void SendSimulatedSpeechRecognition(string typedInput, DateTime? startTime = null)
    {
        var recoList = ConvertToTranscription(typedInput);
        SendSpeechRecognition(recoList, startTime);
    }

    public void SendSpeechRecognition(List<SpeechRecoItem> recoList, DateTime? startTime = null, DateTime? endTime = null)
    {
        Send("SendSpeechRecognition", new
        {
            recoList = recoList,
            startTime = startTime ?? DateTime.UtcNow,
            endTime = endTime
        });
    }

    public void SendSpeechRecognitionExt(Auth auth, List<SpeechRecoItem> recoList, DateTime? startTime = null, DateTime? endTime = null)
    {
        Send("SendSpeechRecognition", new
        {
            auth = auth,
            recoList = recoList,
            startTime = startTime ?? DateTime.UtcNow,
            endTime = endTime
        });
    }

    public void SetSpeechListening(bool listen, Auth auth = null)
    {
        Send("SetSpeechListening", new
        {
            listen = listen,
            auth = auth
        });
    }

    public void RecognizeNow(Auth auth = null)
    {
        Send("RecognizeNow", new
        {
            auth = auth
        });
    }

    #endregion

    #region Timeout and Segmentation

    public void ChangeTimeOut(double timeout)
    {
        Send("ChangeTimeOut", new
        {
            timeout = timeout
        });
    }

    public void ResetSegmentationTimeout(Auth auth = null)
    {
        Send("ResetSegmentationTimeout", new
        {
            auth = auth
        });
    }

    #endregion

    #region Listen and Audio

    public void SendListen(ListenMode mode, DateTime? time = null)
    {
        Send("SendListen", new
        {
            mode = mode,
            time = time ?? DateTime.UtcNow
        });
    }

    public void SendAudioCaptureState(bool isListening, Auth auth = null)
    {
        Send("SendAudioCaptureState", new
        {
            isListening = isListening,
            auth = auth
        });
    }

    #endregion

    #region Viewport

    public void AdvertiseViewport(LatLon topLeft, LatLon botRight)
    {
        Send("AdvertiseViewport", new
        {
            topLeft = topLeft,
            botRight = botRight
        });
    }

    #endregion

    #region Tasking

    public void SetAutoTasking(bool isEnabled, Auth auth = null)
    {
        Send("SetAutoTasking", new
        {
            isEnabled = isEnabled,
            auth = auth
        });
    }

    #endregion

    #region Symbol CRUD

    public void AddSymbol(StpItem stpSymbol)
    {
        Send("AddSymbol", new
        {
            symbol = stpSymbol
        });
    }

    public void UpdateSymbol(string poid, StpItem stpSymbol)
    {
        Send("UpdateSymbol", new
        {
            poid = poid,
            symbol = stpSymbol
        });
    }

    public void ChooseAlternate(string poid, int nbestIndex)
    {
        Send("ChooseAlternate", new
        {
            poid = poid,
            nbestIndex = nbestIndex
        });
    }

    public void DeleteSymbol(string poid)
    {
        Send("DeleteSymbol", new
        {
            poid = poid
        });
    }

    #endregion

    #region Task CRUD

    public void AddTask(StpTask stpTask)
    {
        Send("AddTask", new
        {
            task = stpTask
        });
    }

    public void UpdateTask(string poid, StpTask stpTask)
    {
        Send("UpdateTask", new
        {
            poid = poid,
            alternates = new[] { stpTask }
        });
    }

    public void UpdateTask(string poid, List<StpTask> stpTaskAlternates)
    {
        Send("UpdateTask", new
        {
            poid = poid,
            alternates = stpTaskAlternates
        });
    }

    public void DeleteTask(string poid)
    {
        Send("DeleteTask", new
        {
            poid = poid
        });
    }

    #endregion

    #region TaskOrg CRUD

    public void AddTaskOrg(StpTaskOrg stpTo)
    {
        Send("AddTaskOrg", new
        {
            taskOrg = stpTo
        });
    }

    public void UpdateTaskOrg(string poid, StpTaskOrg stpTo)
    {
        Send("UpdateTaskOrg", new
        {
            poid = poid,
            taskOrg = stpTo
        });
    }

    public void DeleteTaskOrg(string poid)
    {
        Send("DeleteTaskOrg", new
        {
            poid = poid
        });
    }

    #endregion

    #region TaskOrgUnit CRUD

    public void AddTaskOrgUnit(StpTaskOrgUnit stpToUnit)
    {
        Send("AddTaskOrgUnit", new
        {
            toUnit = stpToUnit
        });
    }

    public void UpdateTaskOrgUnit(string poid, StpTaskOrgUnit stpToUnit)
    {
        Send("UpdateTaskOrgUnit", new
        {
            poid = poid,
            toUnit = stpToUnit
        });
    }

    public void DeleteTaskOrgUnit(string poid)
    {
        Send("DeleteTaskOrgUnit", new
        {
            poid = poid
        });
    }

    #endregion

    #region TaskOrgRelationship CRUD

    public void AddTaskOrgRelationship(StpTaskOrgRelationship stpToRelationship)
    {
        Send("AddTaskOrgRelationship", new
        {
            toRelationship = stpToRelationship
        });
    }

    public void UpdateTaskOrgRelationship(string poid, StpTaskOrgRelationship stpToRelationship)
    {
        Send("UpdateTaskOrgRelationship", new
        {
            poid = poid,
            toRelationship = stpToRelationship
        });
    }

    public void DeleteTaskOrgRelationship(string poid)
    {
        Send("DeleteTaskOrgRelationship", new
        {
            poid = poid
        });
    }

    #endregion

    #region COA Commands

    public void AddCoa(StpCoa stpCoa)
    {
        Send("AddCoa", new
        {
            coa = stpCoa
        });
    }

    public void UpdateCoa(string poid, StpCoa stpCoa)
    {
        Send("UpdateCoa", new
        {
            poid = poid,
            coa = stpCoa
        });
    }

    public void DeleteCoa(string poid)
    {
        Send("DeleteCoa", new
        {
            poid = poid
        });
    }

    #endregion

    #region Role and COA switching

    public void SwitchRoleAndCoa(string newRole, string newCoaPoid)
    {
        Send("SwitchRoleAndCoa", new
        {
            role = newRole,
            coaPoid = newCoaPoid
        });
    }

    public void ResetRole()
    {
        Send("ResetRole", new { });
    }

    #endregion

    #region Undo

    public void UndoLastOp(string poid)
    {
        Send("UndoLastOp", new
        {
            poid = poid
        });
    }

    #endregion

    #region Send helper

    private void Send(string method, object parameters)
    {
        var message = JsonConvert.SerializeObject(new
        {
            method = method,
            @params = parameters
        }, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

        _connector.Send(message);
    }

    #endregion
}
