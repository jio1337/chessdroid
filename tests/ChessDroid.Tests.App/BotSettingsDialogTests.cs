using ChessDroid.Models;

namespace ChessDroid.Tests.App;

/// <summary>
/// Regression test for the Drill-vs-Bot "Challenge Mode is always on" bug: the dialog hides
/// the Friendly/Challenge radio group in drill mode (comment: "challenge mode is always on"),
/// but hiding a RadioButton group doesn't change which one is checked — rbFriendly defaults
/// to checked, so Settings.ChallengeMode silently came back false for every drill game. That
/// in turn left BtnTakeBack_Click's "no takebacks in challenge mode" guard
/// (AnalysisBoardForm.cs) and DrillPracticeAsync's design intent both unenforced. Fixed by
/// explicitly checking rbChallenge when drillMode is true.
/// </summary>
public class BotSettingsDialogTests
{
    private static BotSettings StartWithDrillMode(bool drillMode)
    {
        BotSettings? settings = null;
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                using var dialog = new BotSettingsDialog(isDarkMode: false, Array.Empty<string>(),
                    new Dictionary<string, EngineProfile>(), defaultEngine: "", drillMode: drillMode);
                // Button.PerformClick() only raises Click when CanSelect is true, which (per
                // Control.CanSelect) requires an actual created, visible handle — not just a
                // constructed Form. Firing it from Shown (after ShowDialog has made the form
                // visible/activated) satisfies that, and Close() inside the click handler
                // unblocks ShowDialog() itself.
                dialog.Shown += (_, _) => dialog.btnStart.PerformClick();
                dialog.ShowDialog();
                settings = dialog.Settings;
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "dialog test thread did not complete in time");

        Assert.Null(failure);
        Assert.NotNull(settings);
        return settings!;
    }

    [Fact]
    public void DrillMode_DefaultsToChallengeMode()
    {
        var settings = StartWithDrillMode(drillMode: true);
        Assert.True(settings.ChallengeMode);
    }

    [Fact]
    public void NonDrillMode_DefaultsToFriendlyMode()
    {
        var settings = StartWithDrillMode(drillMode: false);
        Assert.False(settings.ChallengeMode);
    }
}
