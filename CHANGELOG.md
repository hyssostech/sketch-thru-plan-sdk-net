# Sketch-Thru-Plan .NET SDK Change Log

Brief per-version highlights. The full release notes - summaries, narrative
notes, and the detailed changelog - are the single source of truth in
[src/StpSDK.JsonRpc/Docs/ReleaseNotes.md](src/StpSDK.JsonRpc/Docs/ReleaseNotes.md),
which also feeds the NuGet package release notes.

## 0.6.0

- **BREAKING (platform): the modern target framework moves from `net8.0` to
  `net10.0`.** .NET 8 leaves support on 2026-11-10. `netstandard2.0` is
  unchanged and still ships, so .NET Framework consumers are unaffected; a
  consumer pinned to .NET 8 will now resolve the netstandard2.0 asset
- **Fixes events being dropped entirely when the engine sends an enum value this
  build does not know.** `NullSafeStringEnumConverter` existed so an unknown
  member name would degrade to a default, but it returned `null` even for
  non-nullable properties such as `StpTask.What`. Newtonsoft.Json 13.0.3
  tolerated that; 13.0.4 rejects the whole object, and `TaskAdded` then vanished
  with no error at all. Engine and SDK version independently, so this is an
  ordinary deployment condition, not an edge case
- **Fixes `ConnectAsync` ignoring both its `CancellationToken` and its
  `secondsToRetry`.** It built a linked token source, gave it a deadline, and
  never passed it to anything - both branches were the same statement. With
  reconnection enabled, connecting to an unreachable engine hung the caller
  indefinitely: a 30-second token was measured still blocked at 240 seconds
- **A dispatch handler that discards an engine message now says so**, through
  the existing `OnStpMessage` channel at Warning level, naming the event and the
  missing field. 22 of the 35 handlers returned early on an unexpected payload
  and none of them reported it; that silence is why the enum defect above looked
  like the engine simply not sending the event
- **Added `StpSymbol.CompositeSvg(width, height)`** - symbol layer composition
  as SVG, with no imaging library involved. `Bitmap()` is unchanged and still
  ships. Verified against the 4554-graphic set at 32/64/128/256/512px:
  2000/2000 identical for 400 real symbols, and 22730 of 22770 identical across
  every graphic, the residual being sub-pixel antialiasing bounded at 96 pixels
  in total
- **`System.Drawing.Common` is no longer a floating `5.*` range**; it is pinned,
  as is every other dependency - see below
- Dependencies moved to Central Package Management, and several majors advanced:
  `Websocket.Client` 4.7.0 -> 5.5.0, `DynamicData` 8.4.1 -> 9.4.33,
  `Newtonsoft.Json` 13.0.3 -> 13.0.4, `System.Drawing.Common` `5.*` -> 10.0.12,
  `Microsoft.Extensions.Logging` 7.0.0 -> 10.0.12,
  `System.Threading.Tasks.Dataflow` and `System.Resources.Extensions` 6.0.0 ->
  10.0.12, `Svg` 3.4.7 -> 3.4.8. These flow to consumers transitively
- Samples: the plugin path was spelled `Plugins/Mapping` in 9 sample projects
  against a tracked path of `plugins/Mapping`, so none of them resolved on a
  case-sensitive filesystem; 23 undisposed `CancellationTokenSource` leaks fixed
- Samples, quickstart and plugins are now part of `StpSDK.sln`, so CI and static
  analysis can see 64 first-party files that were previously invisible to both

