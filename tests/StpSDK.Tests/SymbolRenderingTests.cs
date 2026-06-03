using System;
using System.Drawing;
using System.IO;
using NUnit.Framework;

namespace StpSDK.Tests;

/// <summary>
/// Validates the JMSML-backed symbol rendering wired into <see cref="StpSymbol.Bitmap"/>.
/// Rendering requires the JMSML SVG graphic set and <c>System.Drawing</c>, so the
/// strong (non-null) assertion runs only on Windows when the SVG set is present;
/// elsewhere the test asserts graceful behavior (no throw, null allowed).
/// </summary>
[TestFixture]
public class SymbolRenderingTests
{
    private const string SvgPath = @"C:\ProgramData\STP\JMS\svg";

    [Test]
    public void Bitmap_KnownUnitSidc_RendersWhenJmsAvailable()
    {
        // Friendly mechanized infantry brigade (valid 15-character 2525C SIDC).
        var sym = new StpSymbol { SymbolId = "SFGPUCIZ---H---" };
        StpRecognizer.JMSSVGPath = SvgPath;

        var bmp = sym.Bitmap(64, 64);

        bool canRender = OperatingSystem.IsWindows() && Directory.Exists(SvgPath);
        if (canRender)
        {
            Assert.That(bmp, Is.Not.Null, "JMSML should render a bitmap for a valid 2525C unit SIDC when the SVG set is present");
            Assert.That(bmp!.Width, Is.GreaterThan(0));
            Assert.That(bmp.Height, Is.GreaterThan(0));
        }
        else
        {
            // No SVG graphics / non-Windows: must degrade gracefully, not throw.
            Assert.Pass($"JMS SVG set not available here (canRender={canRender}); Bitmap returned {(bmp is null ? "null" : "a bitmap")} without throwing.");
        }
    }

    // A few valid 15-character 2525C unit SIDCs spanning affiliations - the JMSML
    // rendering path (Librarian.MakeSymbol("2525C", ...) -> Symbol.Bitmap) should
    // resolve and render each when the SVG set is present, and never throw.
    [TestCase("SFGPUCIZ---H---")] // friendly mechanized infantry brigade
    [TestCase("SHGPUCIZ---H---")] // hostile mechanized infantry brigade
    [TestCase("SNGPUCIZ---H---")] // neutral
    [TestCase("SUGPUCIZ---H---")] // unknown
    public void Bitmap_AffiliationVariants_RenderOrNullGracefully(string sidc)
    {
        StpRecognizer.JMSSVGPath = SvgPath;
        var sym = new StpSymbol { SymbolId = sidc };

        Bitmap bmp = null;
        Assert.DoesNotThrow(() => bmp = sym.Bitmap(64, 64), $"Rendering SIDC {sidc} should not throw");

        if (OperatingSystem.IsWindows() && Directory.Exists(SvgPath))
            Assert.That(bmp, Is.Not.Null, $"Expected a rendered bitmap for {sidc}");
    }

    [Test]
    public void Bitmap_EmptySidc_ReturnsNull()
    {
        var sym = new StpSymbol { SymbolId = "" };
        Assert.That(sym.Bitmap(64, 64), Is.Null);
    }
}
