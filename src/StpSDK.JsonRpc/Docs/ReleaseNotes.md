# Sketch-Thru-Plan .NET SDK Release Notes

The Sketch-Thru-Plan (STP) .NET SDK is published to NuGet as
`HyssosTech.Sdk.STP`. This document is the unified release notes and changelog
for the SDK; notable changes to the accompanying samples, quickstart, and
plugins are folded in under the relevant versions.

## Version 0.6.0

### Summary

**Moves the modern target framework to `net10.0` and fixes two defects that made
events disappear without a trace.** Published first as `0.6.0-rc.1` so the
release path could be exercised without burning the stable version number; NuGet
treats the two as distinct versions, so the candidate does not consume `0.6.0`.

- **BREAKING (platform):** `net8.0` -> `net10.0`. `netstandard2.0` is untouched.
- Unknown enum values from a newer engine dropped the entire event instead of
  degrading.
- `ConnectAsync` honoured neither its cancellation token nor its retry bound.
- Handlers that discard a malformed engine message now report it.
- New: `StpSymbol.CompositeSvg(width, height)`, layer composition without an
  imaging library.

### Notes

**The framework move.** .NET 8 reaches end of support on 2026-11-10, so the
modern leg moves to `net10.0`. `netstandard2.0` is deliberately retained and is
the reason a .NET Framework consumer is unaffected - the `DotNetFrameworkSample`
in this repository exists to keep that leg honest and now builds in CI, with an
assertion that it really consumed the netstandard2.0 assembly rather than
silently resolving the modern one. A consumer still on .NET 8 does not break: it
resolves the netstandard2.0 asset instead of the `net8.0` one.

**Why events vanished on an unknown enum value.** The engine serialises enums
with `.ToString()`, so an engine newer than this SDK sends member names these
enums do not contain. `NullSafeStringEnumConverter` exists precisely so that
degrades gracefully - and it returned `null`, including for **non-nullable**
properties such as `StpTask.What`. Newtonsoft.Json 13.0.3 tolerated the null and
left the property at its default. 13.0.4 rejects it: the containing object fails
to deserialise, and because `HandleTaskAdded` returns early on an empty
alternates list, the entire event was dropped in silence. The converter now
returns a value the target type can hold - the enum's default for a non-nullable
property, `null` only where `Nullable<TEnum>` makes null legal. That is what
13.0.3 effectively produced, so behaviour is unchanged: the full suite passes
under both 13.0.3 and 13.0.4 with the fix in place.

**Why a connect to an unreachable engine never returned.** `ConnectAsync`
accepted a `CancellationToken` and a `secondsToRetry` and honoured neither. It
created a linked token source, called `CancelAfter` on it, disposed it, and
never passed it to anything - both branches of the `if` were the same statement.
With `IsReconnectionEnabled` set, `Start()` retries indefinitely, so the caller
blocked forever with no error and no diagnostic; measured before the fix, a
30-second token was still blocked when the process was killed at 240 seconds.
Both paths are now bounded, including the token-only path that never was. On
cancellation the client is stopped first, mirroring `Disconnect()`, so a
timed-out connect does not leave a reconnect loop running behind it.

**Discarded messages are now observable.** 35 dispatch handlers, 22 of which
return early when a payload is not what they expect, and not one of them said
so. An exception inside a handler at least reaches `OnStpMessage(Error)`; a drop
reached nothing. Each site now reports which field was missing, at **Warning**
level rather than Error - version skew between an engine and an SDK is an
expected condition in a distributed deployment, and reporting it as an error
would get the channel ignored:

    TaskAdded: discarded an engine message - alternates was missing or empty.
    SymbolModified: discarded an engine message - poid was missing or symbol was missing.

No behaviour changed; every guard still returns exactly as before.

