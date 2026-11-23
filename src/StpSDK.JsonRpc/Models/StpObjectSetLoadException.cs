using System;

namespace StpSDK;

/// <summary>
/// An object-set load that the engine refused, stopped part way, or cut off at its timeout - thrown by
/// the per-call timeout overloads of the object-set imports (<c>LoadNewScenarioFromObjectSet</c>,
/// <c>ImportPlanDataFromObjectSet</c>, <c>ImportCoaFromObjectSet</c>, <c>ImportTaskOrgFromObjectSet</c>,
/// <c>SyncScenarioSessionFromObjectSet</c>).
/// </summary>
/// <remarks>
/// The contract (0.9.11) answers such a load with <c>success: false</c> and a text report:
/// the objects applied (they stay applied and were announced), any rejected and skipped, the object in
/// flight when the timeout hit (sent with no reply yet - the engine may still apply and announce it) and
/// those not sent; for an object the engine rejected, which one and why. <see cref="Report"/> is that text,
/// verbatim. The engine also announces it as an <c>StpMessage</c>, prefixed with the method name - all a
/// client whose own wait ended first receives. Engines before STP-1095 answer
/// "The operation was canceled.", naming nothing. It derives from <see cref="StpException"/>, so existing
/// handlers still catch it.
/// </remarks>
public class StpObjectSetLoadException : StpException
{
    /// <summary>The wire method that was refused, e.g. <c>ImportPlanDataFromObjectSet</c>.</summary>
    public string Method { get; }

    /// <summary>The engine's report, verbatim.</summary>
    public string Report { get; }

    public StpObjectSetLoadException(string method, string report, Exception inner)
        : base(report, inner)
    {
        Method = method;
        Report = report;
    }
}
