using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace StpSDK;

/// <summary>
/// One entry of a task's history (STP-1250; contract schema <c>TaskHistoryEntry</c>), as
/// <see cref="StpRecognizer.GetTaskHistoryAsync"/> returns it and
/// <see cref="StpRecognizer.OnTaskEventAdded"/> / <see cref="StpRecognizer.OnTaskEventModified"/> carry it.
/// Not <see cref="StpTask"/>: this is what was done to a task, not the task. The engine writes
/// entries; a client only adds, edits and deletes its own comments.
/// </summary>
/// <remarks>
/// <see cref="Kind"/> and <see cref="Origin"/> are strings, not enums: the contract says later
/// engines add kinds, and an unknown value must not cost the entry. Properties this build does not
/// know are kept in <see cref="AdditionalProperties"/> (the schema allows them).
/// </remarks>
public class TaskHistoryEntry
{
    /// <summary>The poid of the task the entry is about. Entries outlive a deleted task.</summary>
    [JsonProperty("task")]
    public string Task { get; set; }

    /// <summary>When it happened, ISO 8601 UTC.</summary>
    [JsonProperty("utc")]
    public string Utc { get; set; }

    /// <summary>
    /// What happened: created, edited, retasked, confidence, confirmed, unconfirmed,
    /// alternate_chosen, deleted, undone, or comment. Open: show an unknown kind as text.
    /// </summary>
    [JsonProperty("kind")]
    public string Kind { get; set; }

    /// <summary><c>system</c> for an entry the engine recorded from a change; <c>user</c> for a comment.</summary>
    [JsonProperty("origin")]
    public string Origin { get; set; }

    /// <summary>The person who made the change or wrote the comment, or empty.</summary>
    [JsonProperty("author")]
    public string Author { get; set; }

    /// <summary>The role the author's client had (s2, s3, ...), or empty.</summary>
    [JsonProperty("role")]
    public string Role { get; set; }

    /// <summary>The component that made the change (STP Agent, RESTConnector, WebSocketsBridge, ...).</summary>
    [JsonProperty("source")]
    public string Source { get; set; }

    /// <summary>A human-readable line, or a comment's text. Null for a deleted comment.</summary>
    [JsonProperty("text")]
    public string Text { get; set; }

    /// <summary>
    /// A JSON object AS A STRING with the specifics (an edit's fields, a comment's revisions).
    /// Null when a comment is deleted. Left unparsed, as the contract sends it.
    /// </summary>
    [JsonProperty("detail")]
    public string Detail { get; set; }

    /// <summary>When a comment was last edited, ISO 8601 UTC; null if never.</summary>
    [JsonProperty("edited")]
    public string Edited { get; set; }

    /// <summary>When a comment was deleted, ISO 8601 UTC; null if it stands (a tombstone).</summary>
    [JsonProperty("deleted")]
    public string Deleted { get; set; }

    /// <summary>Properties a later engine sends that this build does not model.</summary>
    [JsonExtensionData]
    public IDictionary<string, JToken> AdditionalProperties { get; set; }
}

/// <summary>
/// One item of <see cref="StpRecognizer.GetTaskHistoryAsync"/>'s answer: the entry's poid (what
/// <see cref="StpRecognizer.EditTaskCommentAsync"/> and <see cref="StpRecognizer.DeleteTaskCommentAsync"/>
/// name) and the entry.
/// </summary>
public class TaskHistoryItem
{
    [JsonProperty("poid")]
    public string Poid { get; set; }

    [JsonProperty("entry")]
    public TaskHistoryEntry Entry { get; set; }
}
