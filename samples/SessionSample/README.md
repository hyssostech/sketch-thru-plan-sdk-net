# Session Sample Overview

This sample extends the [Roles sample](../RolesSample), adding the choice of the STP session to join:

* Registering with a session id, or letting STP assign one
* Showing the session this client joined, so other clients can join the same one

Only the aspects particular to this sample are described here.
Details shared by all samples are described in the [main samples page](../README.md).

## STP sessions

An STP engine can serve more than one session. Clients that register with the same session id share one
scenario: they see each other's symbols, tasks, task orgs and COAs as they are created, and each client's
events reach the others. A client that registers without a session id joins the engine's default session.

## Choosing the session

The toolbar has a `Session` box next to the connection string. When `Connect` is selected, its content is
passed as the `session` argument of `ConnectAndRegisterAsync`; left blank, no session id is sent
([Form1.cs](./Form1.cs), `Connect()`):

```csharp
string sessionId = string.IsNullOrWhiteSpace(toolStripTextBoxSessionId.Text) ? null : toolStripTextBoxSessionId.Text.Trim();
session = await _stpRecognizer.ConnectAndRegisterAsync("SessionSample", session: sessionId);
```

`ConnectAndRegisterAsync` returns the session the client joined, which the sample writes back into the
`Session` box. Copy it into a second instance of the sample (or any other STP client) to have both work on
the same scenario.

After registering, the sample offers to join the session's current scenario or start a new one, as the
[Scenario sample](../ScenarioSample) describes.

## Running the sample

Build and run it as described in the [main samples page](../README.md#running-the-samples). Then:

1. Start the sample with the `Session` box blank and select `Connect`; note the session shown
2. Start a second instance, enter the same session and select `Connect`; choose to join the current scenario
3. Sketch and speak in either instance: both show the new symbols
