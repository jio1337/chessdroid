namespace ChessDroid.Tests.App;

/// <summary>
/// PgnGamePickerDialog's static parsing helpers (SplitPgnGames, ParsePgnHeaders, CountMoves)
/// had zero test coverage before this. Found and fixed a real bug in SplitPgnGames while
/// reviewing it: it decided "a new game's headers are starting" from a bare
/// `StartsWith('[')`, but Lichess/chess.com-style move annotations like [%eval 0.31] or
/// [%clk 0:05:00] also start and end with brackets. If a long move-text line word-wraps so one
/// of those lands alone at the start of a physical line, the old check would misfire and split
/// one real game into two bogus fragments mid-game. Fixed by also requiring a quote — real PGN
/// tags are always [Name "value"], %-annotations never are.
/// </summary>
public class PgnGamePickerDialogTests
{
    private const string GameOneHeaders =
        "[Event \"Test Event\"]\n[White \"Alice\"]\n[Black \"Bob\"]\n[Result \"1-0\"]\n\n";
    private const string GameTwoHeaders =
        "[Event \"Test Event\"]\n[White \"Carol\"]\n[Black \"Dave\"]\n[Result \"0-1\"]\n\n";

    [Fact]
    public void SplitPgnGames_SingleGame_ReturnsOneEntry()
    {
        string pgn = GameOneHeaders + "1. e4 e5 2. Nf3 Nc6 1-0";
        var games = PgnGamePickerDialog.SplitPgnGames(pgn);
        Assert.Single(games);
    }

    [Fact]
    public void SplitPgnGames_TwoGames_ReturnsTwoEntries()
    {
        string pgn = GameOneHeaders + "1. e4 e5 1-0\n\n" + GameTwoHeaders + "1. d4 d5 0-1";
        var games = PgnGamePickerDialog.SplitPgnGames(pgn);

        Assert.Equal(2, games.Count);
        Assert.Contains("Alice", games[0]);
        Assert.Contains("Carol", games[1]);
    }

    [Fact]
    public void SplitPgnGames_EmptyString_ReturnsEmptyList()
        => Assert.Empty(PgnGamePickerDialog.SplitPgnGames(""));

    [Fact]
    public void SplitPgnGames_WrappedEvalAnnotationLine_DoesNotSplitMidGame()
    {
        // Simulates a word-wrapped move-text line where a Lichess-style [%eval] annotation
        // lands alone at the start of a physical line — this used to look exactly like a new
        // game's header to the old StartsWith('[')-only check.
        string pgn = GameOneHeaders +
            "1. e4 e5 2. Nf3 Nc6 3. Bb5 a6\n" +
            "[%eval 0.31]\n" +
            "4. Ba4 Nf6 1-0";

        var games = PgnGamePickerDialog.SplitPgnGames(pgn);

        Assert.Single(games);
        Assert.Contains("[%eval 0.31]", games[0]);
        Assert.Contains("4. Ba4 Nf6", games[0]);
    }

    [Fact]
    public void SplitPgnGames_WrappedClkAnnotationLine_DoesNotSplitMidGame()
    {
        string pgn = GameOneHeaders +
            "1. e4 e5 2. Nf3 Nc6\n" +
            "[%clk 0:05:00]\n" +
            "3. Bb5 a6 1-0";

        var games = PgnGamePickerDialog.SplitPgnGames(pgn);
        Assert.Single(games);
    }

    [Fact]
    public void SplitPgnGames_RealHeaderAfterAnnotationLine_StillSplitsCorrectly()
    {
        // The annotation-line fix must not break the normal multi-game case: a genuine new
        // header block must still end the previous game, even if that game's move text also
        // contained a bracketed annotation line earlier.
        string pgn = GameOneHeaders +
            "1. e4 e5\n[%eval 0.31]\n2. Nf3 Nc6 1-0\n\n" +
            GameTwoHeaders + "1. d4 d5 0-1";

        var games = PgnGamePickerDialog.SplitPgnGames(pgn);

        Assert.Equal(2, games.Count);
        Assert.Contains("Alice", games[0]);
        Assert.Contains("Carol", games[1]);
    }

    [Fact]
    public void ParsePgnHeaders_ExtractsTagsCaseInsensitively()
    {
        var headers = PgnGamePickerDialog.ParsePgnHeaders(GameOneHeaders);

        Assert.Equal("Alice", headers["White"]);
        Assert.Equal("Alice", headers["white"]);  // case-insensitive lookup
        Assert.Equal("Bob", headers["Black"]);
        Assert.Equal("1-0", headers["Result"]);
    }

    [Fact]
    public void ParsePgnHeaders_StopsAtFirstMoveTextLine()
    {
        string pgn = GameOneHeaders + "1. e4 e5\n[%eval 0.31]\n2. Nf3 Nc6 1-0";
        var headers = PgnGamePickerDialog.ParsePgnHeaders(pgn);

        // Only the real header tags before move text starts; the embedded [%eval] annotation
        // line is movetext, not a header, and must not corrupt the headers dictionary.
        Assert.Equal(4, headers.Count);
    }

    // Search box (picker + Game Library): every word must appear in some field, any case.
    [Theory]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("bock", true)]                       // part of a name, lower case
    [InlineData("mackenzie bock", true)]             // words may come from different fields
    [InlineData("bock 1874", true)]                  // date/event fields count too
    [InlineData("bock paulsen", false)]              // every word must match
    [InlineData("steinitz", false)]
    public void MatchesSearch_AllWordsMustMatchSomeField(string query, bool expected)
        => Assert.Equal(expected, PgnGamePickerDialog.MatchesSearch(query,
            "Bock, Frederic", "Mackenzie, George Henry", "1874", "USA-03.Congress"));

    [Fact]
    public void MatchesSearch_IgnoresNullFields()
        => Assert.True(PgnGamePickerDialog.MatchesSearch("bock", null, "Bock, Frederic"));

    [Theory]
    [InlineData("1. e4 e5 2. Nf3 Nc6", 2)]
    [InlineData("1. e4 e5 2. Nf3 Nc6 3. Bb5 a6 1-0", 3)]
    [InlineData("*", 0)]
    public void CountMoves_ReturnsHighestMoveNumber(string moveText, int expected)
    {
        string pgn = GameOneHeaders + moveText;
        Assert.Equal(expected, PgnGamePickerDialog.CountMoves(pgn));
    }
}
