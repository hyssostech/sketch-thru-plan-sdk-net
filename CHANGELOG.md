# Sketch-Thru-Plan .NET SDK Change Log

Brief per-version highlights. The full release notes - summaries, narrative
notes, and the detailed changelog - are the single source of truth in
[src/StpSDK.JsonRpc/Docs/ReleaseNotes.md](src/StpSDK.JsonRpc/Docs/ReleaseNotes.md),
which also feeds the NuGet package release notes.

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
