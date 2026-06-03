using System;
using System.Drawing;
using System.IO;
using JointMilitarySymbologyLibrary;

namespace StpSDK;

/// <summary>
/// JMSML-backed symbol renderer. Resolves a SIDC to a Joint Military Symbology
/// Library <see cref="Symbol"/> and renders it to a <see cref="Bitmap"/>.
/// </summary>
/// <remarks>
/// Ported from the original OAA SDK's JMSSIDC. The <see cref="Librarian"/> is
/// created lazily and any failure (e.g. missing JMS configuration/SVG data, or
/// running on a platform where <c>System.Drawing</c> rendering is unavailable)
/// is swallowed so callers simply receive a <c>null</c> bitmap and can fall back
/// to a default rendering, matching the SDK's previous stub contract.
/// </remarks>
internal sealed class JmsSymbol
{
    private static readonly object _sync = new();
    private static Librarian _librarian;
    private static bool _librarianUnavailable;

    private readonly Symbol _jmsSymbol;

    private JmsSymbol(Symbol jmsSymbol) => _jmsSymbol = jmsSymbol;

    /// <summary>
    /// Builds a JMSML-backed symbol from a SIDC string, or returns <c>null</c>
    /// when the SIDC is empty or JMSML data is unavailable. A 20-character (or
    /// longer) code is interpreted as a 2525D Part A + Part B pair; anything
    /// shorter (typically the 15-character legacy code) is treated as 2525C.
    /// </summary>
    public static JmsSymbol FromSidc(string sidc)
    {
        if (string.IsNullOrWhiteSpace(sidc))
            return null;

        var librarian = GetLibrarian();
        if (librarian is null)
            return null;

        try
        {
            Symbol symbol = sidc.Length >= 20
                ? librarian.MakeSymbol(new SIDC(sidc.Substring(0, 10), sidc.Substring(10, 10)))
                : librarian.MakeSymbol("2525C", sidc);
            return symbol is null ? null : new JmsSymbol(symbol);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Renders the symbol to a bitmap of the requested size, or <c>null</c> if
    /// rendering is not possible. Requires the JMSML SVG graphic set to be
    /// reachable via <see cref="StpRecognizer.JMSSVGPath"/>.
    /// </summary>
    public Bitmap Bitmap(int width, int height)
    {
        var librarian = _librarian;
        if (librarian is null || _jmsSymbol is null)
            return null;

        try
        {
            // Refresh the SVG graphic location in case the host set/changed it
            // after the Librarian was first initialized.
            if (!string.IsNullOrEmpty(StpRecognizer.JMSSVGPath))
                librarian.ConfigData.ETLConfig.GraphicHome = StpRecognizer.JMSSVGPath;

            return _jmsSymbol.Bitmap(width, height);
        }
        catch
        {
            return null;
        }
    }

    private static Librarian GetLibrarian()
    {
        if (_librarian != null)
            return _librarian;
        if (_librarianUnavailable)
            return null;

        lock (_sync)
        {
            if (_librarian != null)
                return _librarian;
            if (_librarianUnavailable)
                return null;

            try
            {
                // Config + Instance symbol data are bundled under JMS/ (copied to
                // the consuming app's output). A null logger lets the Librarian
                // fall back to its internal NullLogger.
                _librarian = new Librarian(null, Path.Combine("JMS", "jmsmlSTP.config"));
            }
            catch
            {
                _librarianUnavailable = true;
            }

            return _librarian;
        }
    }
}
