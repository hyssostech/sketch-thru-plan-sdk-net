using System;
using System.Drawing;
using System.IO;
using System.Xml.Linq;
using NUnit.Framework;

namespace StpSDK.Tests;

/// <summary>
/// Validates the JMSML-backed symbol rendering wired into
/// <see cref="StpSymbol.Bitmap"/> and <see cref="StpSymbol.CompositeSvg"/>.
/// </summary>
/// <remarks>
/// <para>
/// STP-775 changed the platform contract. <see cref="StpSymbol.Bitmap"/> needs
/// GDI+ and therefore Windows; off Windows it now THROWS
/// <see cref="PlatformNotSupportedException"/> naming
/// <see cref="StpSymbol.CompositeSvg"/>, rather than returning a silent
/// <c>null</c> that hands the caller an NRE with no explanation.
/// </para>
/// <para>
/// These tests assert BOTH halves of that contract, so each leg of the CI matrix
/// verifies its own: the Windows runner proves rendering still works, the ubuntu
/// runner proves the guard fires. Previously they asserted "never throws", which
/// the ubuntu runner satisfied by rendering nothing at all.
/// </para>
/// </remarks>
[TestFixture]
public class SymbolRenderingTests
{
    private const string SvgPath = @"C:\ProgramData\STP\JMS\svg";

    /// <summary>True only where a bitmap can actually be produced.</summary>
    private static bool CanRasterise => OperatingSystem.IsWindows() && Directory.Exists(SvgPath);

    [Test]
    public void Bitmap_KnownUnitSidc_RendersOnWindows_ThrowsElsewhere()
    {
        // Friendly mechanized infantry brigade (valid 15-character 2525C SIDC).
        var sym = new StpSymbol { SymbolId = "SFGPUCIZ---H---" };
        StpRecognizer.JMSSVGPath = SvgPath;

        if (!OperatingSystem.IsWindows())
        {
            Assert.Throws<PlatformNotSupportedException>(
                () => sym.Bitmap(64, 64),
                "off Windows the platform guard must fire rather than return null");
            return;
        }

        Bitmap bmp = sym.Bitmap(64, 64);

        if (Directory.Exists(SvgPath))
        {
            Assert.That(bmp, Is.Not.Null,
                        "JMSML should render a bitmap for a valid 2525C unit SIDC when the SVG set is present");
            Assert.Multiple(() =>
            {
                Assert.That(bmp!.Width, Is.GreaterThan(0));
                Assert.That(bmp.Height, Is.GreaterThan(0));
            });
        }
        else
        {
            // Windows, but the graphic set is not installed on this machine.
            Assert.Pass($"SVG set absent at {SvgPath}; Bitmap returned {(bmp is null ? "null" : "a bitmap")}.");
        }
    }

    // A few valid 15-character 2525C unit SIDCs spanning affiliations - the JMSML
    // rendering path is Librarian.MakeSymbol("2525C", ...) -> Symbol.Bitmap.
    [TestCase("SFGPUCIZ---H---")] // friendly mechanized infantry brigade
    [TestCase("SHGPUCIZ---H---")] // hostile mechanized infantry brigade
    [TestCase("SNGPUCIZ---H---")] // neutral
    [TestCase("SUGPUCIZ---H---")] // unknown
    public void Bitmap_AffiliationVariants_RenderOnWindows_ThrowElsewhere(string sidc)
    {
        StpRecognizer.JMSSVGPath = SvgPath;
        var sym = new StpSymbol { SymbolId = sidc };

        if (!OperatingSystem.IsWindows())
        {
            Assert.Throws<PlatformNotSupportedException>(() => sym.Bitmap(64, 64));
            return;
        }

        Bitmap bmp = null;
        Assert.DoesNotThrow(() => bmp = sym.Bitmap(64, 64), $"Rendering SIDC {sidc} should not throw on Windows");

        if (Directory.Exists(SvgPath))
            Assert.That(bmp, Is.Not.Null, $"Expected a rendered bitmap for {sidc}");
    }

    /// <summary>
    /// An empty SIDC short-circuits BEFORE the platform guard, so it is null on
    /// every platform - the guard lives in JMSML's Symbol.Bitmap, which an empty
    /// SIDC never reaches because no Symbol is built.
    /// </summary>
    [Test]
    public void Bitmap_EmptySidc_ReturnsNullOnEveryPlatform()
    {
        var sym = new StpSymbol { SymbolId = "" };
        Assert.That(sym.Bitmap(64, 64), Is.Null);
    }

    // ---------------- CompositeSvg: the cross-platform path ----------------

    [Test]
    public void CompositeSvg_NeverThrowsOnAnyPlatform()
    {
        StpRecognizer.JMSSVGPath = SvgPath;
        var sym = new StpSymbol { SymbolId = "SFGPUCIZ---H---" };

        // The whole point: no imaging library, so no platform can refuse it.
        Assert.DoesNotThrow(() => sym.CompositeSvg(64, 64));
    }

    [Test]
    public void CompositeSvg_ProducesWellFormedSvg_WhenGraphicSetPresent()
    {
        StpRecognizer.JMSSVGPath = SvgPath;
        var sym = new StpSymbol { SymbolId = "SFGPUCIZ---H---" };

        string svg = sym.CompositeSvg(64, 64);

        if (!Directory.Exists(SvgPath))
        {
            Assert.That(svg, Is.Null, "with no graphic set there is nothing to compose");
            Assert.Pass($"SVG set absent at {SvgPath}.");
            return;
        }

        Assert.That(svg, Is.Not.Null, "a valid SIDC with the graphic set present should compose");

        XElement root = XDocument.Parse(svg).Root;
        Assert.Multiple(() =>
        {
            Assert.That(root!.Name.LocalName, Is.EqualTo("svg"));
            Assert.That((string)root.Attribute("width"), Is.EqualTo("64"));
            Assert.That((string)root.Attribute("height"), Is.EqualTo("64"));
            // Layers are stacked as groups; a symbol always has at least one.
            Assert.That(root.Elements().Any(e => e.Name.LocalName == "g"), Is.True,
                        "expected at least one composed layer");
        });
    }

    [Test]
    public void CompositeSvg_EmptySidc_ReturnsNull()
    {
        var sym = new StpSymbol { SymbolId = "" };
        Assert.That(sym.CompositeSvg(64, 64), Is.Null);
    }
}
