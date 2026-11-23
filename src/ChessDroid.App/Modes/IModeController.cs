namespace ChessDroid.Modes
{
    /// <summary>
    /// Narrow, Exit-only mode-controller abstraction (CHESSDROID_AUDIT.md Phase 4, §4.2).
    /// The audit's literal sketch (Enter/Exit/OnUserMove/OnNavigate) isn't mechanically
    /// wrappable here without rewriting real behavior: Match mode has no synchronous
    /// Enter/Exit pair (its "enter" is a 150-line inlined click handler, and its "exit"
    /// happens asynchronously inside a match-ended event callback, not when the user clicks
    /// stop), Bot's entry needs a modal dialog's result as a parameter, and Training has
    /// three overlapping lifecycle layers (panel open/closed, game active, which sub-mode)
    /// with cross-mode transitions (drill-vs-bot, drill-watch) buried inside it that
    /// directly flip the Bot/Match flags themselves.
    ///
    /// This interface only formalizes the one thing every mode already does cleanly: a
    /// parameterless, synchronous stop/exit. AnalysisBoardForm.CurrentModeController tracks
    /// which one is active as purely additive bookkeeping — nothing today reads it to gate
    /// behavior, so wiring it in changes no existing control flow. A full Enter/Exit/
    /// OnUserMove/OnNavigate dispatch covering every mode (including Match and Training's
    /// cross-mode transitions) would require actually rewriting that behavior, which can't
    /// be verified without live GUI testing — not available in this environment.
    /// </summary>
    public interface IModeController
    {
        void Exit();
    }
}
