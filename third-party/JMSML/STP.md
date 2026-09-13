# Changes required by STP on top of the standard JMSML

* Add a .NET 6 project 
* In libaraian.cs - make ConfigData property public
* Add an Instance folder to the solution and add the XML contents of ../instance (sibling to the source folder) 
* Make properties of all the Instance/*.xml as Build Action=Content and Copy to Output Directory=Copy if newer
* Add  jmsmlSTP.config, with the following paths:

```xml
<JMSMLConfig xmlns="http://esri.com/jmsmlConfig.xsd"
             LibraryPath=".\Instance"
             LibraryName="Base.xml">
  <ETLConfig DomainSeparator=" : " PointSize="64" SVGHome ="" GraphicRoot="{Symbols_Root}" 
			 GraphicHome="C:\ProgramData\STP\JMS\svg" GraphicExtension="emf">
```
The static reference to NLog is replaced by ILogger, which is injected into the Librarian consturctor

```csharp
internal static ILogger Logger 
{ 
    get => _logger; 
    private set => _logger = value; 
}
protected static ILogger _logger;

public Librarian(ILogger logger, string configPath = "")
{
    Logger = logger ?? NullLogger.Instance;
```

The factory is then used intialize logger objects in the different classes

Helper classes are defined to wrap the actual ILogger calls into the simpler (NLog) ones that
 are used throughout, as well as provide aliases for other initialization methods

```csharp
global using Logger = Microsoft.Extensions.Logging.ILogger;
using Microsoft.Extensions.Logging;

namespace JointMilitarySymbologyLibrary;

internal static class LogExtension
{
    public static void Error(this ILogger logger, string? message, params object?[] args) => logger.LogError(message, args);
    public static void Info(this ILogger logger, string? message, params object?[] args) => logger.LogInformation(message, args);
    public static void Warn(this ILogger logger, string? message, params object?[] args) => logger.LogWarning(message, args);
}

internal static class LogManager
{
    public static ILogger GetCurrentClassLogger() => Librarian.Logger;
}
```
Global aliasing provided by `global using` requires `JMSML.csproj` to use language version 10 or above. 

```xml
  <PropertyGroup>
    <TargetFrameworks>net6.0;netstandard2.0</TargetFrameworks>
    <version>1.0.0</version>
	<LangVersion>10.0</LangVersion>
	<GenerateResourceUsePreserializedResources>true</GenerateResourceUsePreserializedResources>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <GenerateRuntimeConfigurationFiles>true</GenerateRuntimeConfigurationFiles>
  </PropertyGroup>
```

Also required are the following packages:

```xml
  <ItemGroup>
    <PackageReference Include="Svg" Version="3.4.4" />
    <PackageReference Include="System.Resources.Extensions" Version="6.0.0" />
	<PackageReference Include="System.Drawing.Common" Version="5.*" />
	<PackageReference Include="Microsoft.Extensions.Logging" Version="6.0.0" />
  </ItemGroup>
```

NOTE (superseded 2026-09-13, STP-775): this used to say `System.Drawing.Common` must be kept
at version 5, being the last multiplatform release. That guidance is retired. v5 is a .NET 5
package and out of support. Rasterising needs GDI+ on every version - v9 P/Invokes
`gdiplus.dll` and fails on Linux even with libgdiplus installed - so `Symbol.Bitmap` is now
explicitly Windows-only, and `Symbol.CompositeSvg` is the cross-platform path: it composes the
same layers as one SVG document with no imaging library. Packages here can be updated freely