**`CompositeSvg`.** Symbol rendering here is layer compositing, not drawing:
`Symbol.Bitmap()` stacks a frame, an entity icon, modifiers, echelon, status and
HQ/TF marks onto one surface. Stacking SVG onto SVG is pure XML, so the
operation that pins this SDK to Windows needs `System.Drawing` only for the
final rasterisation. `StpSymbol.CompositeSvg(width, height)` emits one SVG
document from the same ordered layer list `Bitmap()` walks. `Bitmap()` is
untouched and still ships.

Verified against the real graphic set (4554 graphics) at 32/64/128/256/512px:
400 real symbols at every size gave 2000/2000 identical under svg-net and
2000/2000 under Skia; across every graphic individually, 22730 of 22770
comparisons were identical, the remaining 40 differing by 96 pixels in total
with a maximum alpha delta of 17. That residual is sub-pixel antialiasing on
hairline strokes: it is characterised, not root-caused, and two hypotheses for
it - decimal precision and an extra viewBox matrix - were tested and falsified.

**Dependencies.** `System.Drawing.Common` was a floating `5.*` range, which
resolves differently over time with no commit and no review. Every dependency is
now pinned exactly, centrally, in `Directory.Packages.props`. Several majors
advanced in the process, and these flow to consumers transitively:

| package | 0.5.0 | 0.6.0 |
| --- | --- | --- |
| `Websocket.Client` | 4.7.0 | 5.5.0 |
| `DynamicData` | 8.4.1 | 9.4.33 |
| `Newtonsoft.Json` | 13.0.3 | 13.0.4 |
| `System.Drawing.Common` | `5.*` | 10.0.12 |
| `Microsoft.Extensions.Logging` | 7.0.0 | 10.0.12 |
| `System.Threading.Tasks.Dataflow` | 6.0.0 | 10.0.12 |
| `System.Resources.Extensions` | 6.0.0 | 10.0.12 |
| `Svg` | 3.4.7 | 3.4.8 |

### Also in this release

- Samples: the plugin path was spelled `Plugins/Mapping` in 9 sample projects
  while the tracked path is `plugins/Mapping`. Both CI legs that matter run on
  Linux, where that does not resolve - the samples could not build there at all.
- Samples: 23 `CancellationTokenSource` instances were created and never
  disposed.
- Samples, quickstart and plugins joined `StpSDK.sln`, putting 64 first-party
  files in front of the build and the static analyser for the first time.
- Release engineering: coverage is now actually collected (the collector had
  been referenced but never invoked), the publish path no longer holds a
  credential and runs third-party install code in the same job, Sonar
  suppressions must carry a justification and a review date, and the tag a
  release is cut from is asserted against the version being packed before
  anything reaches nuget.org.
- The published API documentation had been empty since the framework move:
  `docs/docfx.json` still pinned `net8.0`, a target that no longer existed, so
  docfx resolved no package assets and emitted no API pages. Reproduced with the
  same docfx the workflow installs - `net8.0` gave 21 errors and 0 generated
  files, `net10.0` gives 0 errors and 138. A CI check now asserts the TFM docfx
  pins is one the SDK actually targets, because `docs.yml` only runs on `main`
  and a pull request could not have caught it.

**`LatLon` equality, stated deliberately.** `LatLon.Equals` compares `Lat` and
`Lon` exactly, and that is a decision rather than an oversight. An epsilon
comparison inside `Equals` breaks two invariants the runtime depends on: it is
not transitive, and it cannot agree with `GetHashCode`, which derives from the
exact bits. Under a tolerant `Equals`, every `HashSet<LatLon>`,
`Dictionary<LatLon,_>`, `Distinct()` and `GroupBy()` silently disagrees with
`Equals`, because those consult the hash first and never compare the pair at
all. A caller who needs "close enough" should compare with a tolerance suited to
their own use; the SDK cannot choose that number for them.

## Version 0.5.0

### Summary

**First stable version of `HyssosTech.Sdk.STP` in the JSON-RPC lineage** - the
`-preview` suffix is dropped. Published first as `0.5.0-rc.1` so the release
path could be exercised without burning the stable version number; NuGet treats
the two as distinct versions, so the candidate does not consume `0.5.0`.
Collects five merged fixes, none of which had ever been published: three of them
made calls fail or events vanish with no error.

