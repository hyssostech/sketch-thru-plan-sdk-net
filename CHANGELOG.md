# Sketch-Thru-Plan .NET SDK Change Log

Brief per-version highlights. The full release notes - summaries, narrative
notes, and the detailed changelog - are the single source of truth in
[src/StpSDK.JsonRpc/Docs/ReleaseNotes.md](src/StpSDK.JsonRpc/Docs/ReleaseNotes.md),
which also feeds the NuGet package release notes.

## 0.4.1-preview
- Connector fix: dropped the MAC-based machine id (OAA carryover that yielded an empty session on machines whose first NIC has no MAC); use a random id and STP's Register-assigned session, matching the JS SDK
- Added live parity smoke tests (structured SIDC, rendering, add/update/delete) against a running engine

## 0.4.0-preview
- Migrated to the JSON-RPC / WebSocket transport (replaces the OAA / Prolog protocol)
- SDK relocated to this repository and published as the canonical `HyssosTech.Sdk.STP` package
- Namespace and assembly normalized back to `StpSDK` - a drop-in for existing consumers
- JMSML vendored in-repo (Esri, Apache-2.0); `StpSymbol.Bitmap` symbol rendering wired
- Samples / quickstart / plugins reference the in-repo SDK; DocFX docs publish to GitHub Pages

## 0.3.9-preview and earlier
- See [ReleaseNotes.md](src/StpSDK.JsonRpc/Docs/ReleaseNotes.md) for the full 0.1.0 -> 0.3.9-preview history.
