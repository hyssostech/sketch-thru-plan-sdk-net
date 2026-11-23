using System;
using System.Collections.Generic;

namespace StpSDK;

/// <summary>
/// Typed options for the C2SIM calls - <see cref="StpRecognizer.GetC2SIMContentAsync"/>,
/// <see cref="StpRecognizer.PushC2SIMContentAsync"/>, <see cref="StpRecognizer.PullC2SIMInitializationContentAsync"/>
/// and <see cref="StpRecognizer.PullC2SIMInitializationAsync"/> - with the key set the contract (0.9.11)
/// enumerates for their <c>options</c> param, in the camelCase spelling it lists (the JS SDK's
/// <c>StpC2SIMOptions</c>).
/// </summary>
/// <remarks>
/// <para>It IS the options dictionary those methods already take, so it passes straight to them with no
/// new overloads: each property reads and writes its key, and a property left unset - or set back to
/// null - has no key, so it is omitted from the wire. Options apply to that call only; the engine refuses
/// an unknown key (it compares key names case-insensitively), so add raw keys through the indexer only
/// for an engine that lists them.</para>
/// <para><see cref="RefuseUnconfirmedPlaceholders"/> acts on <c>GetC2SIMContent</c>, where the engine
/// builds the document; the pull and push accept it for symmetry with no effect.</para>
/// <para><see cref="StpRecognizer.ConvertC2SIMContentAsync"/> also takes an options dictionary, which the
/// contract says the engine ignores.</para>
/// </remarks>
public class StpC2SIMOptions : Dictionary<string, object>
{
    /// <summary>C2SIM REST endpoint, host:port/path, e.g. http://10.2.10.30:8080/C2SIMServer.</summary>
    public string RestUrl
    {
        get => GetString("restUrl");
        set => SetOrRemove("restUrl", value);
    }

    /// <summary>C2SIM REST endpoint password. Applied to this call only; it is never stored.</summary>
    public string RestPassword
    {
        get => GetString("restPassword");
        set => SetOrRemove("restPassword", value);
    }

    /// <summary>STOMP endpoint, host:port/destination. Used to build this call's REST client; it does NOT move the engine's long-lived notification subscription.</summary>
    public string StompUrl
    {
        get => GetString("stompUrl");
        set => SetOrRemove("stompUrl", value);
    }

    /// <summary>C2SIM server protocol version.</summary>
    public string ServerProtocol
    {
        get => GetString("serverProtocol");
        set => SetOrRemove("serverProtocol", value);
    }

    /// <summary>Value of SystemEntityList/SystemName in the generated Initialization document.</summary>
    public string SystemName
    {
        get => GetString("systemName");
        set => SetOrRemove("systemName", value);
    }

    /// <summary>Force the C2SIM server to Uninitialized before pushing Initialization, so it is not merged with a previous one.</summary>
    public bool? ResetBeforeInitialize
    {
        get => GetBool("resetBeforeInitialize");
        set => SetOrRemove("resetBeforeInitialize", value);
    }

    /// <summary>Switch the C2SIM server to Running immediately after pushing Initialization.</summary>
    public bool? RunAfterInitialize
    {
        get => GetBool("runAfterInitialize");
        set => SetOrRemove("runAfterInitialize", value);
    }

    /// <summary>Include the full Task Org, including entities not placed on the map. When false, only placed elements and their subordinates.</summary>
    public bool? FullTO
    {
        get => GetBool("fullTO");
        set => SetOrRemove("fullTO", value);
    }

    /// <summary>Refuse the document while a task graphic is still an unconfirmed rung-5 placeholder that COARenderer drew from no evidence (not moved, its confidence not set, by staff); the call then fails with the graphics named. Otherwise every unconfirmed estimate is only warned, in the engine log. Default false.</summary>
    public bool? RefuseUnconfirmedPlaceholders
    {
        get => GetBool("refuseUnconfirmedPlaceholders");
        set => SetOrRemove("refuseUnconfirmedPlaceholders", value);
    }

    /// <summary>Schema version hint (1.0.0 / 1.0.1 / 1.0.2). All inputs generate 1.0.2 unified output.</summary>
    public string SchemaVersion
    {
        get => GetString("schemaVersion");
        set => SetOrRemove("schemaVersion", value);
    }

    /// <summary>Include tactical graphics that are not part of any task in the Initialization document.</summary>
    public bool? AddOrphanTgToInitialization
    {
        get => GetBool("addOrphanTgToInitialization");
        set => SetOrRemove("addOrphanTgToInitialization", value);
    }

    /// <summary>Place tactical graphics in the Initialization document instead of the Order document.</summary>
    public bool? PlaceAllTgInInitialization
    {
        get => GetBool("placeAllTgInInitialization");
        set => SetOrRemove("placeAllTgInInitialization", value);
    }