- Three `ObjectSet` getters always threw; they now accept what the engine sends.
- `OnSpeechParsed` never fired, because the SDK read the wrong wire field.
- Refusals arrived as exceptions with an EMPTY message.
- `ConfirmTask`, `SendSimulatedSpeechRecognition` and `ConvertC2SIMContentAsync`
  close the dispatched wire-surface gap with the JS SDK.
- **BREAKING (behavior):** `SendSimulatedSpeechRecognition` now sends the
  engine's own method instead of a client-side stand-in.

### Notes

**Why three getters always threw.** `GetScenarioObjectSetContentAsync`,
`GetTaskOrgObjectSetAsync` and `GetCoaObjectSetAsync` deserialised an
`ObjectSet`, but the engine answers with a bare array of objects, not
`{"objects":[...]}`. The unit-test oracle had encoded the wrong shape, so the
suite stayed green while every live call failed. Both shapes are now accepted.

**Why speech events vanished.** `HandleSpeechParsed` read a `parsedAlternates`
field. The bridge sends `SendEvent("SpeechParsed", new { alternates = ... })`,
so the field never matched and `OnSpeechParsed` never fired for anyone.

**Why refusals looked empty.** STP answers a method it cannot dispatch with
`success:false` and a null result. That arrives as a `JToken` of type Null, not
a C# null, so the `?? "Request failed"` fallback never fired and `ToString()`
returned the empty string. Every refusal surfaced as an `StpException` with no
message, which is how a family of undispatched methods stayed invisible.

**The simulated-speech change.** `SendSimulatedSpeechRecognition(string,
DateTime?)` previously called this SDK's `ConvertToTranscription` - which,
despite the name, passes text through verbatim - and sent the result as a single
`SendSpeechRecognition` item. Typed input therefore reached STP unconverted
while the JS SDK got the phonetic form: `"A 3 1"` stayed `"A 3 1"` instead of
becoming `"alpha three one"`. It now sends the engine's dedicated
`SendSimulatedSpeechRecognition` method, which does the conversion server-side.
`ConvertToTranscription` remains public but is no longer used by any path here.

**Deprecation.** `SwitchTaskConfirmationAsync` is marked `[Obsolete]`: its wire
method has no case arm in the engine's dispatcher on any release line and
silently does nothing. Use `ConfirmTask`.

### Also in this release

- Samples and plugins build in Release again; the StpSDK reference had been left
  in a Debug-only `ItemGroup`, so no sample project could be packed or shipped.

## Version 0.4.2-preview

### Summary

**Fixes silent data loss.** Symbology values the engine sends were being
discarded by this SDK without any error, because its enum member names had
drifted from the engine's. Anyone on 0.4.1-preview or earlier is affected.

- **BREAKING (source):** several enum members are renamed to match the wire.
- Six enums realigned with the engine; one previously missing member added.

### Notes

**Why values were disappearing**

The engine serialises symbology enums with `.ToString()`, so the C# member NAME
is the wire contract. The engine renamed members on 2026-07-30/31 to match its
authored symbol tables; this SDK did not follow. Because
`StpSymbol.Affiliation` and friends use `NullSafeStringEnumConverter`, whose
`ReadJson` is `try { ... } catch { return null; }`, an unrecognised name was
swallowed and became `null` - no exception, no log entry. Affected symbols
simply arrived with no affiliation, echelon or modifier.

**What changed**

| enum | was | now |
|---|---|---|
| `Affiliation` | `assumedfriend`, `suspected` | `assumed_friend`, `suspect` |
| `Echelon` | `armygroup` | `army_group` |
| `Modifier` | `dummy`, `dummy_hq`, `dummy_task_force`, `dummytask_force_hq` | `feint_dummy`, `feint_dummy_hq`, `feint_dummy_task_force`, `feint_dummy_task_force_hq` |
| `Modifier` | - | `installation` added (was missing entirely) |

