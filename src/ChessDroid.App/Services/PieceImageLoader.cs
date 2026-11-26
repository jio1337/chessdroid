using System.Diagnostics;

namespace ChessDroid.Services
{
    /// <summary>
    /// CHESSDROID_AUDIT.md §3.4 flagged repeated SVG parsing across piece-image consumers.
    /// Checked first: ChessBoardControl.SetTemplateSet already early-returns when the
    /// requested set matches the current one, and MaterialStripControl never parses SVG at
    /// all (PNG-only, falling back to another set's PNG when the chosen set has none) — so
    /// the specific "re-parses 12 SVGs per set change, three times over" perf claim doesn't
    /// reproduce against the current code. What *is* real: ChessBoardControl and
    /// PositionEditorForm each had their own copy of the PNG/SVG-with-fallback loading logic
    /// (including the same filter-attribute-stripping regex) — pure code duplication, not a
    /// perf bug. Consolidated here; each caller still owns and disposes its own Image
    /// instances at its own render size (256px for the board, 128px for the position editor's
    /// palette), so this is not a shared cache — just one implementation instead of two.
    /// </summary>
    internal static class PieceImageLoader
    {
        private static readonly System.Text.RegularExpressions.Regex SvgFilterAttr =
            new(@"\s+filter=""url\([^)]+\)""", System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>
        /// Loads one piece image: PNG if present, else SVG (Lichess "wK.svg" or PyChess
        /// "wk.svg" naming), rendered at renderSize×renderSize. Null if neither exists or
        /// loading failed — callers already treat a missing piece image as non-fatal.
        /// </summary>
        internal static Image? LoadPieceImage(string baseDir, string stem, int renderSize)
        {
            string png = Path.Combine(baseDir, stem + ".png");
            if (File.Exists(png))
            {
                try { return Image.FromFile(png); }
                catch (Exception ex) { Debug.WriteLine($"Failed to load {png}: {ex.Message}"); }
            }

            string svg = Path.Combine(baseDir, stem + ".svg");
            if (File.Exists(svg))
                return RenderSvg(svg, renderSize);

            string svgLower = Path.Combine(baseDir, stem.ToLowerInvariant() + ".svg");
            if (File.Exists(svgLower))
                return RenderSvg(svgLower, renderSize);

            Debug.WriteLine($"Piece not found: {stem} in {baseDir}");
            return null;
        }

        private static Bitmap? RenderSvg(string path, int renderSize)
        {
            try
            {
                // Strip filter="url(#...)" attributes before rendering — the Svg library
                // mishandles chained filter primitives (feComposite/feFlood/feOffset) and
                // produces garbage output. Pieces render correctly without the drop-shadow filters.
                string xml = File.ReadAllText(path);
                xml = SvgFilterAttr.Replace(xml, "");
                var doc = Svg.SvgDocument.FromSvg<Svg.SvgDocument>(xml);
                return doc.Draw(renderSize, renderSize);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to render SVG {path}: {ex.Message}");
                return null;
            }
        }
    }
}
