using ChessDroid.Controls;
using ChessDroid.Models;

namespace ChessDroid.Tests.App;

/// <summary>
/// Tournament mode never updated the persistent Chessdroid rating ladder (EngineProfiles'
/// ChessdroidElo) before this — only the 1v1 "Engine Match" feature
/// (AnalysisBoardForm.Match.cs) did. Added a "Update Chessdroid ratings after finishing"
/// checkbox (defaulted on) to TournamentForm, wired through MatchBoardPanel per game. The
/// actual per-game math is MatchBoardPanel.ComputeRatingUpdate — a pure static method
/// extracted specifically so it's testable without constructing the WinForms panel, mirroring
/// exactly what AnalysisBoardForm.Match.cs's MatchService_OnMatchEnded already does for 1v1
/// matches (same K=32 EloCalculator.EloChangeChessdroid, same CCRL-Elo-seed gating).
/// </summary>
public class MatchBoardPanelRatingTests
{
    private static EngineProfile Profile(string name, int elo, int chessdroidElo = 0, int gamesPlayed = 0) => new()
    {
        DisplayName = name,
        Elo = elo,
        ChessdroidElo = chessdroidElo,
        GamesPlayed = gamesPlayed,
    };

    [Fact]
    public void WhiteWin_IncreasesWhiteRatingAndDecreasesBlack()
    {
        var white = Profile("Engine A", 1500, chessdroidElo: 1500);
        var black = Profile("Engine B", 1500, chessdroidElo: 1500);

        var result = MatchBoardPanel.ComputeRatingUpdate(white, black, whiteScore: 1.0);

        Assert.NotNull(result);
        var (wOld, bOld, wNew, bNew) = result!.Value;
        Assert.Equal(1500, wOld);
        Assert.Equal(1500, bOld);
        Assert.True(wNew.ChessdroidElo > wOld);
        Assert.True(bNew.ChessdroidElo < bOld);
        Assert.Equal(16, wNew.ChessdroidElo - wOld);   // K=32 * (1 - 0.5)
        Assert.Equal(-16, bNew.ChessdroidElo - bOld);
    }

    [Fact]
    public void Draw_BetweenEquallyRatedEngines_LeavesRatingsUnchanged()
    {
        var white = Profile("Engine A", 1500, chessdroidElo: 1500);
        var black = Profile("Engine B", 1500, chessdroidElo: 1500);

        var result = MatchBoardPanel.ComputeRatingUpdate(white, black, whiteScore: 0.5);

        Assert.NotNull(result);
        var (wOld, bOld, wNew, bNew) = result!.Value;
        Assert.Equal(wOld, wNew.ChessdroidElo);
        Assert.Equal(bOld, bNew.ChessdroidElo);
    }

    [Fact]
    public void BlackWin_IncreasesBlackRatingAndDecreasesWhite()
    {
        var white = Profile("Engine A", 1500, chessdroidElo: 1500);
        var black = Profile("Engine B", 1500, chessdroidElo: 1500);

        var result = MatchBoardPanel.ComputeRatingUpdate(white, black, whiteScore: 0.0);

        Assert.NotNull(result);
        var (_, _, wNew, bNew) = result!.Value;
        Assert.True(wNew.ChessdroidElo < 1500);
        Assert.True(bNew.ChessdroidElo > 1500);
    }

    [Fact]
    public void FirstGame_SeedsFromCcrlEloWhenChessdroidEloNotYetSet()
    {
        var white = Profile("Engine A", 1800, chessdroidElo: 0);
        var black = Profile("Engine B", 1600, chessdroidElo: 0);

        var result = MatchBoardPanel.ComputeRatingUpdate(white, black, whiteScore: 0.5);

        Assert.NotNull(result);
        var (wOld, bOld, _, _) = result!.Value;
        Assert.Equal(1800, wOld);  // seeded from CCRL Elo, not 0
        Assert.Equal(1600, bOld);
    }

    [Fact]
    public void AlreadySeeded_PrefersChessdroidEloOverCcrlElo()
    {
        var white = Profile("Engine A", 1800, chessdroidElo: 1850);
        var black = Profile("Engine B", 1600, chessdroidElo: 1550);

        var result = MatchBoardPanel.ComputeRatingUpdate(white, black, whiteScore: 0.5);

        Assert.NotNull(result);
        var (wOld, bOld, _, _) = result!.Value;
        Assert.Equal(1850, wOld);
        Assert.Equal(1550, bOld);
    }

    [Fact]
    public void IncrementsGamesPlayedForBothEngines()
    {
        var white = Profile("Engine A", 1500, chessdroidElo: 1500, gamesPlayed: 3);
        var black = Profile("Engine B", 1500, chessdroidElo: 1500, gamesPlayed: 7);

        var result = MatchBoardPanel.ComputeRatingUpdate(white, black, whiteScore: 1.0);

        Assert.NotNull(result);
        var (_, _, wNew, bNew) = result!.Value;
        Assert.Equal(4, wNew.GamesPlayed);
        Assert.Equal(8, bNew.GamesPlayed);
    }

    [Fact]
    public void PreservesDisplayNameAndCcrlEloAcrossUpdate()
    {
        var white = Profile("Engine A", 1800, chessdroidElo: 1750);
        var black = Profile("Engine B", 1600, chessdroidElo: 1650);

        var result = MatchBoardPanel.ComputeRatingUpdate(white, black, whiteScore: 1.0);

        Assert.NotNull(result);
        var (_, _, wNew, bNew) = result!.Value;
        Assert.Equal("Engine A", wNew.DisplayName);
        Assert.Equal(1800, wNew.Elo);
        Assert.Equal("Engine B", bNew.DisplayName);
        Assert.Equal(1600, bNew.Elo);
    }

    [Fact]
    public void WhiteProfileMissing_ReturnsNull()
        => Assert.Null(MatchBoardPanel.ComputeRatingUpdate(null, Profile("B", 1500), 1.0));

    [Fact]
    public void BlackProfileMissing_ReturnsNull()
        => Assert.Null(MatchBoardPanel.ComputeRatingUpdate(Profile("A", 1500), null, 1.0));

    [Fact]
    public void WhiteHasNoCcrlEloSeed_ReturnsNull()
    {
        var white = Profile("Engine A", 0);  // never configured in EngineProfilesDialog
        var black = Profile("Engine B", 1500);
        Assert.Null(MatchBoardPanel.ComputeRatingUpdate(white, black, 1.0));
    }

    [Fact]
    public void BlackHasNoCcrlEloSeed_ReturnsNull()
    {
        var white = Profile("Engine A", 1500);
        var black = Profile("Engine B", 0);
        Assert.Null(MatchBoardPanel.ComputeRatingUpdate(white, black, 1.0));
    }
}