A sweep of all 16 symbology enums against the engine found three more that had
drifted the same way:

| enum | members | added |
|---|---|---|
| `TaskWhat` | 79 -> 90 | `CANALIZE`, `CONTAIN`, `CONTROL`, `COUNTERRECONNAISSANCE`, `DEMONSTRATING`, `DISENGAGE`, `EXFILTRATE`, `INTERDICT`, `ISOLATE`, `REDUCE`, `SUPPRESS` |
| `Branch` | 11 -> 15 | `non_military_sea`, `non_submarine_subsurface`, `sof_naval`, `sof_support` |
| `CodingScheme` | 9 -> 10 | `mapping` |

`TaskWhat` is the most consequential: a task carrying any of those eleven types
arrived with no task type at all.

**Upgrading**

If you reference the old member names you will get compile errors - rename them
as per the table. The exercise and simulation variants
(`exerciseassumedfriend`, `exercisesuspected`, ...) are deliberately UNCHANGED;
the engine kept the old spellings for those.

**Verification**

Regression tests pin every renamed member's wire spelling in both directions,
and a live test against a running engine sends no affiliation at all and lets
the engine derive it - so the value asserted is the engine's own spelling
rather than an echo. 342 unit tests and 8 live tests pass.

### Also in this release

- Docs workflow no longer triggers on tags; the `github-pages` environment only
  permits deployments from the `main` branch, so tag runs could never deploy.
- README corrections: the package targets .NET 8 and .NET Standard 2.0 (it said
  .NET 6), and an upgrade note explains that `0.3.x` was the OAA SDK while
  `0.4.x` is this JSON-RPC client.

## Version 0.4.1-preview

### Summary

This build supports:

- **Connection fix**: corrected machine-id and session handling in the connector - two regressions from the OAA-to-JSON-RPC port that broke registration.

### Notes

**Machine id computed correctly**

The connector derived the machine id from the *first* network adapter's MAC, but that
adapter is frequently a loopback/virtual one with no MAC - yielding an empty id and an
empty session, so registration failed. It now computes a stable machine id the same way
the STP engine does (`Auth.GetMachineID`: the highest non-empty NIC MAC, formatted
`XX-XX-...`, with a host-name fallback), so .NET clients on the same host - OAA or
JSON-RPC - share the default per-machine session. (A browser can't read a MAC, so the
JS SDK uses a random id; .NET can and does compute a real one.)

**STP-assigned session is authoritative**

Registration previously discarded the session id returned by STP and returned its own
local value. It now returns the session id from the Register response (STP may
assign/normalize a default), matching the JS SDK. Pass an explicit `sessionId` to
`ConnectAndRegisterAsync` to join a specific session.

### Changelog

+ Fixes
	- Connector: compute a stable machine id like the engine (highest non-empty NIC MAC; host-name fallback) instead of the first adapter's MAC - fixes empty-session registration failures
	- Connector: use the STP-assigned session id from the Register response (it was being discarded)
+ Improvements
	- Added live parity smoke tests (structured SIDC, JMSML rendering, add/update/delete lifecycle) exercised against a running engine

## Version 0.4.0-preview

### Summary

This build supports:

- **JSON-RPC transport**: the SDK now communicates with the STP engine over a
  WebSocket JSON-RPC interface, replacing the legacy OAA / Prolog agent protocol
- **New home**: the SDK source now lives in, and is published from, the public
  `sketch-thru-plan-sdk-net` repository as the canonical `HyssosTech.Sdk.STP`
  package
- **Drop-in for existing consumers**: the public namespace and assembly remain
  `StpSDK`; the temporary `StpSDK.JsonRpc` identity used while the JSON-RPC SDK
  matured alongside the OAA SDK has been removed
- **Vendored JMSML**: the Joint Military Symbology Library is vendored in-repo
  with full Apache-2.0 attribution
