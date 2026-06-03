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
