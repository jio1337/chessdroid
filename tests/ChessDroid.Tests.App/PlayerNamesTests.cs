namespace ChessDroid.Tests.App;

/// <summary>
/// Player names on the board (the labels on the material strips) for a loaded PGN, and the
/// PGN headers written back out. Exports used to replace a loaded game's Event/Site/Date/Round
/// with "Chess Analysis" and today's date and drop the Elos — an 1874 game saved from the board
/// came back dated today — and engine-match exports carried no Elo at all.
/// </summary>
public class PlayerNamesTests
{
    [Theory]
    [InlineData("Mackenzie, George Henry", null, "Mackenzie, George Henry")]
    [InlineData("Carlsen, Magnus", "2830", "Carlsen, Magnus [2830]")]
    [InlineData(" Bock, Frederic ", "?", "Bock, Frederic")]      // non-numeric Elo is left out
    [InlineData("Paulsen, Louis", "0", "Paulsen, Louis")]
    [InlineData("?", "2700", null)]                                  // PGN's "unknown player"
    [InlineData("", null, null)]
    [InlineData(null, null, null)]
    public void PlayerLabel_FromPgnHeaders(string? name, string? elo, string? expected)
        => Assert.Equal(expected, AnalysisBoardForm.PlayerLabel(name, elo));

    private static readonly DateTime Today = new(2026, 11, 28);

    [Fact]
    public void Export_KeepsTheLoadedGamesHeaders()
    {
        var imported = new Dictionary<string, string>
        {
            ["Event"] = "USA-03.Congress", ["Site"] = "Chicago", ["Date"] = "1874.07.??", ["Round"] = "1",
            ["WhiteElo"] = "2400", ["BlackElo"] = "?",
        };
        var lines = AnalysisBoardForm.PgnHeaderLines(imported, "Bock, Frederic", "Mackenzie, George Henry", 0, 0, Today).ToList();

        Assert.Equal(new[]
        {
            "[Event \"USA-03.Congress\"]", "[Site \"Chicago\"]", "[Date \"1874.07.??\"]", "[Round \"1\"]",
            "[White \"Bock, Frederic\"]", "[Black \"Mackenzie, George Henry\"]", "[Result \"*\"]",
            "[WhiteElo \"2400\"]",
        }, lines);
    }

    [Fact]
    public void Export_WithoutLoadedHeaders_UsesDefaults()
    {
        var lines = AnalysisBoardForm.PgnHeaderLines(new Dictionary<string, string>(), "?", "?", 0, 0, Today).ToList();

        Assert.Equal("[Event \"Chess Analysis\"]", lines[0]);
        Assert.Equal("[Date \"2026.11.28\"]", lines[2]);
        Assert.DoesNotContain(lines, l => l.Contains("Elo"));
    }

    [Fact]
    public void Export_EngineMatch_WritesTheEnginesElo()
    {
        var lines = AnalysisBoardForm.PgnHeaderLines(new Dictionary<string, string>(), "stockfish18", "komodo", 3200, 3100, Today);

        Assert.Contains("[WhiteElo \"3200\"]", lines);
        Assert.Contains("[BlackElo \"3100\"]", lines);
    }
}
