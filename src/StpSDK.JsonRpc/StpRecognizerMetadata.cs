using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StpSDK;

public partial class StpRecognizer
{
    #region Scenario Management

    public async Task<string> CreateNewScenarioAsync(string name, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync("CreateNewScenario", new { name }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> CreateCoaAsync(string name, string affiliation, string role, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync("CreateCoa", new { name, affiliation, role }, cancellationToken).ConfigureAwait(false);
    }

    public async Task LoadNewScenarioAsync(string content, CancellationToken cancellationToken = default)
    {
        await SendRequestAsync("LoadNewScenario", new { content }, cancellationToken).ConfigureAwait(false);
    }

    public async Task LoadNewScenarioAsync(ObjectSet os, CancellationToken cancellationToken = default)
    {
        await SendRequestAsync("LoadNewScenarioFromObjectSet", ObjectsParam(os), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// <see cref="LoadNewScenarioAsync(ObjectSet, CancellationToken)"/> with a per-call load timeout
    /// (<c>LoadNewScenarioFromObjectSet</c>). See <see cref="StpObjectSetLoadException"/> for the report a
    /// load that stops or times out answers with.
    /// </summary>
    /// <param name="os">The objects, as GetScenarioObjectSet returned them.</param>
    /// <param name="timeout">How long the engine may spend loading, sent as the request's <c>timeout</c>
    /// (whole seconds, rounded up; the default for the other calls is 30 s). <see cref="TimeSpan.Zero"/> =
    /// no engine timeout. The client waits a few seconds longer, for the engine's report.</param>
    /// <param name="cancellationToken">Stops waiting; the engine may still finish the load.</param>
    public async Task LoadNewScenarioAsync(ObjectSet os, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        await SendLoadRequestAsync("LoadNewScenarioFromObjectSet", ObjectsParam(os), timeout, cancellationToken).ConfigureAwait(false);
    }

    public async Task ResetStpScenarioAsync(CancellationToken cancellationToken = default)
    {
        await SendRequestAsync("ResetStpScenario", new { }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> GetScenarioContentAsync(CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync("GetScenarioContent", new { }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ObjectSet> GetScenarioObjectSetContentAsync(CancellationToken cancellationToken = default)
    {
        string result = await SendRequestAsync("GetScenarioObjectSet", new { }, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(result)) return new ObjectSet();
        return ParseObjectSetResponse(result);
    }

    public async Task JoinScenarioSessionAsync(CancellationToken cancellationToken = default)
    {
        await SendRequestAsync("JoinScenarioSession", new { }, cancellationToken).ConfigureAwait(false);
    }

    public async Task SyncScenarioSessionAsync(string content, CancellationToken cancellationToken = default)
    {
        await SendRequestAsync("SyncScenarioSession", new { content }, cancellationToken).ConfigureAwait(false);
    }

    public async Task SyncScenarioSessionAsync(ObjectSet localObjects, CancellationToken cancellationToken = default)
    {
        await SendRequestAsync("SyncScenarioSessionFromObjectSet", ObjectsParam(localObjects), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// <see cref="SyncScenarioSessionAsync(ObjectSet, CancellationToken)"/> with a per-call load timeout
    /// (<c>SyncScenarioSessionFromObjectSet</c>); see <see cref="StpObjectSetLoadException"/>.
    /// </summary>
    public async Task SyncScenarioSessionAsync(ObjectSet localObjects, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        await SendLoadRequestAsync("SyncScenarioSessionFromObjectSet", ObjectsParam(localObjects), timeout, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Merge text content into the current scenario (<c>ImportPlanData</c>). True once the engine accepts
    /// the import: the contract answers null on success, which the connector delivers as an empty string.
    /// A refusal throws <see cref="StpException"/>. False only for an explicit boolean <c>false</c> answer,
    /// which no contract engine sends (STP-1799; before it, the empty success answer returned false).
    /// </summary>
    public async Task<bool> ImportSTPDataAsync(string content, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("ImportPlanData", new { content }, cancellationToken).ConfigureAwait(false);
        return IsImportAccepted(result);
    }

    /// <summary>
    /// The object-set form of <see cref="ImportSTPDataAsync(string, CancellationToken)"/>
    /// (<c>ImportPlanDataFromObjectSet</c>), with the same result: true unless the engine refuses (which throws).
    /// </summary>
    public async Task<bool> ImportSTPDataAsync(ObjectSet stpObjects, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("ImportPlanDataFromObjectSet", ObjectsParam(stpObjects), cancellationToken).ConfigureAwait(false);
        return IsImportAccepted(result);
    }

    /// <summary>
    /// <see cref="ImportSTPDataAsync(ObjectSet, CancellationToken)"/> with a per-call load timeout
    /// (<c>ImportPlanDataFromObjectSet</c>); see <see cref="StpObjectSetLoadException"/>.
    /// </summary>
    public async Task<bool> ImportSTPDataAsync(ObjectSet stpObjects, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var result = await SendLoadRequestAsync("ImportPlanDataFromObjectSet", ObjectsParam(stpObjects), timeout, cancellationToken).ConfigureAwait(false);
        return IsImportAccepted(result);
    }

    public async Task<bool> HasActiveScenarioAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("HasActiveScenario", new { }, cancellationToken).ConfigureAwait(false);
        return IsTrueAnswer(result);
    }

    public async Task<PlanningScenario> GetActiveScenarioDescriptionAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("GetActiveScenarioDescription", new { }, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(result)) return null;
        return JsonConvert.DeserializeObject<PlanningScenario>(result);
    }

    #endregion

    #region Task Org Import/Export

    public async Task<string> ImportTaskOrgContentAsync(string content, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync("ImportTaskOrgContent", new { content }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> ImportTaskOrgAsync(ObjectSet os, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync("ImportTaskOrgFromObjectSet", ObjectsParam(os), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// <see cref="ImportTaskOrgAsync(ObjectSet, CancellationToken)"/> with a per-call load timeout
    /// (<c>ImportTaskOrgFromObjectSet</c>); see <see cref="StpObjectSetLoadException"/>.
    /// </summary>
    public async Task<string> ImportTaskOrgAsync(ObjectSet os, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        return await SendLoadRequestAsync("ImportTaskOrgFromObjectSet", ObjectsParam(os), timeout, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// The task org with its units and relationships as STP object-set text
    /// (<c>object_set([...])</c>) - the form <see cref="ImportTaskOrgContentAsync"/> takes back.
    /// Null when the engine answers with nothing.
    /// </summary>
    /// <remarks>
    /// Returns the text unchanged (STP-1092). Before 0.6.1 this returned <c>JToken.Parse(result)</c>,
    /// and object-set text is not JSON, so every real answer threw <c>JsonReaderException</c>.
    /// Use <see cref="GetTaskOrgObjectSetAsync"/> for the parsed objects.
    /// </remarks>
    public async Task<string> GetTaskOrgContentAsync(string poid, CancellationToken cancellationToken)
    {
        var result = await SendRequestAsync("GetTaskOrgContent", new { poid }, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrEmpty(result) ? null : result;
    }

    public async Task<ObjectSet> GetTaskOrgObjectSetAsync(string poid, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("GetTaskOrgObjectSet", new { poid }, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(result)) return new ObjectSet();
        return ParseObjectSetResponse(result);
    }

    public async Task SetDefaultTaskOrgAsync(string poid, CancellationToken cancellationToken = default)
    {
        await SendRequestAsync("SetDefaultTaskOrg", new { poid }, cancellationToken).ConfigureAwait(false);
    }

    public async Task ResetDefaultTaskOrgAsync(string affiliation, CancellationToken cancellationToken)
    {
        await SendRequestAsync("ResetDefaultTaskOrg", new { affiliation }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Select a task org. Sends <c>SetCoaTaskOrg</c>. Without <paramref name="coaPoid"/> the TO
    /// becomes the default for its affiliation, exactly as <see cref="SetDefaultTaskOrgAsync"/> does.
    /// With one, the TO is tied to that COA only. Engines before STP-1077 refuse a
    /// <paramref name="coaPoid"/> with "per-COA task org is not supported yet".
    /// </summary>
    public async Task SetCoaTaskOrgAsync(string toPoid, string coaPoid = null, CancellationToken cancellationToken = default)
    {
        await SendRequestAsync("SetCoaTaskOrg", new { toPoid, coaPoid }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Clear a TO selection - the TO itself is not deleted. Sends <c>ResetCoaTaskOrg</c>. Without
    /// <paramref name="coaPoid"/> it unsets the default TO for <paramref name="affiliation"/>
    /// (<c>friend</c> or <c>hostile</c>), exactly as <see cref="ResetDefaultTaskOrgAsync"/> does,
    /// and <paramref name="affiliation"/> is required. With a <paramref name="coaPoid"/> the TO tied
    /// to that COA is cleared - for <paramref name="affiliation"/>, or for every affiliation when it
    /// is null. Engines before STP-1077 refuse a <paramref name="coaPoid"/>.
    /// </summary>
    public async Task ResetCoaTaskOrgAsync(string affiliation, string coaPoid = null, CancellationToken cancellationToken = default)
    {
        await SendRequestAsync("ResetCoaTaskOrg", new { affiliation, coaPoid }, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region COA Import/Export

    public async Task<string> ImportCoaContentAsync(string content, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync("ImportCoaContent", new { content }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> ImportCoaAsync(ObjectSet os, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync("ImportCoaFromObjectSet", ObjectsParam(os), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// <see cref="ImportCoaAsync(ObjectSet, CancellationToken)"/> with a per-call load timeout
    /// (<c>ImportCoaFromObjectSet</c>); see <see cref="StpObjectSetLoadException"/>.
    /// </summary>
    public async Task<string> ImportCoaAsync(ObjectSet os, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        return await SendLoadRequestAsync("ImportCoaFromObjectSet", ObjectsParam(os), timeout, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// The COA with its units, graphics and tasks as STP object-set text (<c>object_set([...])</c>) -
    /// the form <see cref="ImportCoaContentAsync"/> takes back. Null when the engine answers with nothing.
    /// </summary>
    /// <remarks>
    /// Returns the text unchanged (STP-1092). Before 0.6.1 this returned <c>JToken.Parse(result)</c>,
    /// and object-set text is not JSON, so every real answer threw <c>JsonReaderException</c>.
    /// Use <see cref="GetCoaObjectSetAsync"/> for the parsed objects.
    /// </remarks>
    public async Task<string> GetCoaContentAsync(string poid, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("GetCoaContent", new { poid }, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrEmpty(result) ? null : result;
    }

    public async Task<ObjectSet> GetCoaObjectSetAsync(string poid, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("GetCoaObjectSet", new { poid }, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(result)) return new ObjectSet();
        return ParseObjectSetResponse(result);
    }

    public async Task DeleteCoaAsync(string poid, CancellationToken cancellationToken = default)
    {
        await SendRequestAsync("DeleteCoa", new { poid }, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Role and COA

    /// <summary>
    /// Switch this client's role. Sends <c>SetRole</c> with <c>role</c>; see <see cref="StpRole"/> for the
    /// values the contract lists.
    /// </summary>
    public async Task SetRoleAsync(string newRole, CancellationToken cancellationToken = default)
    {
        await SendRequestAsync("SetRole", new { role = newRole }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Switch this client's role, sending the contract's optional <c>createIfNone</c> as well (whether to
    /// create a default COA for the role when there is none). The contract notes the engine ignores it
    /// over this API - no COA is created - so it is sent only for clients that need to state it.
    /// </summary>
    public async Task SetRoleAsync(string newRole, bool createIfNone, CancellationToken cancellationToken = default)
    {
        await SendRequestAsync("SetRole", new { role = newRole, createIfNone }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Make a COA the current one. Sends <c>SetCurrentCoa</c> with the contract's single param
    /// <c>poid</c> (STP-1798; earlier versions sent it as <c>coaPoid</c>, a name the contract does not list).
    /// </summary>
    public async Task SetCoaAsync(string newCoaPoid, CancellationToken cancellationToken = default)
    {
        await SendRequestAsync("SetCurrentCoa", new { poid = newCoaPoid }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> GetCurrentCoaPoidAsync(string role, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync("GetCurrentCoaPoid", new { role }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> CreateDefaultCoaAsync(string role)
    {
        return await SendRequestAsync("CreateDefaultCoa", new { role }, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Switch role, then make a COA current. The contract has no <c>SwitchRoleAndCoa</c> method (the engine
    /// does not dispatch it), so this is an SDK convenience over the two contract calls: it sends
    /// <c>SetRole</c> with <c>role</c>, waits for its answer, then sends <c>SetCurrentCoa</c> with
    /// <c>poid</c>. The COA is selected after the role because a role switch can change the current COA.
    /// A null or blank <paramref name="newCoaPoid"/> sends <c>SetRole</c> only (the engine refuses a blank
    /// poid). A refusal of either call throws <see cref="StpException"/>; if <c>SetRole</c> is refused,
    /// <c>SetCurrentCoa</c> is not sent. (STP-1799; earlier versions sent a single <c>SwitchRoleAndCoa</c>
    /// with <c>role</c> and <c>coaPoid</c>.)
    /// </summary>
    public async Task SwitchRoleAndCoaAsync(string newRole, string newCoaPoid, CancellationToken cancellationToken = default)
    {
        await SetRoleAsync(newRole, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(newCoaPoid))
            await SetCoaAsync(newCoaPoid, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends the wire method "SwitchTaskConfirmation", which has no case arm in the engine's
    /// dispatcher (BridgingAgents/WebSocketsBridge/JsonMessage.cs / StpJsonClient.cs) on any release
    /// line. The call is answered success=false with a null result and does nothing. Use
    /// <see cref="StpRecognizerCommands.ConfirmTask(string, int, bool)"/> instead, which sends the
    /// engine's actual "ConfirmTask" wire method (StpJsonClient.cs:289).
    /// </summary>
    [Obsolete("SwitchTaskConfirmation is not dispatched by the engine (no case arm on any release line) and silently does nothing. Use StpRecognizer.ConfirmTask instead.")]
    public async Task SwitchTaskConfirmationAsync(string poid, int index, bool isConfirmed)
    {
        await SendRequestAsync("SwitchTaskConfirmation", new { poid, index, isConfirmed }, CancellationToken.None).ConfigureAwait(false);
    }

    #endregion

    #region Queries

    public async Task<List<StpCoa>> RequestActiveCoasAsync(CancellationToken cancellationToken)
    {
        var result = await SendRequestAsync("GetActiveCoaList", new { }, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(result)) return new List<StpCoa>();
        return JsonConvert.DeserializeObject<List<StpCoa>>(result) ?? new List<StpCoa>();
    }

    public async Task<List<StpObject>> RequestAllStpObjectsAsync(CancellationToken cancellationToken)
    {
        var result = await SendRequestAsync("GetAllObjects", new { }, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(result)) return new List<StpObject>();
        var objects = JsonConvert.DeserializeObject<List<StpObject>>(result) ?? new List<StpObject>();
        return objects.Select(o => o.AsTypedObject()).ToList();
    }

    public async Task<List<StpCoa>> GetScenarioCoasAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("GetScenarioCoaList", new { }, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(result)) return new List<StpCoa>();
        return JsonConvert.DeserializeObject<List<StpCoa>>(result) ?? new List<StpCoa>();
    }

    public async Task<List<StpTaskOrg>> GetScenarioTaskOrgsAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("GetScenarioTaskOrgList", new { }, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(result)) return new List<StpTaskOrg>();
        return JsonConvert.DeserializeObject<List<StpTaskOrg>>(result) ?? new List<StpTaskOrg>();
    }

    public async Task<List<StpObject>> GetTaskOrgObjectsAsync(string poid, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("GetTaskOrgObjects", new { poid }, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(result)) return new List<StpObject>();
        var objects = JsonConvert.DeserializeObject<List<StpObject>>(result) ?? new List<StpObject>();
        return objects.Select(o => o.AsTypedObject()).ToList();
    }

    public async Task<List<StpObject>> GetCoaObjectsAsync(string poid, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("GetCoaObjects", new { poid }, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(result)) return new List<StpObject>();
        var objects = JsonConvert.DeserializeObject<List<StpObject>>(result) ?? new List<StpObject>();
        return objects.Select(o => o.AsTypedObject()).ToList();
    }

    public async Task<StpObject> RequestPoidObjectAsync(string poid, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("GetPoidObject", new { poid }, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(result)) return null;
        var obj = JsonConvert.DeserializeObject<StpObject>(result);
        return obj?.AsTypedObject();
    }

    public async Task<List<StpObject>> RequestDeletedStpObjectsAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("GetDeletedObjects", new { }, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(result)) return new List<StpObject>();
        var objects = JsonConvert.DeserializeObject<List<StpObject>>(result) ?? new List<StpObject>();
        return objects.Select(o => o.AsTypedObject()).ToList();
    }

    #endregion

    #region Task History (STP-1250, contract 0.9.11)

    /// <summary>
    /// A task's history, newest first: what was done to the task, by whom and from where, and the
    /// comments on it. Sends <c>GetTaskHistory</c>. Each item is the entry's poid (what
    /// <see cref="EditTaskCommentAsync"/> and <see cref="DeleteTaskCommentAsync"/> name) and the entry.
    /// Entries outlive a deleted task. An unknown poid, or a task with no history, answers an empty
    /// list. To follow it live, attach <see cref="OnTaskEventAdded"/> and <see cref="OnTaskEventModified"/>.
    /// </summary>
    /// <param name="taskPoid">The task's unique id (from TaskAdded).</param>
    public async Task<List<TaskHistoryItem>> GetTaskHistoryAsync(string taskPoid, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("GetTaskHistory", new { taskPoid }, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(result)) return new List<TaskHistoryItem>();
        return JsonConvert.DeserializeObject<List<TaskHistoryItem>>(result) ?? new List<TaskHistoryItem>();
    }

    /// <summary>
    /// Add a comment to a task's history. Sends <c>AddTaskComment</c>. The engine stamps the author
    /// (the user this client registered with), role, time and kind; the client sends only the text.
    /// Answers the new entry's poid; <see cref="OnTaskEventAdded"/> goes to every client of the session.
    /// Refused (<see cref="StpException"/>) when the client registered without a user, or for a
    /// missing task or blank text.
    /// </summary>
    /// <param name="taskPoid">The task's unique id (from TaskAdded).</param>
    /// <param name="text">The comment. Required; blank is refused.</param>
    /// <returns>The new entry's poid.</returns>
    public async Task<string> AddTaskCommentAsync(string taskPoid, string text, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("AddTaskComment", new { taskPoid, text }, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrEmpty(result) ? null : result;
    }

    /// <summary>
    /// Change the text of a comment. Sends <c>EditTaskComment</c>. Only its author may; the earlier
    /// text is kept in the entry's detail as revisions. <see cref="OnTaskEventModified"/> goes to every
    /// client of the session. Refused (<see cref="StpException"/>) for a system entry, someone else's
    /// comment, a deleted comment, or blank text.
    /// </summary>
    /// <param name="entryPoid">The comment's entry poid (from GetTaskHistory or TaskEventAdded).</param>
    /// <param name="text">The new text. Required; blank is refused.</param>
    public async Task<bool> EditTaskCommentAsync(string entryPoid, string text, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("EditTaskComment", new { entryPoid, text }, cancellationToken).ConfigureAwait(false);
        return IsTrueAnswer(result);
    }

    /// <summary>
    /// Delete a comment. Sends <c>DeleteTaskComment</c>. Only its author may. The entry stays as a
    /// tombstone (deleted set, text and revisions cleared). <see cref="OnTaskEventModified"/> goes to
    /// every client of the session. Refused (<see cref="StpException"/>) for a system entry or someone
    /// else's comment.
    /// </summary>
    /// <param name="entryPoid">The comment's entry poid (from GetTaskHistory or TaskEventAdded).</param>
    public async Task<bool> DeleteTaskCommentAsync(string entryPoid, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("DeleteTaskComment", new { entryPoid }, cancellationToken).ConfigureAwait(false);
        return IsTrueAnswer(result);
    }

    #endregion

    #region C2SIM

    public async Task<string> GetC2SIMContentAsync(
        string name,
        C2CIMDataType dataType,
        string affiliation,
        List<string> coaPoids,
        Dictionary<string, object> options,
        CancellationToken cancellationToken)
    {
        return await SendRequestAsync("GetC2SIMContent", new
        {
            name,
            dataType = dataType.ToString(),
            affiliation,
            coaPoids,
            options
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task PushC2SIMContentAsync(
        string content,
        C2CIMDataType dataType,
        Dictionary<string, object> options,
        CancellationToken cancellationToken)
    {
        await SendRequestAsync("PushC2SIMContent", new
        {
            content,
            dataType = dataType.ToString(),
            options
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Fetch the C2SIM Initialization document from the C2SIM server. Prefer
    /// <see cref="PullC2SIMInitializationContentAsync"/>, which returns the contract's result as it is.
    /// </summary>
    /// <remarks>
    /// The contract (0.9.11) answers with the XML document as a plain string; the server status is not
    /// returned over this API. This method used to parse the answer as a JSON object
    /// <c>{content, serverStatus}</c>, which threw on every contract answer. It now returns the string
    /// as <c>Content</c> with a null <c>ServerStatus</c>, and still reads the old object form when an
    /// engine sends it.
    /// </remarks>
    public async Task<(string Content, string ServerStatus)> PullC2SIMInitializationAsync(
        Dictionary<string, object> options = null,
        CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("PullC2SIMInitialization", new { options }, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(result)) return (null, null);

        if (result.TrimStart().StartsWith("{"))
        {
            try
            {
                var json = StpJsonRpcConnector.ParseVerbatim(result);
                if (json["content"] != null || json["serverStatus"] != null)
                    return (json["content"]?.ToString(), json["serverStatus"]?.ToString());
            }
            catch (JsonReaderException)
            {
                // Not JSON after all: the document itself.
            }
        }
        return (result, null);
    }

    /// <summary>
    /// Fetch the C2SIM Initialization document (for example the order of battle) from the external C2SIM
    /// server named by <paramref name="options"/> (<see cref="StpC2SIMOptions.RestUrl"/>; the engine's
    /// configured server when omitted) and return it as XML, as the contract specifies. Nothing is loaded
    /// into the scenario: convert it with <see cref="ConvertC2SIMContentAsync"/> and import the result.
    /// Null when the engine answers with nothing.
    /// </summary>
    /// <param name="options">Options for this call only; an <see cref="StpC2SIMOptions"/> gives them typed.
    /// An unknown key is refused by the engine.</param>
    /// <param name="cancellationToken">Token to stop waiting for the answer.</param>
    public async Task<string> PullC2SIMInitializationContentAsync(
        Dictionary<string, object> options = null,
        CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("PullC2SIMInitialization", new { options }, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrEmpty(result) ? null : result;
    }

    /// <summary>
    /// Converts C2SIM content to native STP content, formatted as object_set([[element1], [element2], ...]).
    /// Engine dispatch: StpJsonClient.cs:263 - the arm assigns result = null with a comment that the
    /// call was removed from the SDK; success stays true. The engine currently returns null without
    /// converting anything, so a null result here does NOT mean failure - it means the engine has not
    /// implemented conversion. Do not treat a null return as an error.
    /// </summary>
    /// <param name="content">C2SIM-formatted content</param>
    /// <param name="options">Connector-specific options</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Stp native content, or null - see remarks</returns>
    public async Task<string> ConvertC2SIMContentAsync(string content, Dictionary<string, object> options, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("ConvertC2SIMContent", new { content, options }, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrEmpty(result) ? null : result;
    }

    #endregion

    #region Request Helper

    /// <summary>
    /// The engine answers the ObjectSet getters (GetScenarioObjectSet, GetTaskOrgObjectSet,
    /// GetCoaObjectSet) with a BARE ARRAY of objects - WebSocketsBridge sends `os?.Objects` /
    /// a List&lt;StpObject&gt;, never {"objects":[...]}. Deserialising ObjectSet directly threw on
    /// every live call before 0.4.2-preview. The wrapped form is still accepted.
    /// </summary>
    internal static ObjectSet ParseObjectSetResponse(string result)
    {
        if (string.IsNullOrWhiteSpace(result)) return new ObjectSet();
        string trimmed = result.TrimStart();
        if (trimmed.StartsWith("["))
        {
            var list = JsonConvert.DeserializeObject<List<StpObject>>(result);
            return new ObjectSet(list);
        }
        return JsonConvert.DeserializeObject<ObjectSet>(result) ?? new ObjectSet();
    }

    /// <summary>
    /// A boolean answer as the connector delivers it. The engine answers with a JSON boolean, and the
    /// connector hands its <c>ToString()</c> on, which is <c>"True"</c>/<c>"False"</c> - so a
    /// case-sensitive compare with <c>"true"</c> never matched and HasActiveScenarioAsync was always
    /// false (STP-1093). <see cref="bool.TryParse(string, out bool)"/> ignores case.
    /// </summary>
    internal static bool IsTrueAnswer(string result) => bool.TryParse(result?.Trim(), out bool value) && value;

    /// <summary>
    /// The answer to a call whose contract result is null (<c>ImportPlanData*</c>): the connector delivers
    /// that as an empty string, and a refusal throws before this is reached, so any answer is success
    /// except an explicit boolean false (STP-1799).
    /// </summary>
    internal static bool IsImportAccepted(string result) =>
        !(bool.TryParse(result?.Trim(), out bool value) && !value);

    /// <summary>Any non-empty answer other than a boolean false (in any casing) - STP-1093.</summary>
    internal static bool IsSuccessAnswer(string result) =>
        !string.IsNullOrWhiteSpace(result) && !(bool.TryParse(result.Trim(), out bool value) && !value);

    /// <summary>
    /// Params for the *FromObjectSet requests: the objects as an ARRAY under <c>objects</c>.
    /// </summary>
    /// <remarks>
    /// That is the one property the engine reads - each of these methods deserialises into a
    /// WebSocketsBridge DTO holding <c>[JsonProperty("objects")] List&lt;StpObject&gt;</c> - and
    /// what the public contract and the JS SDK specify. Until STP-1067 this SDK sent the set
    /// serialised to a string under <c>content</c>; the engine saw <c>objects</c> = null and
    /// refused every call.
    /// </remarks>
    private static object ObjectsParam(ObjectSet os) => new { objects = os?.Objects };

    /// <summary>
    /// How much longer than the engine's load timeout the client waits, so the engine's report of a
    /// load it cut off arrives before the client gives up (the contract: a client whose own timeout
    /// fires first receives only the StpMessage announcement).
    /// </summary>
    internal static readonly TimeSpan LoadReportGrace = TimeSpan.FromSeconds(5);

    /// <summary>The engine-side timeout for a load, in whole seconds rounded up; 0 = none.</summary>
    internal static int LoadTimeoutSeconds(TimeSpan timeout)
    {
        if (timeout < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "The load timeout cannot be negative; use TimeSpan.Zero for none.");
        return (int)Math.Ceiling(timeout.TotalSeconds);
    }

    /// <summary>
    /// An object-set load with its own engine timeout. A refusal - the engine stopping on an object, or
    /// cutting the load off at the timeout - is rethrown as <see cref="StpObjectSetLoadException"/>
    /// carrying the engine's report verbatim.
    /// </summary>
    private async Task<string> SendLoadRequestAsync(string method, object parameters, TimeSpan timeout, CancellationToken cancellationToken)
    {
        int engineSeconds = LoadTimeoutSeconds(timeout);
        int clientMs = engineSeconds == 0
            ? Timeout.Infinite
            : (int)Math.Min(int.MaxValue, (engineSeconds * 1000L) + (long)LoadReportGrace.TotalMilliseconds);

        int cookie = NextCookie();
        var message = SerializeRequest(method, parameters, cookie);
        try
        {
            if (_connector is IStpExtendedConnector ext)
                return await ext.SendRequestAsync(message, cookie, engineSeconds, clientMs, cancellationToken).ConfigureAwait(false);
            // A plain IStpConnector sends one timeout for both sides, so the client may give up as the
            // engine does and miss the report; the engine still announces it as StpMessage.
            return await _connector.SendRequestAsync(message, cookie, engineSeconds == 0 ? clientMs : engineSeconds * 1000, cancellationToken).ConfigureAwait(false);
        }
        catch (StpException ex) when (ex is not StpObjectSetLoadException)
        {
            throw new StpObjectSetLoadException(method, ex.Message, ex);
        }
    }

    private static string SerializeRequest(string method, object parameters, int cookie) =>
        JsonConvert.SerializeObject(new
        {
            method = method,
            @params = parameters,
            id = cookie
        }, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

    private async Task<string> SendRequestAsync(string method, object parameters, CancellationToken cancellationToken)
    {
        int cookie = NextCookie();
        var message = SerializeRequest(method, parameters, cookie);

        return await _connector.SendRequestAsync(message, cookie, DefaultTimeoutMs, cancellationToken).ConfigureAwait(false);
    }

    #endregion
}
