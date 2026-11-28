using ChessDroid.Models;
using ChessDroid.Services;

namespace ChessDroid.Tests.App;

/// <summary>
/// PgnImportService's parsing/formatting helpers (ParseHeaders, TokenizeMoveText, the
/// NAG/symbol mappings, and the CachedAnalysis comment serializer) had zero test coverage
/// before this — only ConvertSanToUci was covered, by PgnRoundTripTests. These are all pure
/// static logic with no WinForms dependency, and TokenizeMoveText in particular is the real
/// parser core AnalysisBoardForm.ImportPgn walks for every PGN file, so it's worth locking
/// down directly rather than only exercising it indirectly through a full import.
/// </summary>
public class PgnImportServiceTests
{
    // ---- check marks come from the position, not the PGN's text

    // Bock–Mackenzie, USA-03.Congress 1874: the record ends "13.Nxh4 Rxd4+", but it's mate.
    private const string BockMackenzie =
        "e4 e5 Nc3 Nc6 f4 exf4 d4 Qh4+ Ke2 d5 exd5 Bg4+ Nf3 O-O-O dxc6 Bc5 Qd3 Nh6 Qb5 Rhe8+ Kd3 bxc6 Qa6+ Kb8 Nxh4";

    [Theory]
    [InlineData("Rxd4+", "Rxd4#")]   // the old record's "+" for mate
    [InlineData("Rxd4", "Rxd4#")]    // mark missing altogether
    [InlineData("Rxd4+!!", "Rxd4#!!")] // annotation kept after the mark
    public void NormalizeCheckSuffix_MateFromAnOldRecord(string written, string expected)
    {
        string before = ChessNotationService.GetFenAfterSanMoves(BockMackenzie);
        string after = ChessNotationService.GetFenAfterSanMoves(BockMackenzie + " Rxd4");
        Assert.NotEqual(before, after);
        Assert.Equal(expected, PgnImportService.NormalizeCheckSuffix(written, after));
    }

    [Theory]
    [InlineData("e4 e5 Nf3 d6 Bc4 Nc6", "Bxf7+", "Bxf7+")]   // a check stays "+"
    [InlineData("e4 e5 Nf3 d6 Bc4 Nc6", "Bxf7", "Bxf7+")]    // missing "+" added
    [InlineData("e4 e5", "Nf3#", "Nf3")]                     // a wrong mark is removed
    [InlineData("e4 e5", "Nf3?!", "Nf3?!")]
    public void NormalizeCheckSuffix_ChecksAndQuietMoves(string moves, string written, string expected)
    {
        string after = ChessNotationService.GetFenAfterSanMoves(moves + " " + written.TrimEnd('+', '#', '!', '?'));
        Assert.Equal(expected, PgnImportService.NormalizeCheckSuffix(written, after));
    }

    private static readonly char[] FourMoveTokenTypes = { 'M', 'M', 'M', 'M' };
    private static readonly string[] FourPlainMoves = { "e4", "e5", "Nf3", "Nc6" };
    private static readonly char[] VariationTokenTypes = { 'M', '(', 'M', 'M', ')', 'M' };
    private static readonly string[] TwoPlainMoves = { "e4", "e5" };

    [Fact]
    public void ParseHeaders_ExtractsQuotedTagValues()
    {
        string pgn = "[Event \"Test Game\"]\n[White \"Alice\"]\n[Black \"Bob\"]\n[Result \"1-0\"]\n\n1. e4 e5 1-0";
        var headers = PgnImportService.ParseHeaders(pgn);

        Assert.Equal("Test Game", headers["Event"]);
        Assert.Equal("Alice", headers["White"]);
        Assert.Equal("Bob", headers["Black"]);
        Assert.Equal("1-0", headers["Result"]);
    }

    [Fact]
    public void ParseHeaders_IgnoresMoveTextLines()
    {
        string pgn = "[Event \"Test\"]\n\n1. e4 e5 2. Nf3 Nc6 *";
        var headers = PgnImportService.ParseHeaders(pgn);

        Assert.Single(headers);
        Assert.Equal("Test", headers["Event"]);
    }

    [Fact]
    public void ParseHeaders_EmptyPgn_ReturnsEmptyDictionary()
        => Assert.Empty(PgnImportService.ParseHeaders(""));

    [Fact]
    public void TokenizeMoveText_PlainMoves_YieldsMoveTokensOnly()
    {
        var tokens = PgnImportService.TokenizeMoveText("1. e4 e5 2. Nf3 Nc6");

        Assert.Equal(FourMoveTokenTypes, tokens.Select(t => t.type));
        Assert.Equal(FourPlainMoves, tokens.Select(t => t.value));
    }

