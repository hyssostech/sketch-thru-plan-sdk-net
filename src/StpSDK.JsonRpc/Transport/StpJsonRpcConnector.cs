using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
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

            // STP-763. Both bounding parameters used to be accepted and
            // IGNORED: a linked CTS was created, given a deadline, disposed -
            // and never passed to anything, leaving the two branches identical.
            // With IsReconnectionEnabled set, Start() retries indefinitely, so
            // an unreachable engine hung the caller forever with no error and
            // no diagnostic. Measured: a 30s token was still blocked at 240s.
            using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                if (secondsToRetry > 0)
                {
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(secondsToRetry));
                }

                await StartBoundedAsync(_ws, timeoutCts.Token).ConfigureAwait(false);
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
        _machineId = machineId ?? GetDefaultMachineId();
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

    /// <summary>
    /// Await the websocket client's start, abandoning it if <paramref name="token"/> fires.
    /// </summary>
    /// <remarks>
    /// Task.WaitAsync(CancellationToken) is .NET 6+ and this package also targets
    /// netstandard2.0, so the race is written out by hand rather than taken from
    /// the BCL. On cancellation the client is told to stop retrying - mirroring
    /// Disconnect() - so a timed-out connect does not leave a reconnect loop
    /// running in the background, which would be a worse failure than the hang.
    /// </remarks>
    private static async Task StartBoundedAsync(WebsocketClient ws, CancellationToken token)
    {
        Task start = ws.Start();

        if (!token.CanBeCanceled)
        {
            await start.ConfigureAwait(false);
            return;
        }

        var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (token.Register(() => cancelled.TrySetResult(true)))
        {
            if (await Task.WhenAny(start, cancelled.Task).ConfigureAwait(false) != start)
            {
                ws.IsReconnectionEnabled = false;
                // Fire and forget, deliberately: we are abandoning a connect that has
                // ALREADY exceeded its bound, so blocking on the stop of a socket that
                // never opened would reintroduce the hang this method exists to prevent.
                _ = ws.Stop(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Connect timed out");
                throw new OperationCanceledException(
                    "Timed out or cancelled while connecting to STP.", token);
            }
        }

        // Completed rather than cancelled - observe any fault it carries.
        await start.ConfigureAwait(false);
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

    /// <summary>
    /// Text for a refused request. The engine sends <c>result: null</c> when it cannot dispatch a
    /// method, and that arrives as a JToken of type Null - NOT as a C# null - so a
    /// <c>?? "Request failed"</c> fallback never fires and <c>ToString()</c> on it yields the
    /// EMPTY string. Every refusal used to surface as an exception with no message at all, which
    /// is how a whole family of methods the engine does not dispatch stayed invisible. Engines
    /// from 2026-09 onward put the reason in the result; older ones send nothing, so say what is
    /// known rather than nothing.
    /// </summary>
    internal static string RefusalMessage(JToken result)
    {
        string text = (result == null || result.Type == JTokenType.Null) ? null : result.ToString();
        return string.IsNullOrWhiteSpace(text)
            ? "STP refused the request and gave no reason. The usual cause is a method this engine does not dispatch; check the engine log for 'No handler for method'."
            : text;
    }

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
                        tcs.TrySetException(new StpException(RefusalMessage(responseParams.Result), null));
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

    private static string _cachedMachineId;

    /// <summary>
    /// Stable machine identifier used to key the default per-machine session. Mirrors the
    /// STP engine's <c>Auth.GetMachineID()</c> so .NET clients on the same host (OAA or
    /// JSON-RPC) agree on the session: the highest non-empty NIC MAC, formatted as
    /// upper-case <c>XX-XX-...</c>, falling back to the host name. (Unlike the JS SDK, which
    /// can't read a MAC in the browser sandbox and uses a random id, .NET can and should.)
    /// </summary>
    private static string GetDefaultMachineId()
    {
        if (_cachedMachineId is not null)
            return _cachedMachineId;
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                byte[] bytes = nic.GetPhysicalAddress()?.GetAddressBytes();
                if (bytes is null || bytes.Length == 0)        // skip loopback/virtual adapters with no MAC
                    continue;
                var sb = new StringBuilder(bytes.Length * 3);
                for (int i = 0; i < bytes.Length; i++)
                    sb.AppendFormat("{0:X2}{1}", bytes[i], i < bytes.Length - 1 ? "-" : "");
                string mac = sb.ToString();
                if (mac.CompareTo(_cachedMachineId) > 0)       // largest MAC wins (stable across adapters)
                    _cachedMachineId = mac;
            }
            if (string.IsNullOrEmpty(_cachedMachineId))
                _cachedMachineId = Dns.GetHostName().ToUpperInvariant();
        }
        catch
        {
            _cachedMachineId = Dns.GetHostName().ToUpperInvariant();
        }
        return _cachedMachineId;
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
