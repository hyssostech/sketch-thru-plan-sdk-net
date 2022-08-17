# Sketch-thru-Plan SDK .NET Resources

This project hosts developer resources related to  [Hyssos Tech's Sketch-Thru-Plan](http://www.hyssos.com) Natural Language Planning Engine SDK.

Sketch-Thru-Plan (STP) is a technological advance for today's warfighter that enhances cognition by implementing military doctrine driven, 
AI based task recognition that automatically perceives planners' implicit higher-order intentions, on the fly, generating Tasks, Task Matrix, 
Synch Matrix, and other OPORD products with minimal additional user input. 

This is accomplished via robust multimodal Natural Language Processing that fuses user’s doctrinal speech and sketch for COA creation, 
seamlessly integrating plan outputs that drive simulators for tight adjudication loops and C2 systems. 

A TypeScript/JavaScript version of the SDK can be found at the [Sketch-Thru-Plan JavaScript SDK Resources GitHub repository](https://github.com/hyssostech/sketch-thru-plan-sdk-resources)


## Prerequisites

* Sketch-Thru-Plan (STP) Engine (v5.6.0+) running on localhost or an accessible machine
* STP Speech component running on localhost
* Most samples require a working microphone, mouse or stylus

## Nuget package

The SDK is available as a nuget package supporting .NET 6 and .NET Standard 2.0 (Framework) projects: 

* [HyssosTech.Sdk.STP](https://www.nuget.org/packages/HyssosTech.Sdk.STP/)


## Getting started

The [quickstart](quickstart) folder contains a simple introductory example that illustrates the use of the SDK to in the context of STP's foundational capabilities

## Samples

The [samples](samples) folder contains examples extending the quickstart, and covering additional capabilities of the SDK 

## Reference

[Sketch-Thru-Plan .NET SDK Reference](docs/Sketch-Thru-Plan.NET-SDK-Reference.pdf)

[MIL-STD-2525D Joint Military Symbology](https://www.jcs.mil/Portals/36/Documents/Doctrine/Other_Pubs/ms_2525d.pdf)