    [Fact]
    public void TokenizeMoveText_StripsResultMarkersAndMoveNumbers()
    {
        var tokens = PgnImportService.TokenizeMoveText("1. e4 e5 1/2-1/2");

        Assert.Equal(2, tokens.Count);
        Assert.All(tokens, t => Assert.Equal('M', t.type));
    }

    [Fact]
    public void TokenizeMoveText_ExtractsCommentAsSeparateToken()
    {
        var tokens = PgnImportService.TokenizeMoveText("1. e4 {Good opening move} e5");

        Assert.Equal(('M', "e4"), tokens[0]);
        Assert.Equal(('C', "Good opening move"), tokens[1]);
        Assert.Equal(('M', "e5"), tokens[2]);
    }

    [Fact]
    public void TokenizeMoveText_UnterminatedComment_ConsumesRestOfText()
    {
        var tokens = PgnImportService.TokenizeMoveText("1. e4 {unterminated comment runs on");

        Assert.Equal('M', tokens[0].type);
        Assert.Equal('C', tokens[1].type);
    }

    [Fact]
    public void TokenizeMoveText_ParsesVariationParenthesesAsOwnTokens()
    {
        var tokens = PgnImportService.TokenizeMoveText("1. e4 (1. d4 d5) e5");

        Assert.Equal(VariationTokenTypes, tokens.Select(t => t.type));
    }

    [Fact]
    public void TokenizeMoveText_ExtractsNagToken()
    {
        var tokens = PgnImportService.TokenizeMoveText("1. e4 $1 e5");

        Assert.Equal(('N', "$1"), tokens[1]);
    }

    [Fact]
    public void TokenizeMoveText_SkipsLineCommentsToEndOfLine()
    {
        var tokens = PgnImportService.TokenizeMoveText("1. e4 ; this is a line comment\ne5");

        Assert.Equal(TwoPlainMoves, tokens.Select(t => t.value));
    }

    [Theory]
    [InlineData("e4!", "e4")]
    [InlineData("Qxf7??", "Qxf7")]
    [InlineData("Nf3!?", "Nf3")]
    [InlineData("Bxc6?!", "Bxc6")]
    [InlineData("e5", "e5")]
    public void StripAnnotationSymbols_RemovesTrailingAnnotation(string san, string expected)
        => Assert.Equal(expected, PgnImportService.StripAnnotationSymbols(san));

    [Theory]
    [InlineData("!!", "$3")]
    [InlineData("!", "$1")]
    [InlineData("!?", "$5")]
    [InlineData("?!", "$6")]
    [InlineData("?", "$2")]
    [InlineData("??", "$4")]
    [InlineData("", "")]
    public void GetNagForSymbol_MapsToExpectedNag(string symbol, string expectedNag)
        => Assert.Equal(expectedNag, PgnImportService.GetNagForSymbol(symbol));

    [Theory]
    [InlineData("$3", "!!")]
    [InlineData("$1", "!")]
    [InlineData("$5", "!?")]
    [InlineData("$6", "?!")]
    [InlineData("$2", "?")]
    [InlineData("$4", "??")]
    [InlineData("$99", "")]
    public void GetSymbolForNag_MapsToExpectedSymbol(string nag, string expectedSymbol)
        => Assert.Equal(expectedSymbol, PgnImportService.GetSymbolForNag(nag));

    [Fact]
    public void NagAndSymbolMappings_AreInverses()
    {
        foreach (var symbol in new[] { "!!", "!", "!?", "?!", "?", "??" })
        {
            string nag = PgnImportService.GetNagForSymbol(symbol);
            Assert.Equal(symbol, PgnImportService.GetSymbolForNag(nag));
        }
    }

    [Theory]
    [InlineData("!!", MoveQualityAnalyzer.MoveQuality.Brilliant)]
    [InlineData("!", MoveQualityAnalyzer.MoveQuality.Precise)]
    [InlineData("?!", MoveQualityAnalyzer.MoveQuality.Inaccuracy)]
    [InlineData("?", MoveQualityAnalyzer.MoveQuality.Mistake)]
    [InlineData("??", MoveQualityAnalyzer.MoveQuality.Blunder)]
    [InlineData("", MoveQualityAnalyzer.MoveQuality.Best)]
    public void GetQualityForSymbol_MapsToExpectedMoveQuality(string symbol, MoveQualityAnalyzer.MoveQuality expected)
        => Assert.Equal(expected, PgnImportService.GetQualityForSymbol(symbol));

    [Fact]
    public void GetInlineSymbol_ReturnsLongestMatchingTrailingSymbol()
    {
        Assert.Equal("??", PgnImportService.GetInlineSymbol("Qxf7??"));
        Assert.Equal("!?", PgnImportService.GetInlineSymbol("Nf3!?"));
        Assert.Equal("", PgnImportService.GetInlineSymbol("e5"));
    }