- **Symbol rendering**: `StpSymbol.Bitmap()` is wired to JMSML

### Notes

**JSON-RPC transport**

The connection to STP is now a WebSocket JSON-RPC channel
(`StpJsonRpcConnector`, default `ws://localhost:9599`) instead of the native
OAA/Prolog agent socket. The SDK no longer depends on the Prolog
feature-structure or OAA communication libraries; symbol data is exchanged as
plain JSON. The public `StpRecognizer` surface - connection, the symbol / task /
task-org / COA / speech / sketch events, commands, and metadata - is preserved.

**Drop-in for existing consumers**

The package id (`HyssosTech.Sdk.STP`), the assembly (`StpSDK.dll`), and the root
namespace (`StpSDK`) are unchanged, so applications that referenced the package
and used `using StpSDK;` compile without source changes. During development the
JSON-RPC SDK temporarily used the `StpSDK.JsonRpc` namespace so it could coexist
with the OAA SDK in the internal repository; that suffix has now been removed.

**Vendored JMSML**

The Joint Military Symbology Library (derived from Esri's open-source
`joint-military-symbology-xml`, Copyright 2014-2015 Esri, Apache License 2.0) is
vendored under `third-party/JMSML/`, with the upstream license and a NOTICE
documenting the STP modifications. Runtime symbol data (`JMS/jmsmlSTP.config`
and the `JMS/Instance` symbol set) ships inside the package.

**Symbol rendering**

`StpSymbol.Bitmap(width, height)` now resolves the symbol's SIDC through JMSML
and renders SVG-based 2525 symbology. Set `StpRecognizer.JMSSVGPath` to the SVG
graphic set before rendering. Rendering uses `System.Drawing` and is effectively
Windows-only; on other platforms (or when the SVG set is unavailable) `Bitmap`
returns `null` and hosts fall back to a default rendering.

### Changelog

+ Improvements
	- JSON-RPC over WebSocket transport (`StpJsonRpcConnector`); OAA/Prolog connector retired
	- SDK source relocated into the public `sketch-thru-plan-sdk-net` repository and published from there
	- Namespace and assembly normalized back to `StpSDK`; existing `HyssosTech.Sdk.STP` consumers need no source changes
	- JMSML vendored under `third-party/JMSML` (Esri, Apache-2.0) with `LICENSE` and `NOTICE`
	- Symbol rendering wired: `StpSymbol.Bitmap` resolves the SIDC via JMSML and renders SVG symbology
	- Samples, quickstart, and plugins now reference the in-repo SDK project directly (no cross-repo paths or stale package references)
	- Full sample set carried over and aligned with the JavaScript SDK: Editing, Tasking, Scenario, Reactive, Speech, Roles, Session, TaskOrg, C2SIM, and a .NET Framework compatibility sample, plus the SimpleMap and Azure Speech plugins
	- API documentation builds with DocFX from the new repository and publishes to GitHub Pages
+ Fixes
	- Reduced JMSML conversion-status log noise: only fatal unresolved fields (status bits 0-11) are reported, suppressing benign "Not Found" diagnostics for entity sub-types and modifiers

## Version 0.3.9-preview

### Summary

This build supports:

- Expanded activity-signatures task table with doctrinal descriptions and new tasks

### Changelog

+ Improvements
	- AS_Table_compact: added a Description column (FM 3-90 doctrinal text) and 8 new tasks (SuppressEnemy, SuppressEnemyOnObjective, AttackByFireToSuppress, AttackInZoneToIsolate, ContainEnemy, AttackByFireToContain, CanalizeEnemy, InterdictEnemy)
	- StpTaskFactory: added a Description property to ASCompactTaskDefinition, populated from the table
+ Fixes
	- Stripped an orphan "1" suffix from ConductAMovingFlankScreen, ConductAMovingFlankScreenToSecure, and UnmannedAerialSystemIsrOrderableActivity entries

