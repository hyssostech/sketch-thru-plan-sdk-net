# Sketch-Thru-Plan .NET SDK Change Log

Brief per-version highlights. The full release notes - summaries, narrative
notes, and the detailed changelog - are the single source of truth in
[src/StpSDK.JsonRpc/Docs/ReleaseNotes.md](src/StpSDK.JsonRpc/Docs/ReleaseNotes.md),
which also feeds the NuGet package release notes.

## Unreleased
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
