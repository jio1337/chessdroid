using ChessDroid.Services;

namespace ChessDroid.Tests.App;

/// <summary>
/// Regression test for audit §3.2: DisplayAnalysisResults used to run MovesExplanation /
/// ThreatDetection / DefenseDetection / Brilliant-move detection inline on the UI thread,
/// interleaved with RichTextBox writes. Fixed by splitting it into BuildAnalysisReport (pure
/// compute, runs via Task.Run) and RenderAnalysisReport (RichTextBox-only), wired together by
/// DisplayAnalysisResultsAsync.
///
/// [StaFact] (Xunit.StaFact) only guarantees the test body runs on an STA thread — it does
/// NOT run a WinForms message loop. DisplayAnalysisResultsAsync's Task.Run continuation
/// resumes via WindowsFormsSynchronizationContext.Post, which requires an active message
/// pump (Application.Run) to actually dispatch; without one it deadlocks forever (this is
/// exactly what happened the first time the throwaway verification harness for this fix was
/// written, before it was wrapped in Application.Run). So this test drives its own
/// Application.Run loop on a plain [Fact] thread instead of relying on [StaFact] alone.
/// </summary>
public class AnalysisReportRenderTests
{
    private const string Fen = "r1bqkbnr/pp1ppppp/2n5/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R b KQkq - 2 3";

    [Fact]
    public void DisplayAnalysisResultsAsync_RendersRealContent_AndResumesOnUiThread()
    {
        Exception? failure = null;
        string? text = null;
        bool resumedOnUiThread = false;

        var thread = new Thread(() =>
        {
            var richTextBox = new RichTextBox();
            using var hostForm = new Form { WindowState = FormWindowState.Minimized, ShowInTaskbar = false };
            hostForm.Controls.Add(richTextBox);

            hostForm.Load += async (_, _) =>
            {
                try
                {
                    var config = new AppConfig
                    {
                        ShowMoveQuality = true,
                        ShowThreats = true,
                        ShowWDL = true,
                        PlayStyleEnabled = true,
                        Aggressiveness = 75, // non-balanced -> exercises the "Recommended" section
                        ShowOpeningName = true,
                        ShowBookMoves = true,
                        ShowExplanations = true,
                        ShowBestLine = true,
                        ShowSecondLine = true,
                        ShowThirdLine = true,
                    };

                    var formatter = new ConsoleOutputFormatter(richTextBox, config, MovesExplanation.GenerateMoveExplanation);

                    var pvs = new List<string> { "d7d6 d2d4 c5d4 f3d4", "g8f6 e4e5 f6d5", "e7e6 d2d4 d7d5" };
                    var evals = new List<string> { "+0.45", "+0.20", "-0.10" };
                    var wdl = new WDLInfo(420, 380, 200);

                    int uiThreadId = Environment.CurrentManagedThreadId;

                    await formatter.DisplayAnalysisResultsAsync("d7d6", "+0.45", pvs, evals, Fen,
                        showBestLine: true, showSecondLine: true, showThirdLine: true, wdl, bookMoves: null);

                    resumedOnUiThread = Environment.CurrentManagedThreadId == uiThreadId;
                    text = richTextBox.Text;
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
                finally
                {
                    Application.Exit();
                }
            };

            Application.Run(hostForm);
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)), "render test thread did not complete in time");

        Assert.Null(failure);
        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.Contains("+0.45", text);
        Assert.Contains("Style:", text); // Aggressiveness=75, non-balanced -> Recommended section
        Assert.True(resumedOnUiThread, "rendering must happen back on the UI thread, not a thread-pool thread");
    }
}
