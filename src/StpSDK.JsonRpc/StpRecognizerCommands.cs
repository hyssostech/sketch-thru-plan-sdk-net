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

    /// <summary>
    /// Sends text to STP as if it came from speech recognition. The engine converts numbers and
    /// letters server-side into the words a speech recognizer would produce (e.g. "A 3 1" becomes
    /// "alpha three one"), matching the JS SDK's sendSimulatedSpeechRecognition. Fire-and-forget.
    /// Engine dispatch: StpJsonClient.cs:272 (StpJsonClient.SendSimulatedSpeechRecognition).
    /// Prior to this method, the SDK sent a client-side stand-in (a single verbatim recoList item
    /// over the wire method "SendSpeechRecognition") that did not perform this conversion; this
    /// implementation now uses the engine's dedicated wire method instead.
    /// </summary>
    /// <param name="typedInput">Text to be converted and sent as speech</param>
    /// <param name="startTime">Optional time the speech occurred. Defaults server-side to the current time if not provided.</param>
    public void SendSimulatedSpeechRecognition(string typedInput, DateTime? startTime = null)
    {
        Send("SendSimulatedSpeechRecognition", new
        {
            text = typedInput,
            startTime = startTime
        });
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

    /// <summary>
    /// Picks an alternate as the confirmed task, matching the JS SDK's confirmTask. Fire-and-forget:
    /// the STP runtime responds asynchronously with a task update notification in which uiStatus is
    /// set to 'confirmed'.
    /// Engine dispatch: StpJsonClient.cs:289 - the arm currently passes true to
    /// SwitchTaskConfirmationAsync's confirmation flag regardless of what isConfirmed carries, so
    /// isConfirmed: false is accepted on the wire but does not un-confirm today.
    /// </summary>
    /// <param name="poid">Unique identifier of the task for which an alternate is being confirmed</param>
    /// <param name="nBestIndex">Index indicating which of the current alternates is selected for confirmation</param>
    /// <param name="isConfirmed">True (default) to switch the task to confirmed status, false to switch back to confirming. See remarks - the engine ignores this value today.</param>
    public void ConfirmTask(string poid, int nBestIndex, bool isConfirmed = true)
    {
        Send("ConfirmTask", new
        {
            poid = poid,
            nBestIndex = nBestIndex,
            isConfirmed = isConfirmed
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
