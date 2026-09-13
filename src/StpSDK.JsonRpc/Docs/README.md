# Sketch-thru-Plan .NET SDK

This SDK provides means to interact with [Hyssos Tech's Sketch-Thru-Plan](http://www.hyssos.com) natural language Engine for map-based planning.

Sketch-Thru-Plan (STP) is a technological advance for today's warfighter that enhances cognition by implementing military doctrine driven, 
AI based task recognition that automatically perceives planners' implicit higher-order intentions, on the fly, generating Tasks, Task Matrix, 
Sync Matrix, and other OPORD products with minimal additional user input. 

This is accomplished via Robust multimodal Natural Language Processing that fuses user's doctrinal speech and sketch for COA creation, 
seamlessly integrating plan outputs that drive simulators for tight adjudication loops and C2 systems. 


## Upgrading from 0.3.x

`HyssosTech.Sdk.STP` spans two different SDKs.

* `0.4.0-preview` and later - **this SDK**, a JSON-RPC client whose surface mirrors the JavaScript SDK.
* `0.3.9-preview` and earlier - the original OAA SDK, a substantially larger API.

`0.4.0-preview` was the changeover and is **not** a drop-in replacement: this
client deliberately carries a smaller surface. If your code uses types such as
`SymbolIdCode`, `MilTypes`, `StpTaskFactory` or `StpOaaConnector`, they are not
present here - stay on `0.3.9-preview`, which remains published and listed.


## Resources

Supporting documentation and source code can be found in the [Sketch-Thru-Plan .NET SDK Resources GitHub repository](https://github.com/hyssostech/sketch-thru-plan-sdk-net)

A TypeScript/JavaScript version of the SDK can be found at the [Sketch-Thru-Plan JavaScript SDK repository](https://github.com/hyssostech/sketch-thru-plan-sdk-js)

## Verifying this package

Every package on nuget.org carries a repository signature applied by
nuget.org itself. You can check the one you restored:

```sh
dotnet nuget verify HyssosTech.Sdk.STP.<version>.nupkg
```

That proves the bytes are the bytes nuget.org recorded. It does **not** prove
which source built them - a repository signature is a statement about custody,
not provenance.

This package is also built deterministically with SourceLink and ships a
`.snupkg`, so a debugger can step into the exact commit it came from.

Releases additionally attach a `SHA256SUMS` manifest, a SLSA build provenance
attestation over it, a CycloneDX SBOM and the vulnerability scan taken over
that SBOM. The full procedure - and, for each check, what it does *not* prove -
is in [VERIFYING.md](https://github.com/hyssostech/sketch-thru-plan-sdk-net/blob/main/VERIFYING.md).
It is linked rather than reproduced because it is not part of this package.

## API & Reference Documentation

API reference documentation is generated with [DocFX](https://github.com/dotnet/docfx) from the SDK source and published to GitHub Pages.

Build and preview locally from the repository root:
```
dotnet tool update -g docfx
docfx docs/docfx.json --serve
```

[MIL-STD-2525D Joint Military Symbology](https://www.jcs.mil/Portals/36/Documents/Doctrine/Other_Pubs/ms_2525d.pdf)
