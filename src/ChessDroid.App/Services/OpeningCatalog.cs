namespace ChessDroid.Services
{
    /// <summary>
    /// CHESSDROID_AUDIT.md §3.4/§6.3 named three independent "opening name" systems
    /// (OpeningBook, OpeningDatabase, EcoBookService) that should sit behind one catalog.
    /// A real investigation (not just code-shape reading) found this isn't a simple
    /// "merge three copies of the same thing": OpeningBook's ~500 hand-curated entries and
    /// the ECO JSON data genuinely disagree on naming for some positions (e.g. "Grob Attack"
    /// vs. "Grob Opening" for the same line), and OpeningBook/OpeningDatabase answer a
    /// different *shape* of question (single-position FEN lookup, transpositions collapse to
    /// one name) than EcoBookService (flat move-sequence-driven browse list for picker UIs —
    /// collapsing it by position first would silently drop differently-named transpositions,
    /// per EcoFileCache's own doc comment). Deliberately NOT re-deciding which name wins for
    /// conflicting positions here — that changes what users see and deserves its own review,
    /// not a silent pick made while chasing a structural-hygiene audit item.
    ///
    /// What this class does: give every call site one place to ask either question, with the
    /// exact same precedence/behavior as before (OpeningBook's curated entries still win,
    /// OpeningDatabase/ECO JSON is still the fallback, EcoBookService's browse list is
    /// untouched) — so the next reader finds one documented entry point instead of three
    /// scattered systems, with zero change to any displayed name.
    /// </summary>
    public static class OpeningCatalog
    {
        /// <summary>Single-position lookup, keyed by piece placement — curated entries win, ECO JSON is the fallback.</summary>
        public static string GetOpeningDisplay(string fen) => OpeningBook.GetOpeningDisplay(fen);

        /// <summary>Flat, (Eco,Name)-deduped browse list for picker UIs / random-pick — not FEN-keyed.</summary>
        public static List<OpeningEntry> Browse(string booksFolder) => EcoBookService.LoadAll(booksFolder);
    }
}
