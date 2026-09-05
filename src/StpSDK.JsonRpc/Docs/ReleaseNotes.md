# Sketch-Thru-Plan .NET SDK Release Notes

The Sketch-Thru-Plan (STP) .NET SDK is published to NuGet as
`HyssosTech.Sdk.STP`. This document is the unified release notes and changelog
for the SDK; notable changes to the accompanying samples, quickstart, and
plugins are folded in under the relevant versions.

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
