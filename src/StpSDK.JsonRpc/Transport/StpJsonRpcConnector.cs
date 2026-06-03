using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Websocket.Client;

namespace StpSDK;

public class StpJsonRpcConnector : IStpConnector
{
    private WebsocketClient _ws;
    private readonly ILogger _logger;
    private int _nextCookie;
    private readonly ConcurrentDictionary<int, TaskCompletionSource<string>> _pendingRequests = new();
    private ActionBlock<string> _messageProcessor;
    private string _serviceName;
    private string _machineId;
    private string _sessionId;
    private bool _disposed;

    public event StpMessageReceivedDelegate OnMessage;
    public event StpConnectionErrorDelegate OnConnectionError;

    public bool Connected => _ws?.IsRunning == true;
    public string Name => $"{_serviceName}_{_machineId}";
    public string BaseName => _serviceName;
    public ILogger Logger => _logger;

    public string Url { get; }

    public StpJsonRpcConnector(ILogger logger = null, string url = "ws://localhost:9599")
    {
        _logger = logger;
        Url = url;
        _messageProcessor = new ActionBlock<string>(
            ProcessMessageAsync,
            new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 }
        );
    }

    public async Task<bool> ConnectAsync(string url, int secondsToRetry = 0, CancellationToken ct = default)
    {
        try
        {
            var uri = new Uri(url);
            _ws = new WebsocketClient(uri)
            {
                ReconnectTimeout = TimeSpan.FromSeconds(30),
                IsReconnectionEnabled = true
            };

            _ws.MessageReceived.Subscribe(msg =>
            {
                if (!string.IsNullOrEmpty(msg.Text))
                {
                    _messageProcessor.Post(msg.Text);
                }
            });

            _ws.DisconnectionHappened.Subscribe(info =>
            {
                if (info.Type == DisconnectionType.Error || info.Type == DisconnectionType.Lost)
                {
                    OnConnectionError?.Invoke(
                        $"WebSocket disconnected: {info.Type}",
                        info.Type == DisconnectionType.Error,
                        info.Exception as StpCommunicationException
                            ?? new StpCommunicationException($"WebSocket disconnected: {info.Type}", info.Exception));
                }
            });

            _ws.ReconnectionHappened.Subscribe(info =>
            {
                _logger?.LogInformation("Reconnected: {Type}", info.Type);
            });

            if (secondsToRetry > 0)
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(secondsToRetry));
                await _ws.Start().ConfigureAwait(false);
            }
            else
            {
                await _ws.Start().ConfigureAwait(false);
            }

            return Connected;
        }
        catch (Exception ex)
        {
            OnConnectionError?.Invoke($"Failed to connect: {ex.Message}", true,
                ex as StpCommunicationException ?? new StpCommunicationException($"Failed to connect: {ex.Message}", ex));
            return false;
        }
    }

    public async Task<string> RegisterAsync(string serviceName, List<string> solvables, string machineId = null, string sessionId = null, CancellationToken ct = default)
    {
        _serviceName = serviceName;
        _machineId = machineId ?? GetUniqueId();
        _sessionId = sessionId ?? _machineId;

        var registerMsg = new JsonRpcMessage
        {
            Method = "Register",
            Params = JToken.FromObject(new JsonRpcRegisterParams
            {
                ServiceName = serviceName,
                Language = "csharp",
                Solvables = solvables,
                MachineId = _machineId,
                SessionId = _sessionId
            })
        };

        int cookie = Interlocked.Increment(ref _nextCookie);
        string result = await SendRequestAsync(
            JsonConvert.SerializeObject(registerMsg),
            cookie,
            30000,
            ct).ConfigureAwait(false);

        // STP returns the session id it actually used (it may assign/normalize a default),
        // mirroring the JS SDK which treats the Register response as authoritative.
        if (!string.IsNullOrEmpty(result))
            _sessionId = result;
        return _sessionId;
    }

    public void Disconnect()
    {
        if (_ws != null)
        {
            _ws.IsReconnectionEnabled = false;
            _ws.Stop(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Client disconnecting");
            _ws.Dispose();
            _ws = null;
        }
        CancelPendingRequests();
    }

    public void Send(string jsonMessage)
    {
        if (!Connected)
            throw new InvalidOperationException("Not connected to STP Engine");
        _ws.Send(jsonMessage);
    }

    public async Task<string> SendRequestAsync(string jsonMessage, int cookie, int timeoutMs = 30000, CancellationToken ct = default)
    {
        if (!Connected)
            throw new InvalidOperationException("Not connected to STP Engine");

        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests[cookie] = tcs;

        var wrapper = new JsonRpcRequestWrapper
        {
            Params = new JsonRpcRequestParams
            {
                JsonRequest = jsonMessage,
                Cookie = cookie,
                Timeout = timeoutMs / 1000
            }
        };

        _ws.Send(JsonConvert.SerializeObject(wrapper));

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeoutMs);

        try
        {
            using (timeoutCts.Token.Register(() => tcs.TrySetCanceled()))
            {
                return await tcs.Task.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException($"Request timed out after {timeoutMs}ms");
        }
        finally
        {
            _pendingRequests.TryRemove(cookie, out _);
        }
    }

    public int GetNextCookie() => Interlocked.Increment(ref _nextCookie);

    private Task ProcessMessageAsync(string json)
    {
        try
        {
            var msg = JObject.Parse(json);
            string method = msg["method"]?.ToString();

            if (method == "RequestResponse")
            {
                var responseParams = msg["params"]?.ToObject<JsonRpcResponseParams>();
                if (responseParams != null && _pendingRequests.TryRemove(responseParams.Cookie, out var tcs))
                {
                    if (responseParams.Success)
                        tcs.TrySetResult(responseParams.Result?.ToString());
                    else
                        tcs.TrySetException(new StpException(
                            responseParams.Result?.ToString() ?? "Request failed", null));
                }
            }
            else
            {
                OnMessage?.Invoke(json);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error processing message");
        }
        return Task.CompletedTask;
    }

    private void CancelPendingRequests()
    {
        foreach (var kvp in _pendingRequests)
        {
            kvp.Value.TrySetCanceled();
        }
        _pendingRequests.Clear();
    }

    // The JSON-RPC SDK has no physical machine identity (unlike the OAA SDK, which keyed
    // per-machine sessions off a NIC MAC). Mirror the JS SDK: when no machineId is supplied
    // use a short random id; STP assigns/normalizes the session and returns it from Register.
    private static string GetUniqueId(int numChars = 9)
    {
        string id = Guid.NewGuid().ToString("N");
        return numChars > 0 && numChars < id.Length ? id.Substring(0, numChars) : id;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            Disconnect();
            _messageProcessor?.Complete();
        }
    }
}

public class StpException : Exception
{
    public StpException(string message, Exception inner) : base(message, inner) { }
}

public class StpCommunicationException : Exception
{
    public StpCommunicationException(string message) : base(message) { }
    public StpCommunicationException(string message, Exception inner) : base(message, inner) { }
}

public class StpConnectionException : StpException
{
    public StpConnectionException(string message, Exception inner) : base(message, inner) { }
}
