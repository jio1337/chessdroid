namespace ChessDroid
{
    /// <summary>
    /// Which training sub-mode is selected in the Training panel. Replaces 4 mutually-
    /// exclusive bools (_openingModeSelected/_puzzleModeSelected/_visionModeSelected/
    /// _drillModeSelected) plus a stringly-typed SetTrainingMode(string) that reconstructed
    /// "which one is true" via a ternary chain — an enum makes two of these being set at
    /// once structurally impossible instead of just conventionally avoided.
    /// </summary>
    public enum TrainingSubMode
    {
        Square,
        Opening,
        Puzzle,
        Vision,
        Drill
    }
}
