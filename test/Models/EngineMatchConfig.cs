namespace ChessDroid.Models
{
    public enum TimeControlType
    {
        FixedDepth,
        FixedTimePerMove,
        TotalPlusIncrement
    }

    public enum MatchTermination
    {
        None,
        Checkmate,
        Stalemate,
        TimeForfeit,
        FiftyMoveRule,
        ThreefoldRepetition,
        InsufficientMaterial,
        UserStopped,
        EngineError
    }

    public enum MatchOutcome
    {
        WhiteWins,
        BlackWins,
        Draw,
        Interrupted
    }

    public class EngineMatchTimeControl
    {
        public TimeControlType Type { get; set; } = TimeControlType.FixedTimePerMove;

        /// <summary>For FixedDepth mode</summary>
        public int Depth { get; set; } = 15;

        /// <summary>For FixedTimePerMove mode (milliseconds)</summary>
        public int MoveTimeMs { get; set; } = 1000;

        /// <summary>For TotalPlusIncrement mode - total time per side (milliseconds)</summary>
        public int TotalTimeMs { get; set; } = 300_000;

        /// <summary>For TotalPlusIncrement mode - increment per move (milliseconds)</summary>
        public int IncrementMs { get; set; } = 2000;

        public string BuildGoCommand(long whiteRemainingMs, long blackRemainingMs)
        {
            return Type switch
            {
                TimeControlType.FixedDepth => $"go depth {Depth}",
                TimeControlType.FixedTimePerMove => $"go movetime {MoveTimeMs}",
                TimeControlType.TotalPlusIncrement =>
                    $"go wtime {whiteRemainingMs} btime {blackRemainingMs} winc {IncrementMs} binc {IncrementMs}",
                _ => $"go movetime {MoveTimeMs}"
            };
        }

        public int GetTimeoutMs(long remainingMs)
        {
            return Type switch
            {
                TimeControlType.FixedDepth => 120_000,
                TimeControlType.FixedTimePerMove => MoveTimeMs + 5000,
                TimeControlType.TotalPlusIncrement => (int)Math.Min(remainingMs + 10000, 300_000),
                _ => 30_000
            };
        }

        public override string ToString()
        {
            return Type switch
            {
                TimeControlType.FixedDepth => $"Depth {Depth}",
                TimeControlType.FixedTimePerMove => $"{MoveTimeMs}ms/move",
                TimeControlType.TotalPlusIncrement =>
                    $"{TotalTimeMs / 1000}s + {IncrementMs / 1000}s",
                _ => "Unknown"
            };
        }
    }

    public class EngineMatchResult
    {
        public MatchOutcome Outcome { get; set; }
        public MatchTermination Termination { get; set; }
        public int TotalMoves { get; set; }
        public long WhiteTimeRemainingMs { get; set; }
        public long BlackTimeRemainingMs { get; set; }

        public string GetResultString()
        {
            string result = Outcome switch
            {
                MatchOutcome.WhiteWins => "1-0",
                MatchOutcome.BlackWins => "0-1",
                MatchOutcome.Draw => "1/2-1/2",
                _ => "*"
            };

            string reason = Termination switch
            {
                MatchTermination.Checkmate => "checkmate",
                MatchTermination.Stalemate => "stalemate",
                MatchTermination.TimeForfeit => "time forfeit",
                MatchTermination.FiftyMoveRule => "50-move rule",
                MatchTermination.ThreefoldRepetition => "threefold repetition",
                MatchTermination.InsufficientMaterial => "insufficient material",
                MatchTermination.UserStopped => "stopped by user",
                MatchTermination.EngineError => "engine error",
                _ => ""
            };

            return $"{result} ({reason}) after {TotalMoves} moves";
        }
    }
}