    [Fact]
    public void BuildPgnComment_FormatsEvalAndQualityLabel()
    {
        var result = new MoveReviewResult
        {
            EvalAfter = 1.25,
            Quality = MoveQualityAnalyzer.MoveQuality.Blunder,
        };

        Assert.Equal("{ [+1.25] Blunder }", PgnImportService.BuildPgnComment(result));
    }

    [Fact]
    public void BuildPgnComment_FormatsNegativeEval()
    {
        var result = new MoveReviewResult
        {
            EvalAfter = -0.4,
            Quality = MoveQualityAnalyzer.MoveQuality.Best,
        };

        Assert.Equal("{ [-0.40] Best }", PgnImportService.BuildPgnComment(result));
    }

    [Theory]
    [InlineData("{ [+1.25] Blunder }", 1.25)]
    [InlineData("{ [-0.40] Best }", -0.40)]
    [InlineData("No eval here", null)]
    public void ParseEvalFromComment_ExtractsBracketedNumber(string comment, double? expected)
        => Assert.Equal(expected, PgnImportService.ParseEvalFromComment(comment));

    [Theory]
    [InlineData("{ [+1.25] Blunder }", MoveQualityAnalyzer.MoveQuality.Blunder)]
    [InlineData("{ [+0.00] Brilliant }", MoveQualityAnalyzer.MoveQuality.Brilliant)]
    [InlineData("{ [+0.00] Mistake }", MoveQualityAnalyzer.MoveQuality.Mistake)]
    [InlineData("{ [+0.00] Inaccuracy }", MoveQualityAnalyzer.MoveQuality.Inaccuracy)]
    [InlineData("No quality keyword here", MoveQualityAnalyzer.MoveQuality.Best)]
    public void ParseQualityFromComment_MatchesKnownLabel(string comment, MoveQualityAnalyzer.MoveQuality expected)
        => Assert.Equal(expected, PgnImportService.ParseQualityFromComment(comment));

    [Fact]
    public void SerializeThenDeserializeCachedAnalysis_RoundTrips()
    {
        var original = new CachedAnalysis
        {
            Depth = 20,
            BestMove = "e2e4",
            Evaluation = "+0.35",
            PVs = new List<string> { "e2e4 e7e5", "d2d4 d7d5" },
            Evaluations = new List<string> { "+0.35", "+0.20" },
            WDL = new WDLInfo(450, 400, 150),
        };

        // SerializeCachedAnalysis wraps its output as a full PGN comment token ("{ ... }"),
        // but DeserializeCachedAnalysis expects the bracket content with the braces already
        // stripped — exactly what TokenizeMoveText's 'C' tokens hand it in real usage. Round
        // the output through the tokenizer instead of stripping braces by hand, so this
        // exercises the real production pipeline, not an assumption about its string shape.
        string comment = PgnImportService.SerializeCachedAnalysis(original);
        var tokens = PgnImportService.TokenizeMoveText(comment);
        string inner = tokens.Single(t => t.type == 'C').value;
        var roundTripped = PgnImportService.DeserializeCachedAnalysis(inner);

        Assert.NotNull(roundTripped);
        Assert.Equal(original.Depth, roundTripped!.Depth);
        Assert.Equal(original.BestMove, roundTripped.BestMove);
        Assert.Equal(original.Evaluation, roundTripped.Evaluation);
        Assert.Equal(original.PVs, roundTripped.PVs);
        Assert.Equal(original.Evaluations, roundTripped.Evaluations);
        Assert.NotNull(roundTripped.WDL);
        Assert.Equal(original.WDL!.Win, roundTripped.WDL!.Win);
        Assert.Equal(original.WDL.Draw, roundTripped.WDL.Draw);
        Assert.Equal(original.WDL.Loss, roundTripped.WDL.Loss);
    }

    [Fact]
    public void SerializeCachedAnalysis_OmitsEmptyOptionalFields()
    {
        var minimal = new CachedAnalysis { Depth = 10 };

        string comment = PgnImportService.SerializeCachedAnalysis(minimal);

        Assert.Equal("{ [%cda d=10] }", comment);
    }

    [Fact]
    public void DeserializeCachedAnalysis_NotCdaFormat_ReturnsNull()
        => Assert.Null(PgnImportService.DeserializeCachedAnalysis("{ just a normal comment }"));

    [Fact]
    public void DeserializeCachedAnalysis_MissingDepth_ReturnsNull()
    {
        // Depth defaults to 0 when the "d=" field is absent; DeserializeCachedAnalysis
        // treats Depth <= 0 as "not a real cached-analysis comment" and returns null.
        var result = PgnImportService.DeserializeCachedAnalysis("[%cda b=e2e4]");
        Assert.Null(result);
    }
}
