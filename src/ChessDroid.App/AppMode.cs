namespace ChessDroid
{
    /// <summary>
    /// The top-level session mode AnalysisBoardForm is in. Confirmed mutually exclusive by a
    /// full survey of existing guard conditions before introducing this (e.g.
    /// "if (matchRunning || _botModeActive)") — this enum doesn't replace _botModeActive/
    /// matchRunning/_trainingGameActive (each still carries its own start/stop lifecycle and
    /// has independent readers), it gives call sites that need "which ONE mode is active" a
    /// single, exhaustive, compiler-checked answer instead of hand-rolling a partial
    /// combination of the three bools (which is how the editing-position guard ended up
    /// silently excluding Training — see AnalysisBoardForm.cs, BtnEditPosition_Click).
    ///
    /// Deliberately NOT included: Chess960 (_chess960Active) is an orthogonal board-variant
    /// setting that combines with any mode, not a mode itself; _bookArrowsActive/_autoPlaying
    /// are transient display toggles; _drillBotActive/_drillWatchActive are sub-flavors of
    /// Bot/Match, not separate modes; _trainingUiVisible (panel open) is not the same as
    /// Training (a game actively running) — you can browse the panel with no game active.
    /// </summary>
    public enum AppMode
    {
        Analysis,
        Bot,
        Match,
        Training
    }
}