## Version 0.3.8-preview

### Summary

This build supports:

- COA lifecycle API, richer symbol designation properties, and safer enum deserialization

### Changelog

+ Improvements
	- StpSymbol: added `SymbolDesignation`, `Abbr`, and `ShortForm` properties
	- Added `UnitDesignationGenerator` for doctrinal short-form designations
	- COA: lifecycle events and command API
	- MilTypes: `NullSafeStringEnumConverter` for safer JSON deserialization of enums
	- nuget.config: forward-slash paths for cross-platform (Linux/macOS) restore
+ Fixes
	- StpOaaConnector: removed an erroneous `auth:` prefix on new_scenario messages (the single parameter is a full Auth feature structure)

## Version 0.3.4

### Changelog

+ Improvements
	- Added `ExternalId` to StpSymbol to retain 3rd-party system ids through import/export
	- Added a TaskFactory constructor that takes the root path of the task metadata tables
+ Fixes
	- Fixed delivery of MOOTW symbol update notifications

## Version 0.3.3

### Changelog

+ Improvements
	- Added `battery` and `troop` as `company` echelon alternative names
	- Added task description as a task creation parameter
	- Now building .NET 6, .NET 8, and .NET Standard 2.0
+ Fixes
	- Unwrapped nullables to recover the underlying type during deserialization
	- Fixed delivery of MOOTW symbol update notifications

## Version 0.3.2

### Changelog

+ Improvements
	- Expanded SIDC parsing for additional coding schemes
	- Extended the API to operate on STP object sets directly, not just serialized versions
	- Added tests

## Version 0.3.1

### Changelog

+ Improvements
	- Enhanced SIDC parsing with initial 2525D handling
	- Task factory class driven by the STP recognizer's metadata
	- Task creation validation

## Version 0.3.0

### Summary

This build supports:

- Extensive updates to align with STP v5.9: sessions, scenario synchronization, TO/ORBAT, role switching, and C2SIM

### Changelog

+ Improvements
	- Ability to connect to a Session
	- Scenario synchronization - reconciles application content with a Session context
	- TO/ORBAT import / export / selection API; TO Service fixes; `SpeechPhrases` TO-unit property
	- Tasks: ability to toggle task confirmation and to enable/disable Auto Tasking
	- Role switching API
	- C2SIM: configuration of parameters (server endpoints, generation options), API to generate Orders and Initialization and to retrieve Initialization from a server, additional symbol properties (DIS code, federate, resource), and a rules-of-engagement task property

## Version 0.2.6

### Changelog

+ Improvements
	- Added support for connections over WebSockets
	- Added a standalone app sample that embeds a cloud speech transcription service
	- Cleaned up speech-related interfaces
	- Reactive Extensions (Rx) sample documentation

## Version 0.2.5

### Changelog

+ Improvements
	- Removed the hard NLog dependency from the JMSML library (updated the open-source code to use ILogger instead)
	- Added Reactive Extensions (Rx) capabilities (preview)

## Version 0.2.4

### Changelog

+ Improvements
	- Improvements to globalization

## Version 0.2.3

### Changelog

+ Improvements
	- Added higher-level scenario management methods
	- Improved long-duration operation handling

## Version 0.2.2

### Changelog

+ Fixes
	- Fixed assemblies that were packed incorrectly in the previous version

## Version 0.2.1

### Changelog

+ Improvements
	- Exposed additional audio and sketch events for finer-grained user feedback
	- Renamed some methods and changed parameter types for consistency and clarity
	- Added JMSML SVG artifacts to support simple symbol rendering

## Version 0.2.0

### Changelog

+ Improvements
	- Added Task Org / ORBAT handling
	- Improved documentation, fixed XML doc tags
+ Fixes
	- Fixed occasional failure to detect a failed connection (STP not running)

## Version 0.1.1

### Changelog

+ Fixes
	- Fixed documentation links

## Version 0.1.0

### Summary

- Initial public release
