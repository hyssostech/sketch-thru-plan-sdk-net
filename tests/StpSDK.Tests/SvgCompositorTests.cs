using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using JointMilitarySymbologyLibrary;
using NUnit.Framework;

namespace StpSDK.Tests;

/// <summary>
/// STP-775. Locks the geometry and the inheritance rules that make a composed
/// SVG reproduce <c>Symbol.Bitmap</c> exactly.
/// </summary>
/// <remarks>
/// <para>
/// These assertions are structural on purpose: they read the emitted markup
/// rather than rasterising it, so they run on the Linux leg of the CI matrix
/// too - System.Drawing has been Windows-only since .NET 6, and a test that
/// silently skips on half the matrix is a gate that measures nothing.
/// </para>
/// <para>
/// The pixel-level evidence comes from a probe run against the full shipped
/// graphic set - 4554 graphics and 400 real symbols at 32/64/128/256/512 px,
/// compared against Symbol.Bitmap under svg-net AND against a layer-by-layer
/// render under Svg.Skia. Each case below corresponds to a divergence that
/// probe actually found, so these are regression locks, not speculation.
/// </para>
/// </remarks>
[TestFixture]
public class SvgCompositorTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    private static string Layer(string name)
    {
        string path = Path.Combine(TestContext.CurrentContext.TestDirectory,
                                   "TestData", "SvgLayers", name);
        Assert.That(File.Exists(path), $"fixture missing: {path}");
        return path;
    }

    private static XElement ComposeRoot(IEnumerable<string> layers, int w, int h)
    {
        string markup = SvgCompositor.Compose(layers.ToList(), w, h);
        Assert.That(markup, Is.Not.Null, "Compose returned null");
        return XDocument.Parse(markup).Root;
    }

    /// <summary>Pull the numbers out of "translate(a,b) scale(s)".</summary>
    private static (double tx, double ty, double scale) Transform(XElement g)
    {
        string t = (string)g.Attribute("transform");
        Assert.That(t, Is.Not.Null, "group has no transform");

        int ti = t.IndexOf("translate(", StringComparison.Ordinal) + "translate(".Length;
        string translate = t.Substring(ti, t.IndexOf(')', ti) - ti);
        int si = t.IndexOf("scale(", StringComparison.Ordinal) + "scale(".Length;
        string scale = t.Substring(si, t.IndexOf(')', si) - si);

        string[] parts = translate.Split(',');
        return (double.Parse(parts[0], CultureInfo.InvariantCulture),
                double.Parse(parts[1], CultureInfo.InvariantCulture),
                double.Parse(scale, CultureInfo.InvariantCulture));
    }

    [Test]
    public void Compose_SizesTheRootToTheRequestedImage()
    {
        XElement root = ComposeRoot(new[] { Layer("frame-612x792.svg") }, 64, 64);

        Assert.Multiple(() =>
        {
            Assert.That(root.Name, Is.EqualTo(Svg + "svg"));
            Assert.That((string)root.Attribute("width"), Is.EqualTo("64"));
            Assert.That((string)root.Attribute("height"), Is.EqualTo("64"));
            // The viewBox is what lets a consumer override the size in CSS or an
            // <img> tag and have the whole composite rescale instead of crop.
            Assert.That((string)root.Attribute("viewBox"), Is.EqualTo("0 0 64 64"));
        });
    }

    /// <summary>
    /// The shipped renderer scales a layer to the target HEIGHT and then centres
    /// the leftover slack (svg-net reports xMidYMid/meet). Anchoring top-left
    /// instead shifts every layer up by a fraction of a pixel - invisible, and
    /// 138 differing pixels in a single 64x64 layer.
    /// </summary>
    [Test]
    public void Compose_CentresTheSubPixelSlack_NotTopLeft()
    {
        XElement root = ComposeRoot(new[] { Layer("frame-612x792.svg") }, 64, 64);
        var (tx, ty, scale) = Transform(root.Elements(Svg + "g").Single());

        // 612x792 into 64px high: the viewport is truncated to 49x64, so the
        // content scales by 49/612 and leaves 0.588px of vertical slack.
        Assert.Multiple(() =>
        {
            Assert.That(scale, Is.EqualTo(49.0 / 612.0).Within(1e-9));
            Assert.That(tx, Is.EqualTo(0).Within(1e-9));
            Assert.That(ty, Is.EqualTo((64.0 - 792.0 * (49.0 / 612.0)) / 2.0).Within(1e-9),
                        "vertical slack must be centred, not anchored at the top");
            Assert.That(ty, Is.GreaterThan(0.2), "a zero here means top-left anchoring is back");
        });
    }

    /// <summary>
    /// The layer root's inherited attributes have to follow its children onto
    /// the group. Dropping xml:space changed 557 pixels on one control-measure
    /// graphic, because it governs whitespace inside &lt;text&gt;.
    /// </summary>
    [Test]
    public void Compose_CarriesInheritedAttributesOntoTheGroup()
    {
        XElement root = ComposeRoot(new[] { Layer("frame-612x792.svg") }, 64, 64);
        XElement g = root.Elements(Svg + "g").Single();

        Assert.That((string)g.Attribute(XNamespace.Xml + "space"), Is.EqualTo("preserve"));
    }

    /// <summary>
    /// Viewport attributes must NOT be carried over - the compositor has already
    /// turned them into the transform, so re-emitting them would scale twice.
    /// "id" goes too: every graphic in the set uses the same handful of ids, and
    /// keeping them would put duplicates in one document.
    /// </summary>
    [Test]
    public void Compose_DropsViewportAndIdentityAttributes()
    {
        XElement root = ComposeRoot(new[] { Layer("frame-612x792.svg") }, 64, 64);
        XElement g = root.Elements(Svg + "g").Single();

        Assert.Multiple(() =>
        {
            Assert.That(g.Attribute("width"), Is.Null);
            Assert.That(g.Attribute("height"), Is.Null);
            Assert.That(g.Attribute("viewBox"), Is.Null);
            Assert.That(g.Attribute("x"), Is.Null);
            Assert.That(g.Attribute("y"), Is.Null);
            Assert.That(g.Attribute("id"), Is.Null);
            Assert.That(g.Attribute("version"), Is.Null);
            Assert.That(g.Attribute("enable-background"), Is.Null);
        });
    }

    /// <summary>
    /// A layer with no width/height means 100% of the viewport, not its viewBox
    /// extent. Getting this wrong left a 400-unit legacy graphic sitting at 400px
    /// inside a 512px image - 60989 differing pixels.
    /// </summary>
    [TestCase(64, 0.16)]
    [TestCase(128, 0.32)]
    [TestCase(512, 1.28)]
    public void Compose_TreatsAbsentWidthHeightAsFillingTheTarget(int size, double expectedScale)
    {
        XElement root = ComposeRoot(new[] { Layer("nosize-viewbox-only.svg") }, size, size);
        var (tx, ty, scale) = Transform(root.Elements(Svg + "g").Single());

        Assert.Multiple(() =>
        {
            Assert.That(scale, Is.EqualTo(expectedScale).Within(1e-9));
            Assert.That(tx, Is.EqualTo(0).Within(1e-9));
            Assert.That(ty, Is.EqualTo(0).Within(1e-9));
        });
    }

    /// <summary>
    /// A layer already smaller than the target is left at its own size, which is
    /// what the shipped renderer does - it only ever scales DOWN.
    /// </summary>
    [Test]
    public void Compose_DoesNotScaleUpALayerShorterThanTheTarget()
    {
        XElement root = ComposeRoot(new[] { Layer("icon-400x400.svg") }, 512, 512);
        var (_, _, scale) = Transform(root.Elements(Svg + "g").Single());

        Assert.That(scale, Is.EqualTo(1.0).Within(1e-9),
                    "a 400x400 layer in a 512px image must stay at 400px, as Symbol.Bitmap leaves it");
    }

    /// <summary>
    /// Layers are stacked with a group, never a nested &lt;svg&gt;. A nested svg
    /// establishes a viewport and CLIPS to it, and at least one shipped graphic
    /// draws outside its own viewBox - the bitmap path does not clip it, so a
    /// nested svg would silently drop those pixels.
    /// </summary>
    [Test]
    public void Compose_UsesGroupsSoOverflowIsNotClipped()
    {
        XElement root = ComposeRoot(new[] { Layer("overflow-viewbox.svg") }, 512, 512);

        Assert.Multiple(() =>
        {
            Assert.That(root.Descendants(Svg + "svg").Any(), Is.False,
                        "a nested <svg> would clip content that the bitmap path keeps");
            Assert.That(root.Descendants().Any(e => e.Attribute("clip-path") != null), Is.False);
            // The overflowing rect must survive into the output.
            Assert.That(root.Descendants(Svg + "rect").Any(r => (string)r.Attribute("x") == "380"),
                        Is.True);
        });
    }

    [Test]
    public void Compose_PreservesLayerOrder()
    {
        XElement root = ComposeRoot(
            new[] { Layer("frame-612x792.svg"), Layer("icon-400x400.svg") }, 64, 64);

        List<XElement> groups = root.Elements(Svg + "g").ToList();
        Assert.That(groups, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(groups[0].Descendants(Svg + "rect").Any(), Is.True, "frame must be first");
            Assert.That(groups[1].Descendants(Svg + "circle").Any(), Is.True, "icon must be second");
        });
    }

    [Test]
    public void Compose_SkipsMissingLayersAndKeepsTheRest()
    {
        XElement root = ComposeRoot(
            new[] { Path.Combine("does", "not", "exist.svg"), Layer("icon-400x400.svg") }, 64, 64);

        Assert.That(root.Elements(Svg + "g").Count(), Is.EqualTo(1));
    }

    [Test]
    public void Compose_ReturnsNullWhenNothingCouldBeComposed()
    {
        Assert.That(SvgCompositor.Compose(new[] { "no-such-file.svg" }, 64, 64), Is.Null);
        Assert.That(SvgCompositor.Compose(new string[0], 64, 64), Is.Null);
    }

    /// <summary>
    /// A comma decimal separator would emit transform="translate(0,0,2941...)",
    /// which parses as the wrong number of arguments and silently misplaces
    /// every layer on a machine with a European locale.
    /// </summary>
    [Test]
    public void Compose_IsInvariantOfTheAmbientCulture()
    {
        CultureInfo original = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            string markup = SvgCompositor.Compose(new[] { Layer("frame-612x792.svg") }, 64, 64);

            XElement g = XDocument.Parse(markup).Root.Elements(Svg + "g").Single();
            string transform = (string)g.Attribute("transform");

            Assert.That(transform, Does.Contain("0.29411"),
                        $"decimal separator leaked from the ambient culture: {transform}");
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }

    /// <summary>
    /// The serialised output must not carry a platform line ending.
    /// </summary>
    /// <remarks>
    /// XmlWriter's default NewLineChars is the platform's, so the same symbol
    /// composed on Windows and on Linux differed by one byte per line - all
    /// 22770 composites in the shipped set, purely CR. Semantically and visually
    /// identical, but it defeats hashing the output for a cache key, an ETag or
    /// a build attestation, which is exactly what a service serving these wants
    /// to do. With the writer pinned, both platforms produce the same SHA256
    /// over the whole graphic set.
    /// </remarks>
    [Test]
    public void Compose_EmitsPlatformIndependentLineEndings()
    {
        string markup = SvgCompositor.Compose(new[] { Layer("frame-612x792.svg") }, 64, 64);

        Assert.That(markup, Does.Not.Contain("\r"),
                    "a CR here means the output is not byte-stable across platforms");
    }

    [Test]
    public void Compose_RejectsNonPositiveSizes()
    {
        var layers = new[] { Layer("icon-400x400.svg") };
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SvgCompositor.Compose(layers, 0, 64));
            Assert.Throws<ArgumentOutOfRangeException>(() => SvgCompositor.Compose(layers, 64, -1));
            Assert.Throws<ArgumentNullException>(() => SvgCompositor.Compose(null, 64, 64));
        });
    }
}
