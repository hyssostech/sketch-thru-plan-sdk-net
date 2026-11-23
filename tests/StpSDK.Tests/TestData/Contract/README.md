# Vendored public contract (pinned)

`sketch-thru-plan-api.json` is a byte-for-byte copy of the public OpenRPC contract, pinned so the
SDK's coverage test (`ContractCoverageTests`) checks against a known version.

| | |
|---|---|
| Source repository | `hyssostech/sketch-thru-plan-sdk-js` |
| Commit | `b7a490d00195fb2c8ed4a29e965938a62f94a8e4` (release/5.12), plus the STP-1803 description-text edits below |
| Path in the source | `json-api/sketch-thru-plan-api.json` |
| Contract version (`info.version`) | `0.9.11` |
| SHA-256 of this copy (LF line endings) | `d58b82fa6918ed1aa6875d270614a2832f408b9b984ecb3a6a92c80078566b57` |

`ContractCoverageTests` checks the version and the SHA-256 above, so the copy cannot change by
accident. To move to a newer contract, copy the file from the new commit, update this table (commit,
version and hash) and the constants at the top of `ContractCoverageTests.cs`, then add any SDK member
the coverage test reports missing, or record the mapping in `docs/articles/contract-name-mapping.md`.

## STP-1803 description-text edits

Description text only; no method, param or event name changed, and `info.version` stays `0.9.11`.
The same edits are made to the source file in `hyssostech/sketch-thru-plan-sdk-js` release/5.12.

* "change sets" -> "batches" and "change set" -> "batch" (STP-1265 batches; the engine's MilTable
  "change set" is a different thing).
* "Limited in STP 5.12: symbols and tasks are not assigned to a COA, so planning by COA is not
  supported. " put at the start of the description of the methods `SetCoaTaskOrg`,
  `ResetCoaTaskOrg`, `ImportCoaContent`, `GetCoaContent`, `SetCurrentCoa`, `AddCoa`, `UpdateCoa`,
  `DeleteCoa`, `GetCoaObjectSet` and `ImportCoaFromObjectSet`, and of the events `CoaAdded`,
  `CoaModified` and `CoaDeleted`.
