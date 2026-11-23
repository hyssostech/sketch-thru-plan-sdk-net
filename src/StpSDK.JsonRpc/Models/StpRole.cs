using System.Collections.Generic;

namespace StpSDK;

/// <summary>
/// The roles the contract (0.9.11) lists for <c>SetRole</c> (schema <c>StpRole</c>), for
/// <see cref="StpRecognizer.SetRoleAsync(string, System.Threading.CancellationToken)"/>. The engine
/// accepts them in any case and confirms the switch with <c>RoleSwitched</c>, the role in lower case.
/// S2 works on the hostile side, the others on the friendly side.
/// </summary>
public static class StpRole
{
    /// <summary>Intelligence (hostile side).</summary>
    public const string S2 = "S2";

    /// <summary>Operations.</summary>
    public const string S3 = "S3";

    /// <summary>Logistics.</summary>
    public const string S4 = "S4";

    /// <summary>Fire support officer.</summary>
    public const string FSO = "FSO";

    /// <summary>Engineer.</summary>
    public const string ENGINEER = "ENGINEER";

    /// <summary>
    /// Alias of <see cref="ENGINEER"/>, and the same value. The contract also accepts the wire value
    /// <c>ENG</c> (STP-1159), but engines before STP-1159 confirm it as <c>eng</c>, which their task
    /// recognizer does not know - it keeps the client's previous role - so this alias sends
    /// <c>ENGINEER</c>, which every engine takes. Send the string <c>"ENG"</c> yourself only if you need
    /// that exact value on the wire.
    /// </summary>
    public const string ENG = ENGINEER;

    /// <summary>
    /// The canonical role values, in the contract's order (without the <c>ENG</c> alias). <c>SetRole</c>
    /// also takes <c>none</c> to clear the role, as <c>ResetRole</c> does; it is not part of the enum.
    /// </summary>
    public static IReadOnlyList<string> All { get; } = new[] { S2, S3, S4, FSO, ENGINEER };
}
