namespace ChessDroid.Services
{
    /// <summary>
    /// Result of tokenizing one UCI "info ... pv ..." line. Fields are null/empty when the
    /// line didn't carry that piece of data (e.g. no "wdl" token). WDL is exactly as the
    /// engine sent it (W D L, side-to-move's perspective) — callers that need White's
    /// perspective flip it themselves, since the two existing call sites disagreed on this
    /// before the consolidation and this extraction is behavior-preserving, not a fix.
    /// </summary>
    internal readonly record struct UciPvLine(int? Depth, int MultiPv, string Eval, WDLInfo? Wdl, List<string> PvMoves);

    /// <summary>
    /// Shared tokenizer for the "info ... score ... pv ..." line shape, factored out of
    /// GetBestMoveAsync and RunContinuousAnalysisAsync (ChessEngineService.cs) — the two
    /// methods that need full multipv/depth/WDL extraction, not just a single eval string
    /// (that simpler case already went through the shared ParseEvalFromInfoLine). Each
    /// caller keeps its own buffering/aggregation/perspective logic; only the token-walking
    /// loop itself (CHESSDROID_AUDIT.md §5.1's "5 copies of the UCI parser") is shared here.
    /// </summary>
    internal static class UciInfoLineParser
    {
        internal static UciPvLine ParsePvLine(string line, bool whiteToMove)
        {
            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            int? depth = null;
            int mpv = 1;
            string eval = "";
            WDLInfo? wdl = null;
            var pvMoves = new List<string>();

            for (int i = 0; i < tokens.Length; i++)
            {
                if (tokens[i] == "depth" && i + 1 < tokens.Length && int.TryParse(tokens[i + 1], out int d))
                    depth = d;

                if (tokens[i] == "multipv" && i + 1 < tokens.Length && int.TryParse(tokens[i + 1], out int m))
                    mpv = m;

                if (tokens[i] == "score" && i + 2 < tokens.Length)
                {
                    if (tokens[i + 1] == "cp" && double.TryParse(tokens[i + 2], out double cp))
                    {
                        double displayCp = whiteToMove ? cp : -cp;
                        eval = Models.Score.FromCentipawns(displayCp).ToString();
                    }
                    else if (tokens[i + 1] == "mate" && int.TryParse(tokens[i + 2], out int mateIn))
                    {
                        int displayMate = whiteToMove ? mateIn : -mateIn;
                        eval = Models.Score.FromMate(displayMate).ToString();
                    }
                }

                if (tokens[i] == "wdl" && i + 3 < tokens.Length && mpv == 1 &&
                    int.TryParse(tokens[i + 1], out int w) &&
                    int.TryParse(tokens[i + 2], out int dr) &&
                    int.TryParse(tokens[i + 3], out int l))
                {
                    wdl = new WDLInfo(w, dr, l);
                }

                if (tokens[i] == "pv")
                {
                    for (int j = i + 1; j < tokens.Length; j++)
                        pvMoves.Add(tokens[j]);
                    break;
                }
            }

            return new UciPvLine(depth, mpv, eval, wdl, pvMoves);
        }
    }
}
