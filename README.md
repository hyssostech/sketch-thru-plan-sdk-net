# Sketch-Thru-Plan .NET SDK

This repository holds the .NET SDK for [Hyssos Tech's Sketch-Thru-Plan](http://www.hyssos.com)
Natural Language Planning Engine, together with its samples and developer resources.

Sketch-Thru-Plan (STP) is a technological advance for today's warfighter that enhances cognition by implementing military doctrine driven, 
AI based task recognition that automatically perceives planners' implicit higher-order intentions, on the fly, generating Tasks, Task Matrix, 
Synch Matrix, and other OPORD products with minimal additional user input. 

This is accomplished via robust multimodal Natural Language Processing that fuses the user's doctrinal speech and sketch for COA creation, 
seamlessly integrating plan outputs that drive simulators for tight adjudication loops and C2 systems. 

A TypeScript/JavaScript version of the SDK can be found at the [Sketch-Thru-Plan JavaScript SDK repository](https://github.com/hyssostech/sketch-thru-plan-sdk-js)


## Prerequisites

* Sketch-Thru-Plan (STP) Engine (v5.6.0+) running on localhost or an accessible machine
* STP Speech component running on localhost
* Most samples require a working microphone, mouse or stylus

## Nuget package

The SDK is available as a nuget package supporting .NET 8 and .NET Standard 2.0 (Framework) projects: 

* [HyssosTech.Sdk.STP](https://www.nuget.org/packages/HyssosTech.Sdk.STP/)

## Which package version do you need?

`HyssosTech.Sdk.STP` spans two different SDKs. Read this before upgrading.

| version | what it is | published from |
|---|---|---|
| `0.4.0-preview` and later | **This SDK** - a JSON-RPC client whose surface mirrors the JavaScript SDK | `sketch-thru-plan-sdk-net` |
| `0.3.9-preview` and earlier | The original OAA SDK - a substantially larger API | an internal repository |

`0.4.0-preview` was the changeover, and it is **not** a drop-in replacement for
`0.3.x`: this client deliberately carries a smaller surface. If your code uses
types such as `SymbolIdCode`, `MilTypes`, `StpTaskFactory` or `StpOaaConnector`,
they are not present here - stay on `0.3.9-preview`, which remains published and
listed on nuget.org.

## Getting started

The [quickstart](quickstart) folder contains a simple introductory example that illustrates the use of the SDK to in the context of STP's foundational capabilities

## Samples

The [samples](samples) folder contains examples extending the quickstart, and covering additional capabilities of the SDK 

## Reference

[Sketch-Thru-Plan .NET SDK Reference](docs/Sketch-Thru-Plan.NET-SDK-Reference.pdf)

[MIL-STD-2525D Joint Military Symbology](https://www.jcs.mil/Portals/36/Documents/Doctrine/Other_Pubs/ms_2525d.pdf)

