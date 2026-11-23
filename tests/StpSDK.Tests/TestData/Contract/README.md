# Vendored public contract (pinned)

`sketch-thru-plan-api.json` is a byte-for-byte copy of the public OpenRPC contract, pinned so the
SDK's coverage test (`ContractCoverageTests`) checks against a known version.

| | |
|---|---|
| Source repository | `hyssostech/sketch-thru-plan-sdk-js` |
| Commit | `b7a490d00195fb2c8ed4a29e965938a62f94a8e4` (release/5.12) |
| Path in the source | `json-api/sketch-thru-plan-api.json` |
| Contract version (`info.version`) | `0.9.11` |
| SHA-256 of this copy (LF line endings) | `8c48ac0f0d8ec5ca5e13c4b6d930d311c13fdb59e5a52ede3d92f07fea37fa8b` |

`ContractCoverageTests` checks the version and the SHA-256 above, so the copy cannot change by
accident. To move to a newer contract, copy the file from the new commit, update this table (commit,
version and hash) and the constants at the top of `ContractCoverageTests.cs`, then add any SDK member
the coverage test reports missing, or record the mapping in `docs/articles/contract-name-mapping.md`.
