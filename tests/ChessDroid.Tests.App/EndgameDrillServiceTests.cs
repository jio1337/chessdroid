using ChessDroid.Services;

namespace ChessDroid.Tests.App;

/// <summary>
/// EndgameDrillService.ParsePgn had zero test coverage despite being the thing that turns
/// arbitrary user-dropped .pgn files into drill positions — a malformed or unusually-shaped
/// file fails silently (missing FEN -> chapter skipped, nothing else validated) rather than
/// throwing, so these tests lock down what today's parser actually accepts/produces.
/// </summary>
public class EndgameDrillServiceTests : IDisposable
{
    private static readonly string[] ExpectedDistinctStudyNames = { "Study A", "Study B" };

    private readonly string _tempDir;

    public EndgameDrillServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "chessdroid-drill-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
        GC.SuppressFinalize(this);
    }

    private string WritePgn(string fileName, string content)
    {
        string path = Path.Combine(_tempDir, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void ParsePgn_TwoChapters_ParsesBothWithTagsAndDescription()
    {
        string path = WritePgn("studies.pgn", """
            [Event "Chapter: KPK Opposition"]
            [Site "https://lichess.org/study/abc123"]
            [StudyName "King and Pawn Endgames"]
            [ChapterName "KPK Opposition"]
            [FEN "8/8/8/4k3/4P3/4K3/8/8 w - - 0 1"]
            [SetUp "1"]

            1. Kd3 { The key idea is opposition. [%cal Ge3d3] } Kd5 2. e5 Ke6 *

            [Event "Chapter: Second position"]
            [StudyName "King and Pawn Endgames"]
            [FEN "8/8/8/8/4k3/8/4P3/4K3 b - - 0 1"]

            1... Kd4 *
            """);

        var chapters = EndgameDrillService.ParsePgn(path);

        Assert.Equal(2, chapters.Count);

        var first = chapters[0];
        Assert.Equal("King and Pawn Endgames", first.StudyName);
        Assert.Equal("KPK Opposition", first.ChapterName);
        Assert.Equal("8/8/8/4k3/4P3/4K3/8/8 w - - 0 1", first.Fen);
        Assert.True(first.WhiteToMove);
        Assert.Equal("The key idea is opposition.", first.Description);

        var second = chapters[1];
        Assert.Equal("8/8/8/8/4k3/8/4P3/4K3 b - - 0 1", second.Fen);
        Assert.False(second.WhiteToMove);
        Assert.Equal("", second.Description);
    }

    [Fact]
    public void ParsePgn_ChapterMissingFenTag_IsSkipped()
    {
        string path = WritePgn("nofen.pgn", """
            [Event "Chapter: Has FEN"]
            [FEN "8/8/8/4k3/4P3/4K3/8/8 w - - 0 1"]

            1. Kd3 *

            [Event "Chapter: No FEN"]

            1. e4 *
            """);

        var chapters = EndgameDrillService.ParsePgn(path);

        Assert.Single(chapters);
        Assert.Equal("Chapter: Has FEN", chapters[0].ChapterName);
    }

    [Fact]
    public void ParsePgn_MissingOptionalTags_FallsBackToDefaults()
    {
        string path = WritePgn("minimal.pgn", """
            [FEN "8/8/8/4k3/4P3/4K3/8/8 w - - 0 1"]

            1. Kd3 *
            """);

        var chapters = EndgameDrillService.ParsePgn(path);

        Assert.Single(chapters);
        Assert.Equal("Position", chapters[0].ChapterName); // no ChapterName or Event tag
        Assert.Equal("Drills", chapters[0].StudyName);     // no StudyName tag
    }

    [Fact]
    public void ParsePgn_ChapterNameFallsBackToEventTag()
    {
        string path = WritePgn("event-fallback.pgn", """
            [Event "Rook Endgame Basics"]
            [FEN "8/8/8/4k3/4P3/4K3/8/8 w - - 0 1"]

            1. Kd3 *
            """);

        var chapters = EndgameDrillService.ParsePgn(path);

        Assert.Single(chapters);
        Assert.Equal("Rook Endgame Basics", chapters[0].ChapterName);
    }

    [Fact]
    public void ParsePgn_DescriptionWithTrailingArrowAnnotation_StripsLichessMarkup()
    {
        string path = WritePgn("annotated.pgn", """
            [Event "Chapter"]
            [FEN "8/8/8/4k3/4P3/4K3/8/8 w - - 0 1"]

            1. Kd3 { Good square for the king [%cal Gd3e3] } *
            """);

        var chapters = EndgameDrillService.ParsePgn(path);

        Assert.Single(chapters);
        Assert.Equal("Good square for the king", chapters[0].Description);
    }

    [Fact]
    public void ParsePgn_NoCommentInMoveText_DescriptionIsEmpty()
    {
        string path = WritePgn("nocomment.pgn", """
            [Event "Chapter"]
            [FEN "8/8/8/4k3/4P3/4K3/8/8 w - - 0 1"]

            1. Kd3 Kd5 2. e5 *
            """);

        var chapters = EndgameDrillService.ParsePgn(path);

        Assert.Single(chapters);
        Assert.Equal("", chapters[0].Description);
    }

    [Fact]
    public void ParsePgn_ThankYouTrailerChapter_IsSkipped()
    {
        string path = WritePgn("trailer.pgn", """
            [Event "Real Chapter"]
            [FEN "8/8/8/4k3/4P3/4K3/8/8 w - - 0 1"]

            1. Kd3 *

            [Event "Credits"]
            [ChapterName "Thanks for enjoying this study!"]
            [FEN "8/8/8/4k3/4P3/4K3/8/8 w - - 0 1"]

            { Thanks for following along! https://lichess.org/study/abc123 } *
            """);

        var chapters = EndgameDrillService.ParsePgn(path);

        Assert.Single(chapters);
        Assert.Equal("Real Chapter", chapters[0].ChapterName);
    }

    [Fact]
    public void ParsePgn_ChapterWithNoBlackKing_IsSkipped()
    {
        string path = WritePgn("nokingblack.pgn", """
            [Event "Real Chapter"]
            [FEN "8/8/8/4k3/4P3/4K3/8/8 w - - 0 1"]

            1. Kd3 *

            [Event "Know what can mate!"]
            [FEN "Q1R1B1B1/4B1N1/8/8/8/8/8/4K3 w - - 0 1"]

            { Illustrative diagram only, not a real position. } *
            """);

        var chapters = EndgameDrillService.ParsePgn(path);

        Assert.Single(chapters);
        Assert.Equal("Real Chapter", chapters[0].ChapterName);
    }

    [Fact]
    public void ParsePgn_ChapterWithNoWhiteKing_IsSkipped()
    {
        string path = WritePgn("nokingwhite.pgn", """
            [Event "No White King"]
            [FEN "4k3/8/8/8/8/8/8/8 w - - 0 1"]

            *
            """);

        var chapters = EndgameDrillService.ParsePgn(path);

        Assert.Empty(chapters);
    }

    [Fact]
    public void HasDrills_NoFolder_ReturnsFalse()
    {
        Assert.False(EndgameDrillService.HasDrills(Path.Combine(_tempDir, "does-not-exist")));
    }

    [Fact]
    public void HasDrills_FolderWithNoPgnFiles_ReturnsFalse()
    {
        File.WriteAllText(Path.Combine(_tempDir, "readme.txt"), "not a pgn");
        Assert.False(EndgameDrillService.HasDrills(_tempDir));
    }

    [Fact]
    public void HasDrills_FolderWithPgnFile_ReturnsTrue()
    {
        WritePgn("studies.pgn", "[Event \"x\"]\n[FEN \"8/8/8/4k3/4P3/4K3/8/8 w - - 0 1\"]\n\n*");
        Assert.True(EndgameDrillService.HasDrills(_tempDir));
    }

    [Fact]
    public void LoadFromFolder_NonexistentFolder_ReturnsEmptyList()
    {
        var chapters = EndgameDrillService.LoadFromFolder(Path.Combine(_tempDir, "missing"));
        Assert.Empty(chapters);
    }

    [Fact]
    public void LoadFromFolder_MultipleFiles_AggregatesChaptersFromAll()
    {
        WritePgn("a.pgn", """
            [Event "A1"]
            [FEN "8/8/8/4k3/4P3/4K3/8/8 w - - 0 1"]

            *
            """);
        WritePgn("b.pgn", """
            [Event "B1"]
            [FEN "8/8/8/8/4k3/8/4P3/4K3 b - - 0 1"]

            *
            """);

        var chapters = EndgameDrillService.LoadFromFolder(_tempDir);

        Assert.Equal(2, chapters.Count);
        Assert.Contains(chapters, c => c.ChapterName == "A1");
        Assert.Contains(chapters, c => c.ChapterName == "B1");
    }

    [Fact]
    public void LoadFromFolder_RealShippedDrills_HasNoTrailerOrDegenerateChapters()
    {
        string drillsFolder = Path.Combine(AppContext.BaseDirectory, "Drills");
        var chapters = EndgameDrillService.LoadFromFolder(drillsFolder);

        Assert.NotEmpty(chapters);
        Assert.DoesNotContain(chapters, c => c.ChapterName == "Thanks for enjoying this study!");
        Assert.DoesNotContain(chapters, c => c.ChapterName == "Know what can mate!");
    }

    [Fact]
    public void GetStudyNames_ReturnsDistinctNamesOnly()
    {
        var chapters = new List<ChessDroid.Services.EndgameChapter>
        {
            new("Study A", "Ch1", "8/8/8/4k3/4P3/4K3/8/8 w - - 0 1", "", true),
            new("Study A", "Ch2", "8/8/8/4k3/4P3/4K3/8/8 w - - 0 1", "", true),
            new("Study B", "Ch1", "8/8/8/4k3/4P3/4K3/8/8 w - - 0 1", "", true),
        };

        var names = EndgameDrillService.GetStudyNames(chapters);

        Assert.Equal(ExpectedDistinctStudyNames, names);
    }
}
