Do Not Translate or Localize

This project is based on or incorporates material from the projects listed below (Third Party IP). The original copyright notice and the license under which Hyssos received such Third Party IP, are set forth below. Such licenses and notices are provided for informational purposes only. Where permitted, Hyssos licenses the Third Party IP to you under the licensing terms for Hyssos products. Hyssos reserves all other rights not expressly granted under this agreement, whether by implication, estoppel or otherwise.

**a. Joint Military Symbology Library (JMSML)**

This repository VENDORS (includes a copy of) source code DERIVED FROM the open-source
Joint Military Symbology XML / Joint Military Symbology Library project by Esri, available at
https://github.com/Esri/joint-military-symbology-xml. The vendored code lives under
`third-party/JMSML/` and is bundled into the `HyssosTech.Sdk.STP` NuGet package (it provides
the 2525 symbology resolution and rendering used by `StpSymbol`).

The vendored copy has been MODIFIED by Hyssos Tech (repackaged as a .NET SDK-style project,
logging switched from NLog to Microsoft.Extensions.Logging, reduced conversion-status log
noise, and related changes). Those modifications are documented in `third-party/JMSML/NOTICE`
and `third-party/JMSML/STP.md`. The original Esri copyright and Apache-2.0 license headers are
retained verbatim at the top of each hand-written source file.

Copyright 2014 - 2015 Esri

Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.

A full copy of the Apache License 2.0 is included at `third-party/JMSML/LICENSE`.

[](Esri Tags: ArcGIS Defense and Intelligence Joint Military Symbology XML ArcGISSolutions) [](Esri Language: XML)

**b. Logging makes use of NLog**
NLog is licensed under (the terms)[https://github.com/NLog/NLog.Extensions.Logging/blob/master/LICENSE]:
```
Copyright (c) 2016, NLog
All rights reserved.
Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
```
**c. DocFX**
API reference documentation is generated with DocFX (https://github.com/dotnet/docfx), a build-time tool licensed under the MIT License. DocFX is not redistributed as part of the SDK package.