    /// <summary>Include MapGraphicID references to tactical graphics in tasks, rather than relying on Location.</summary>
    public bool? IncludeMapGraphicId
    {
        get => GetBool("includeMapGraphicId");
        set => SetOrRemove("includeMapGraphicId", value);
    }

    /// <summary>Maximum number of characters in a generated entity name; 0 for no limit.</summary>
    public int? EntityNameCharLimit
    {
        get => GetInt("entityNameCharLimit");
        set => SetOrRemove("entityNameCharLimit", value);
    }

    /// <summary>Append the description to generated entity names - required to round-trip an ORBAT imported from C2SIM documents.</summary>
    public bool? IncludeDescriptionInName
    {
        get => GetBool("includeDescriptionInName");
        set => SetOrRemove("includeDescriptionInName", value);
    }

    /// <summary>Default rules of engagement. The value is matched CASE-SENSITIVELY against the engine's ROE members, unlike the key; any other value (for example the pre-0.6.17 SDK spellings ROEHold / ROEFree / ROETight) is not applied - the engine logs a warning and keeps its configured default.</summary>
    public string RulesOfEngagement
    {
        get => GetString("rulesOfEngagement");
        set => SetOrRemove("rulesOfEngagement", value);
    }

    /// <summary>Use Position Report information to update units. ACCEPTED BUT INERT for a single call: it is read only by the engine's inbound report handler, which has no per-call context.</summary>
    public bool? UpdateUnitPositions
    {
        get => GetBool("updateUnitPositions");
        set => SetOrRemove("updateUnitPositions", value);
    }

    /// <summary>Default document export folder.</summary>
    public string ExportFileDir
    {
        get => GetString("exportFileDir");
        set => SetOrRemove("exportFileDir", value);
    }

    /// <summary>Path to a schema file to validate generated XML against; empty for no validation.</summary>
    public string ValidationSchema
    {
        get => GetString("validationSchema");
        set => SetOrRemove("validationSchema", value);
    }

    /// <summary>Mission start date. ACCEPTED BUT INERT: nothing in the C2SIM bridge reads it.</summary>
    public string StartDate
    {
        get => GetString("startDate");
        set => SetOrRemove("startDate", value);
    }

    /// <summary>Duration of each STP phase, in minutes.</summary>
    public int? PhaseDuration
    {
        get => GetInt("phaseDuration");
        set => SetOrRemove("phaseDuration", value);
    }

    /// <summary>Sender unique id.</summary>
    public string FromSenderUUID
    {
        get => GetString("fromSenderUUID");
        set => SetOrRemove("fromSenderUUID", value);
    }

    /// <summary>Receiver unique id.</summary>
    public string ToReceiverUUID
    {
        get => GetString("toReceiverUUID");
        set => SetOrRemove("toReceiverUUID", value);
    }

    /// <summary>Friendly forces name.</summary>
    public string FriendFstName
    {
        get => GetString("friendFstName");
        set => SetOrRemove("friendFstName", value);
    }

    /// <summary>Friendly forces unique id.</summary>
    public string FriendFstUUID
    {
        get => GetString("friendFstUUID");
        set => SetOrRemove("friendFstUUID", value);
    }

    /// <summary>Hostile forces name.</summary>
    public string HostileFstName
    {
        get => GetString("hostileFstName");
        set => SetOrRemove("hostileFstName", value);
    }

    /// <summary>Hostile forces unique id.</summary>
    public string HostileFstUUID
    {
        get => GetString("hostileFstUUID");
        set => SetOrRemove("hostileFstUUID", value);
    }

    /// <summary>Neutral forces name.</summary>
    public string NeutralFstName
    {
        get => GetString("neutralFstName");
        set => SetOrRemove("neutralFstName", value);
    }

    /// <summary>Neutral forces unique id.</summary>
    public string NeutralFstUUID
    {
        get => GetString("neutralFstUUID");
        set => SetOrRemove("neutralFstUUID", value);
    }

    /// <summary>Unknown forces name. ACCEPTED BUT INERT: nothing in the C2SIM bridge reads it.</summary>
    public string UnknownFstName
    {
        get => GetString("unknownFstName");
        set => SetOrRemove("unknownFstName", value);
    }

    /// <summary>Unknown forces unique id. ACCEPTED BUT INERT: nothing in the C2SIM bridge reads it.</summary>
    public string UnknownFstUUID
    {
        get => GetString("unknownFstUUID");
        set => SetOrRemove("unknownFstUUID", value);
    }

    private void SetOrRemove(string key, object value)
    {
        if (value == null) Remove(key);
        else this[key] = value;
    }

    private string GetString(string key) => TryGetValue(key, out var v) && v != null ? Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) : null;

    private bool? GetBool(string key) => TryGetValue(key, out var v) && v != null ? Convert.ToBoolean(v, System.Globalization.CultureInfo.InvariantCulture) : null;

    private int? GetInt(string key) => TryGetValue(key, out var v) && v != null ? Convert.ToInt32(v, System.Globalization.CultureInfo.InvariantCulture) : null;
}
