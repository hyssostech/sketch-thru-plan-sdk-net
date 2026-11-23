using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StpSDK;

public delegate void StpMessageReceivedDelegate(string jsonMessage);
// 3rd parameter typed as StpCommunicationException (rather than Exception) for source
// compatibility with prior SDK versions; StpCommunicationException derives from Exception,
// so handlers declared with either type bind via method-group contravariance.
public delegate void StpConnectionErrorDelegate(string message, bool stpDisabled, StpCommunicationException exception);

public interface IStpConnector : IDisposable
{
    event StpMessageReceivedDelegate OnMessage;
    event StpConnectionErrorDelegate OnConnectionError;

    Task<bool> ConnectAsync(string url, int secondsToRetry = 0, CancellationToken ct = default);
    Task<string> RegisterAsync(string serviceName, List<string> solvables, string machineId = null, string sessionId = null, CancellationToken ct = default);
    void Disconnect();
    bool Connected { get; }

    void Send(string jsonMessage);
    Task<string> SendRequestAsync(string jsonMessage, int cookie, int timeoutMs = 30000, CancellationToken ct = default);

    string Name { get; }
    string BaseName { get; }
    ILogger Logger { get; }
}

/// <summary>
/// Optional connector capabilities added with the 5.12 contract (0.9.11). A separate interface so
/// that existing <see cref="IStpConnector"/> implementations keep compiling; <see cref="StpJsonRpcConnector"/>
/// implements it. <see cref="StpRecognizer"/> uses it when the connector offers it.
/// </summary>
public interface IStpExtendedConnector : IStpConnector
{
    /// <summary>
    /// <c>Register</c> with the contract's optional <c>user</c> param (STP-1260): the person using this
    /// client, as a display name. Null or empty omits it.
    /// </summary>
    Task<string> RegisterAsync(string serviceName, List<string> solvables, string machineId, string sessionId, string user, CancellationToken ct = default);

    /// <summary>
    /// A <c>Request</c> whose engine-side timeout (<c>timeout</c>, whole seconds; 0 = none) differs from
    /// how long the client waits for the answer.
    /// </summary>
    Task<string> SendRequestAsync(string jsonMessage, int cookie, int engineTimeoutSeconds, int clientTimeoutMs, CancellationToken ct = default);
}