## 0.5.0
- **Fixes `OnSpeechParsed` never firing.** The handler read a `parsedAlternates` field, but the bridge sends the alternates under `alternates` (`StpJsonClient.cs`), so the event was silently dropped on every recognition
- Added `RefreshSubscriptionsAsync`, so a handler attached after connect is routed instead of staying silently unsubscribed; when it cannot refresh, it now names the real cause
- Samples: every sample and plugin project was unbuildable in Release - the StpSDK reference sat in a Debug-only `ItemGroup` - and the samples called a dead method instead of `ConfirmTask`
- **A refused request now carries a readable message.** STP answers a method it cannot dispatch with `success:false` and a null result; that arrives as a `JToken` of type Null, not a C# null, so the `?? "Request failed"` fallback never fired and `ToString()` on it returned the empty string. Every refusal surfaced as an `StpException` with an EMPTY message, which is how a family of undispatched methods stayed invisible. The engine text is used when present, otherwise an explanatory sentence pointing at the engine log
- **Fixes `GetScenarioObjectSetContentAsync`, `GetTaskOrgObjectSetAsync`, `GetCoaObjectSetAsync` always throwing**: the engine answers these with a bare array of objects, not `{"objects":[...]}`; the SDK now accepts both. The old unit-test oracle had encoded the wrong shape, so the suite was green while every live call failed
- Added `ConfirmTask`, `SendSimulatedSpeechRecognition`, `ConvertC2SIMContentAsync` to close the DISPATCHED wire-surface gap with the JS SDK (all three have engine dispatch arms; `ConvertC2SIMContent` currently returns null - the engine has not implemented conversion, so a null result does not mean failure)
- **BREAKING (behavior)**: `SendSimulatedSpeechRecognition(string, DateTime?)` now sends the engine's dedicated `SendSimulatedSpeechRecognition` wire method (server-side number/letter-to-word conversion), matching the JS SDK, instead of a client-side stand-in that wrapped the raw text as a single `SendSpeechRecognition` recoList item. The client-side stand-in called this SDK's own `ConvertToTranscription`, which despite its name passes text through verbatim, so typed input reached STP unconverted while the JS SDK got the phonetic form - `"A 3 1"` stayed `"A 3 1"` instead of becoming `"alpha three one"`. `ConvertToTranscription` stays public but is no longer used by any path here, and its doc now says plainly that it does not transcribe
- `SwitchTaskConfirmationAsync` marked `[Obsolete]`: its wire method `SwitchTaskConfirmation` has no case arm in the engine's dispatcher on any release line and silently does nothing; use the new `ConfirmTask` instead

## 0.4.2-preview
- **Fixes silent data loss**: symbology values sent by the engine were discarded without error because this SDK's enum member names had drifted from the engine's
- **BREAKING (source)**: `Affiliation.assumedfriend` -> `assumed_friend`, `suspected` -> `suspect`; `Echelon.armygroup` -> `army_group`; `Modifier.dummy*` -> `feint_dummy*`
- `Modifier.installation` added (was missing); `TaskWhat` 79 -> 90 members, `Branch` 11 -> 15, `CodingScheme` 9 -> 10
- Exercise/simulation affiliation variants deliberately unchanged - the engine kept those spellings
- Docs workflow no longer triggers on tags (the github-pages environment only allows the `main` branch)
- README: corrected .NET 6 -> .NET 8, added a 0.3.x -> 0.4.x upgrade note

## 0.4.1-preview
- Connector fix: compute a stable machine id like the STP engine (highest non-empty NIC MAC; host-name fallback) instead of the first adapter's MAC, which was often empty -> empty session -> failed registration
- Connector fix: use the STP-assigned session id from the Register response (was discarded)
- Added live parity smoke tests (structured SIDC, rendering, add/update/delete) against a running engine

## 0.4.0-preview
- Migrated to the JSON-RPC / WebSocket transport (replaces the OAA / Prolog protocol)
- SDK relocated to this repository and published as the canonical `HyssosTech.Sdk.STP` package
- Namespace and assembly normalized back to `StpSDK` - a drop-in for existing consumers
- JMSML vendored in-repo (Esri, Apache-2.0); `StpSymbol.Bitmap` symbol rendering wired
- Samples / quickstart / plugins reference the in-repo SDK; DocFX docs publish to GitHub Pages

## 0.3.9-preview and earlier
- See [ReleaseNotes.md](src/StpSDK.JsonRpc/Docs/ReleaseNotes.md) for the full 0.1.0 -> 0.3.9-preview history.
