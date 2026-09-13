using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace JointMilitarySymbologyLibrary
{
    /// <summary>
    /// Composes the ordered SVG layers of a symbol into a SINGLE SVG document,
    /// with no imaging library involved.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Symbol rendering here is LAYER COMPOSITING, not drawing: a frame, an
    /// entity icon, modifiers, echelon, status and HQ/TF/dummy marks are stacked
    /// onto one surface. Stacking SVG onto SVG is pure XML, so this half of the
    /// pipeline needs neither System.Drawing nor a native rasteriser, and runs
    /// anywhere .NET runs. Only <see cref="Symbol.Bitmap"/> is Windows-bound.
    /// </para>
    /// <para>
    /// Each layer becomes a NESTED &lt;svg&gt; carrying its own viewBox. That is
    /// what lets layers with different coordinate systems compose correctly: the
    /// shipped graphic set uses both "0 0 612 792" (3879 files) and
    /// "0 0 400 400" (660 files), and a single symbol routinely mixes the two.
    /// </para>
    /// <para>
    /// The per-layer viewport is sized by REPLICATING the rule in
    /// <see cref="Symbol.Bitmap"/> - scale to the target height, preserving
    /// aspect, and never scale up - rather than delegating to
    /// preserveAspectRatio="meet". The two rules agree only while every layer is
    /// at least as tall as the target and none is wider than tall. Both hold for
    /// the current corpus at the usual 64px point size, but neither is
    /// guaranteed: Bitmap(int,int) is public and a caller asking for 512px hits
    /// a target taller than the 400x400 layers, where "meet" would scale them up
    /// and the shipped renderer does not. Replicating the rule keeps the two
    /// outputs identical at every size instead of only the sizes tested.
    /// </para>
    /// <para>
    /// preserveAspectRatio is "xMidYMid meet" because that is what the shipped
    /// renderer does, measured rather than assumed: svg-net reports
    /// AspectRatio = xMidYMid/meet on every graphic in the set. It matters. The
    /// integer truncation above makes the viewport very slightly non-proportional
    /// to the viewBox - 612x792 into a 64px target gives a 49x64 viewport holding
    /// content that scales to 49x63.41 - and "meet" centres that 0.59px of slack.
    /// Anchoring top-left instead shifts every layer down by ~0.3px, which is
    /// invisible to the eye and shows up as 138 differing pixels in a single
    /// 64x64 layer. Switching this string to xMinYMin regresses the composite
    /// from exact to merely close.
    /// </para>
    /// </remarks>
    public static class SvgCompositor
    {
        private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";
        private static readonly XNamespace Xlink = "http://www.w3.org/1999/xlink";

        /// <summary>
        /// Compose <paramref name="layerPaths"/> into one SVG document sized
        /// <paramref name="width"/> x <paramref name="height"/>, laid out to
        /// match <see cref="Symbol.Bitmap"/> at the same size.
        /// </summary>
        /// <returns>
        /// The SVG markup, or <c>null</c> when no layer could be composed -
        /// mirroring <see cref="Symbol.Bitmap"/>, which returns <c>null</c>
        /// rather than an empty surface when there is nothing to draw.
        /// </returns>
        /// <remarks>
        /// The result carries both width/height AND a matching viewBox, so a
        /// consumer that overrides the size (CSS, or an &lt;img&gt; with its own
        /// dimensions) rescales the whole composite cleanly rather than cropping
        /// it. Missing and unparseable layers are skipped, which is the
        /// tolerance the bitmap path already has - it logs and carries on.
        /// </remarks>
        public static string Compose(IEnumerable<string> layerPaths, int width, int height)
        {
            if (layerPaths == null) throw new ArgumentNullException(nameof(layerPaths));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            var root = new XElement(Svg + "svg",
                new XAttribute("version", "1.1"),
                new XAttribute("width", Str(width)),
                new XAttribute("height", Str(height)),
                new XAttribute("viewBox", "0 0 " + Str(width) + " " + Str(height)),
                new XAttribute(XNamespace.Xmlns + "xlink", Xlink.NamespaceName));

            int composed = 0;

            foreach (string path in layerPaths)
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    continue;

                XElement layer;
                try
                {
                    layer = LoadRoot(path);
                }
                catch (XmlException)
                {
                    continue;
                }

                if (layer == null)
                    continue;

                string viewBox = (string)layer.Attribute("viewBox");
                double srcW, srcH;
                if (!TryMeasure(layer, width, height, out srcW, out srcH))
                    continue;

                // The layer's own user-space box. Absent a viewBox, the layer's
                // intrinsic size IS its user space, so the fit is the identity.
                double vbX = 0, vbY = 0, vbW = srcW, vbH = srcH;
                if (!string.IsNullOrEmpty(viewBox))
                    TryViewBox(viewBox, ref vbX, ref vbY, ref vbW, ref vbH);

                // Symbol.Bitmap's rule, verbatim: shrink to the target height,
                // keeping aspect, and leave anything already short enough alone.
                // The int truncation is deliberate - it is what the shipped
                // renderer does, and rounding instead would shift layers by a
                // pixel relative to today's output.
                double dstW = srcW, dstH = srcH;
                if (srcH > height)
                {
                    dstW = (int)((srcW / srcH) * height);
                    dstH = height;
                }

                // Apply the viewBox fit as an explicit transform on a <g>
                // rather than handing a nested <svg> a viewBox and letting the
                // renderer work it out. Two reasons, both measured:
                //
                //  1. A nested <svg> establishes a viewport and CLIPS to it. At
                //     least one shipped graphic (45110315.svg) draws outside its
                //     own 400x400 viewBox, and the bitmap path does not clip it,
                //     so a nested <svg> silently loses those pixels. A <g> does
                //     not clip, which is what the shipped renderer does.
                //  2. It removes any dependence on how a consuming renderer
                //     implements nested viewports - the composite carries a plain
                //     translate/scale that every SVG renderer handles.
                //
                // The transform is the "xMidYMid meet" fit written out: one
                // uniform scale, the slack split evenly, and the viewBox origin
                // subtracted.
                double scale = Math.Min(dstW / vbW, dstH / vbH);
                double tx = (dstW - vbW * scale) / 2.0 - vbX * scale;
                double ty = (dstH - vbH * scale) / 2.0 - vbY * scale;

                var group = new XElement(Svg + "g",
                    new XAttribute("transform",
                        "translate(" + Str(tx) + "," + Str(ty) + ") scale(" + Str(scale) + ")"));

                // Carry the layer root's INHERITED attributes onto the group.
                // Reparenting the children without these silently changes how
                // they render: every graphic in the set carries
                // xml:space="preserve", which governs whitespace inside <text>,
                // and dropping it moved a line of text far enough to change 557
                // pixels on a single 400px control-measure graphic.
                foreach (XAttribute a in layer.Attributes())
                {
                    if (!IsInherited(a)) continue;
                    group.SetAttributeValue(a.Name, a.Value);
                }

                group.Add(layer.Nodes());
                root.Add(group);

                composed++;
            }

            if (composed == 0)
                return null;

            var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), root);

            // Pin the line ending. XmlWriter's default NewLineChars is the
            // platform's, so the same symbol serialised on Windows and on Linux
            // differed by one byte per line - 22770 composites, every one of
            // them, purely CR. Semantically identical and visually identical,
            // but it defeats hashing the output for a cache, an ETag or an
            // attestation, which is exactly what a service serving these will
            // want to do. Verified: with this pinned, Windows and Linux produce
            // byte-identical output over the whole graphic set.
            var settings = new XmlWriterSettings
            {
                Indent = false,
                NewLineChars = "\n",
                NewLineHandling = NewLineHandling.Replace,
                OmitXmlDeclaration = false,
            };

            using (var writer = new Utf8StringWriter())
            {
                using (XmlWriter xml = XmlWriter.Create(writer, settings))
                {
                    doc.Save(xml);
                }

                return writer.ToString();
            }
        }

        /// <summary>
        /// Determine the box a layer occupies in the target image.
        /// </summary>
        /// <remarks>
        /// 15 of the 4554 shipped graphics - Legacy/2525B_Change_2 and four
        /// Legacy/2525C control measures - declare a viewBox and NO width or
        /// height. They are reachable: a 15-character SIDC routes through the
        /// 2525C legacy set. Per the SVG spec an absent width/height means 100%,
        /// so those layers FILL the target rather than sitting at their viewBox
        /// extent, which is also what the shipped bitmap path does - svg-net
        /// reports W=100% H=100% and resolves it against the surface. Falling
        /// back to the viewBox extent instead left one of them 400px wide inside
        /// a 512px image, differing by 60989 pixels.
        /// </remarks>
        private static bool TryMeasure(XElement layer, int width, int height, out double w, out double h)
        {
            bool haveW = TryLength((string)layer.Attribute("width"), out w);
            bool haveH = TryLength((string)layer.Attribute("height"), out h);

            // Absent or percentage: resolve against the target viewport.
            if (!haveW || w <= 0) w = width * Percent((string)layer.Attribute("width")) / 100.0;
            if (!haveH || h <= 0) h = height * Percent((string)layer.Attribute("height")) / 100.0;

            return w > 0 && h > 0;
        }

        /// <summary>
        /// The percentage in an SVG length, defaulting to 100 - which is both the
        /// spec default for an absent width/height and the right answer for an
        /// unparseable one.
        /// </summary>
        private static double Percent(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                string s = value.Trim();
                if (s.EndsWith("%", StringComparison.Ordinal))
                {
                    double pct;
                    if (double.TryParse(s.Substring(0, s.Length - 1), NumberStyles.Float,
                                        CultureInfo.InvariantCulture, out pct) && pct > 0)
                    {
                        return pct;
                    }
                }
            }

            return 100.0;
        }

        /// <summary>
        /// Whether an attribute on a layer's root &lt;svg&gt; should follow its
        /// children onto the wrapping group.
        /// </summary>
        /// <remarks>
        /// Everything that describes the layer's own VIEWPORT is excluded - the
        /// compositor has already turned that into a transform, and re-applying
        /// it would scale the layer twice. Namespace declarations belong on the
        /// composite root, and "id" is dropped because every graphic in the set
        /// uses the same handful of ids: keeping them would put duplicates in one
        /// document. Everything else - notably xml:space, and any presentation
        /// attribute an author put on the root for its children to inherit -
        /// comes along.
        /// </remarks>
        private static bool IsInherited(XAttribute a)
        {
            if (a.IsNamespaceDeclaration)
                return false;

            if (a.Name.Namespace == XNamespace.None)
            {
                switch (a.Name.LocalName)
                {
                    case "width":
                    case "height":
                    case "viewBox":
                    case "preserveAspectRatio":
                    case "x":
                    case "y":
                    case "version":
                    case "baseProfile":
                    case "id":
                    case "enable-background":
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Parse a viewBox into its four numbers, leaving the defaults in place
        /// if it is malformed.
        /// </summary>
        private static bool TryViewBox(string viewBox, ref double x, ref double y, ref double w, ref double h)
        {
            string[] parts = viewBox.Replace(',', ' ')
                                    .Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            double px, py, pw, ph;
            if (parts.Length != 4 ||
                !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out px) ||
                !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out py) ||
                !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out pw) ||
                !double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out ph) ||
                pw <= 0 || ph <= 0)
            {
                return false;
            }

            x = px; y = py; w = pw; h = ph;
            return true;
        }

        /// <summary>
        /// Parse an SVG length, accepting the "px" suffix the corpus uses.
        /// A percentage is rejected: it is relative to a viewport this layer
        /// does not have, so the viewBox is the better answer.
        /// </summary>
        private static bool TryLength(string value, out double result)
        {
            result = 0;
            if (string.IsNullOrEmpty(value))
                return false;

            string s = value.Trim();
            if (s.EndsWith("%", StringComparison.Ordinal))
                return false;
            if (s.EndsWith("px", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(0, s.Length - 2).Trim();

            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        /// <summary>
        /// Format without the ambient culture - a comma decimal separator would
        /// produce markup that parses as two values or not at all.
        /// </summary>
        private static string Str(double v)
        {
            return v == Math.Floor(v) && Math.Abs(v) < 1e9
                ? ((long)v).ToString(CultureInfo.InvariantCulture)
                : v.ToString("0.##########", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Load a layer's root element, tolerating the DOCTYPE these Illustrator
        /// exports carry; XDocument.Load would otherwise try to resolve the DTD
        /// over the network.
        /// </summary>
        private static XElement LoadRoot(string path)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                XmlResolver = null,
                IgnoreComments = true,
            };

            using (var reader = XmlReader.Create(path, settings))
            {
                return XDocument.Load(reader).Root;
            }
        }

        private sealed class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }
        }
    }
}
