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
        var content = JsonConvert.SerializeObject(os);
        await SendRequestAsync("LoadNewScenarioFromObjectSet", new { content }, cancellationToken).ConfigureAwait(false);
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
        var content = JsonConvert.SerializeObject(localObjects);
        await SendRequestAsync("SyncScenarioSessionFromObjectSet", new { content }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ImportSTPDataAsync(string content, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("ImportPlanData", new { content }, cancellationToken).ConfigureAwait(false);
        return !string.IsNullOrEmpty(result) && result != "false";
    }

    public async Task<bool> ImportSTPDataAsync(ObjectSet stpObjects, CancellationToken cancellationToken = default)
    {
        var content = JsonConvert.SerializeObject(stpObjects);
        var result = await SendRequestAsync("ImportPlanDataFromObjectSet", new { content }, cancellationToken).ConfigureAwait(false);
        return !string.IsNullOrEmpty(result) && result != "false";
    }

    public async Task<bool> HasActiveScenarioAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("HasActiveScenario", new { }, cancellationToken).ConfigureAwait(false);
        return result == "true";
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
        var content = JsonConvert.SerializeObject(os);
        return await SendRequestAsync("ImportTaskOrgFromObjectSet", new { content }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<object> GetTaskOrgContentAsync(string poid, CancellationToken cancellationToken)
    {
        var result = await SendRequestAsync("GetTaskOrgContent", new { poid }, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(result)) return null;
        return JToken.Parse(result);
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

    #endregion

    #region COA Import/Export

    public async Task<string> ImportCoaContentAsync(string content, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync("ImportCoaContent", new { content }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> ImportCoaAsync(ObjectSet os, CancellationToken cancellationToken = default)
    {
        var content = JsonConvert.SerializeObject(os);
        return await SendRequestAsync("ImportCoaFromObjectSet", new { content }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<object> GetCoaContentAsync(string poid, CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("GetCoaContent", new { poid }, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(result)) return null;
        return JToken.Parse(result);
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

    public async Task SetRoleAsync(string newRole, CancellationToken cancellationToken = default)
    {
        await SendRequestAsync("SetRole", new { role = newRole }, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetCoaAsync(string newCoaPoid, CancellationToken cancellationToken = default)
    {
        await SendRequestAsync("SetCurrentCoa", new { coaPoid = newCoaPoid }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> GetCurrentCoaPoidAsync(string role, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync("GetCurrentCoaPoid", new { role }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> CreateDefaultCoaAsync(string role)
    {
        return await SendRequestAsync("CreateDefaultCoa", new { role }, CancellationToken.None).ConfigureAwait(false);
    }

    public async Task SwitchRoleAndCoaAsync(string newRole, string newCoaPoid, CancellationToken cancellationToken = default)
    {
        await SendRequestAsync("SwitchRoleAndCoa", new { role = newRole, coaPoid = newCoaPoid }, cancellationToken).ConfigureAwait(false);
    }

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

    public async Task<(string Content, string ServerStatus)> PullC2SIMInitializationAsync(
        Dictionary<string, object> options = null,
        CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("PullC2SIMInitialization", new { options }, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(result)) return (null, null);

        var json = JObject.Parse(result);
        var content = json["content"]?.ToString();
        var serverStatus = json["serverStatus"]?.ToString();
        return (content, serverStatus);
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

    private async Task<string> SendRequestAsync(string method, object parameters, CancellationToken cancellationToken)
    {
        int cookie = NextCookie();
        var message = JsonConvert.SerializeObject(new
        {
            method = method,
            @params = parameters,
            id = cookie
        }, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

        return await _connector.SendRequestAsync(message, cookie, DefaultTimeoutMs, cancellationToken).ConfigureAwait(false);
    }

    #endregion
}
