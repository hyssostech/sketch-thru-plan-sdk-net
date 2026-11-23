# C2SIM Sample Overview

This sample extends the [Session sample](../SessionSample), adding C2SIM interchange:

* Generating C2SIM documents (Initialization or Order) from the scenario
* Pushing a generated document to a C2SIM server
* Pulling the C2SIM Initialization from a C2SIM server, converting it and importing it
* Following the symbol reports relayed from the C2SIM server
* Setting the C2SIM options as typed properties, among them `RefuseUnconfirmedPlaceholders`

Only the aspects particular to this sample are described here.
Details shared by all samples are described in the [main samples page](../README.md).

## C2SIM

C2SIM (Command and Control to Simulation Interoperability, SISO-STD-019) is the standard for exchanging
orders, reports and the initial state of a scenario between C2 systems and simulations. STP builds C2SIM
documents from the plan (units, task orgs, graphics and tasks) and talks to a C2SIM server for the client.

## C2SIM options

Every C2SIM call takes an options dictionary. `StpC2SIMOptions` is that dictionary with the contract's keys
as typed properties; a property left null is not sent, so the engine's configured value applies. The sample's
`C2SIM options` panel ([Form1.cs](./Form1.cs), `#region C2SIM options panel`) sets three of them:

```csharp
var options = new StpC2SIMOptions();
options.RestUrl = string.IsNullOrWhiteSpace(_textC2SimRestUrl.Text) ? null : _textC2SimRestUrl.Text.Trim();
options.RefuseUnconfirmedPlaceholders = _checkRefuseUnconfirmed.Checked;
options.FullTO = _checkFullTO.Checked ? true : null;
```

* `RestUrl` - the C2SIM server; blank uses the server configured in the engine
* `RefuseUnconfirmedPlaceholders` - STP can draw a task graphic from no evidence, as a placeholder, when a task
  needs one the user did not sketch. With this option, generating a document fails while such a placeholder has
  not been confirmed by a user (moved, or its confidence set), and the error names the graphics. Without it, each
  unconfirmed placeholder is only warned about in the engine log. It applies where the engine builds a document
  (`GetC2SIMContentAsync`); the other calls accept it and ignore it
* `FullTO` - include the full task org, not only the units placed on the map

## Generating and pushing documents

Choose the affiliation and the document type (Initialization or Order) in the C2SIM box and select `Export`:

```csharp
_lastC2SimContent = await _stpRecognizer.GetC2SIMContentAsync(
    "C2SIMSample", dataType, affiliation, null, options, cts.Token);
```

A refusal (for example an unconfirmed placeholder) arrives as an `StpException` and is shown in the log.
`Push last export` in the `C2SIM options` panel sends the document to the C2SIM server:

```csharp
await _stpRecognizer.PushC2SIMContentAsync(_lastC2SimContent, _lastC2SimDataType, C2SimOptions(), cts.Token);
```

## Pulling the Initialization

`Import` fetches the C2SIM Initialization from the C2SIM server. `PullC2SIMInitializationContentAsync` returns
the document (XML) as the contract specifies; nothing is loaded into the scenario. The older
`PullC2SIMInitializationAsync` is still available and returns the same document as `Content`, with a null
`ServerStatus`.

```csharp
string content = await _stpRecognizer.PullC2SIMInitializationContentAsync(C2SimOptions(), cts.Token);
```

The sample then converts the document to STP content and imports it. `ConvertC2SIMContentAsync` answers null
from engines that do not implement the conversion; that is not an error, and nothing is imported.

```csharp
string stpContent = await _stpRecognizer.ConvertC2SIMContentAsync(content, C2SimOptions(), cts.Token);
if (stpContent != null) await _stpRecognizer.ImportSTPDataAsync(stpContent, cts.Token);
```

## C2SIM reports

STP relays reports it receives from the C2SIM server about symbols (for example a unit's new position) as
`OnSymbolReport`, subscribed before connecting. The sample writes them to the log.

## Running the sample

Build and run it as described in the [main samples page](../README.md#running-the-samples). A C2SIM server is
needed for `Push` and `Import`; `Export` needs only the STP engine. Then:

1. Load the friendly task org (`Friend`) and create a task: place a unit, an axis of advance and an objective
2. Select `Export` with `friend` and `Initialization`; the log shows the size of the document
3. Check `Refuse unconfirmed placeholders` and export an `Order` for a task whose graphics STP drew for you:
   the export is refused and the log names the graphics
4. Enter the C2SIM server's REST URL and select `Push last export`, or `Import` to pull its Initialization
