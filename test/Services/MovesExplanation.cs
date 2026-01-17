using ChessDroid.Models;
using System.Diagnostics;

namespace ChessDroid.Services
{
    public class MovesExplanation
    {
        // =============================
        // CHESS MOVE EXPLANATION ENGINE
        // Comprehensive tactical and positional analysis system
        // =============================
        //
        // ADVANCED FEATURES (Ethereal-inspired):
        // - Static Exchange Evaluation (SEE) for captures
        // - Move interestingness scoring (forcing vs quiet)
        // - Positional evaluation (pawn structure, piece activity, king safety)
        // - Improving/worsening position detection
        //
        // =============================
        // CHESS TACTICAL MOTIFS REFERENCE
        // Comprehensive guide to recognizing and executing chess tactics
        // =============================
        //
        // BASIC TACTICS (Foundations - Learn these first)
        // ------------------------------------------------
        //
        // 1. FORK ✓ [IMPLEMENTED]
        //    One piece attacks two or more enemy pieces simultaneously. The opponent can only save one.
        //    Example: Knight on e5 attacking queen on c6 and rook on g6 (royal fork)
        //    Common perpetrators: Knights (can't be blocked), Queens, Pawns
        //    Detection: Royal fork (K+Q), Family fork (K+Q+R), or any 2+ valuable pieces
        //
        // 2. PIN ✓ [IMPLEMENTED]
        //    A piece cannot move without exposing a more valuable piece behind it.
        //    - Absolute Pin: Moving exposes king (illegal move)
        //    - Relative Pin: Moving loses material (legal but costly)
        //    Example: Bishop pins knight to queen on same diagonal
        //    Only sliding pieces (Bishop, Rook, Queen) can create pins
        //
        // 3. SKEWER ✓ [IMPLEMENTED]
        //    Reverse pin: Attack valuable piece forcing it to move, then capture less valuable piece behind.
        //    Example: Rook attacks king on e-file, king moves, rook captures rook behind
        //    Think of it as: "Attack the big fish, eat the small fish"
        //
        // 4. DISCOVERED ATTACK ✓ [IMPLEMENTED]
        //    Moving one piece reveals attack from another piece that was blocked.
        //    Example: Bishop on b2 attacks rook on g7, pawn on e5 moves revealing the attack
        //    Most dangerous when the moving piece also creates a threat (discovered check)
        //
        // 5. DOUBLE CHECK ✓ [IMPLEMENTED]
        //    Two pieces give check simultaneously (usually via discovered check).
        //    Example: Moving piece gives check AND reveals check from piece behind
        //    Devastating because opponent MUST move king (can't block or capture both)
        //
        // 6. HANGING PIECE ✓ [IMPLEMENTED]
        //    An undefended piece that can be captured for free.
        //    Example: Knight on f6 with no defenders, opponent's bishop takes it
        //    Most common tactical mistake. Always check: "Is this piece defended?"
        //
        // 7. TRAPPED PIECE ✓ [IMPLEMENTED]
        //    A piece with no safe squares to move to.
        //    Example: Bishop on h6 surrounded by pawns, king on g8, attacked by knight
        //    Common victims: Bishops in corners, knights on rim, advanced rooks
        //
        // 8. REMOVAL OF DEFENDER ✓ [IMPLEMENTED]
        //    Eliminate the piece protecting a key square or piece.
        //    Example: Rook defends knight. Capture rook, then capture now-undefended knight
        //    Two-step tactic: Remove guardian → Capture treasure
        //
        // INTERMEDIATE TACTICS (More complex patterns)
        // ---------------------------------------------
        //
        // 9. DOUBLE ATTACK ✓ [IMPLEMENTED]
        //    Any move creating two separate threats at once. Fork is a subtype.
        //    Example: Queen moves to attack both an undefended rook and threaten mate
        //    Opponent can only address one threat
        //
        // 10. DEFLECTION ✓ [IMPLEMENTED]
        //     Force a defending piece away from its critical duty.
        //     Example: Rook sacrifice forces king away from protecting queen
        //     Similar to removal, but the defender moves rather than dies
        //
        // 11. DECOY ✓ [IMPLEMENTED]
        //     Lure a piece to a bad square using a sacrifice.
        //     Example: Queen sacrifice on h7 forces king to h7, then knight delivers mate
        //     The sacrifice must force the opponent's hand
        //
        // 12. OVERLOADING ✓ [IMPLEMENTED]
        //     One piece trying to defend multiple things—something must fall.
        //     Example: Rook defending both back rank and a knight. Attack both.
        //     Find the piece with too many jobs
        //
        // 13. INTERFERENCE
        //     Block the line between two enemy pieces (usually defenders).
        //     Example: Knight jumps between defending rook and queen
        //     Cuts communication lines
        //
        // 14. CLEARANCE
        //     Vacate a square/line so another piece can use it.
        //     Example: Rook moves to allow queen access to back rank
        //     "Get out of the way, I'm delivering mate"
        //
        // 15. ZWISCHENZUG (In-Between Move)
        //     Instead of obvious recapture, insert a forcing move first.
        //     Example: After Qxe5, instead of recapturing, play check first
        //     The move your opponent didn't calculate
        //
        // 16. X-RAY ATTACK ✓ [IMPLEMENTED]
        //     A piece attacks through another piece (defender or attacker).
        //     Example: Queen x-rays through enemy knight to attack rook behind
        //     Looks harmless until the front piece moves
        //
        // SACRIFICIAL TACTICS (Material for attack)
        // ------------------------------------------
        //
        // 17. SACRIFICE
        //     Intentional material loss for concrete advantage (attack, mate, promotion).
        //     Rule: Must have a forced continuation. No hope = blunder, not sacrifice.
        //     Common types: Greek Gift, Exchange Sacrifice, Queen Sacrifice
        //
        // 18. GREEK GIFT SACRIFICE
        //     Classic Bxh7+ (or Bxh2+ for Black) to expose king.
        //     Conditions needed: King on g8, h7 pawn present, no defenders, follow-up ready
        //     If you don't know the theory, don't do it
        //
        // 19. EXCHANGE SACRIFICE
        //     Give rook for bishop/knight to gain positional compensation.
        //     Example: Rxc3 demolishing pawn structure or opening lines
        //     Engines love these. Humans panic. Trust the position, not the material.
        //
        // CHECKMATE PATTERNS
        // ------------------
        //
        // 20. BACK RANK MATE ✓ [IMPLEMENTED]
        //     King trapped on back rank by own pawns, enemy rook delivers mate.
        //     Prevention: Create luft (pawn move like h3/h6)
        //     Still claims victims at all levels
        //
        // 21. SMOTHERED MATE ✓ [IMPLEMENTED]
        //     King completely surrounded by own pieces, knight delivers mate.
        //     Classic pattern: Knight on f7/f2 with king on h8/h1
        //     Beautiful, rare, unforgettable
        //
        // 22. MATING NET
        //     Coordinated piece setup making mate unstoppable within 2-3 moves.
        //     Example: Queen + Knight + Rook closing in on exposed king
        //     Not one move, but a tightening noose
        //
        // ENDGAME TACTICS
        // ---------------
        //
        // 23. PROMOTION TACTICS ✓ [IMPLEMENTED]
        //     Sacrifices/deflections forcing pawn to queen.
        //     Example: Rook sacrifice clearing path for passed pawn
        //     Pawns are future queens—never underestimate them
        //
        // 24. PASSED PAWN BREAKTHROUGH
        //     Pawn sacrifices creating unstoppable passed pawn.
        //     Example: Three connected pawns, sacrifice two to push one through
        //     Endgame violence
        //
        // 25. OPPOSITION
        //     Kings directly facing each other; whoever moves loses ground.
        //     Example: Ke5 vs Ke7, whoever moves first loses the square
        //     Pawn endgame fundamental
        //
        // 26. TRIANGULATION
        //     King "loses a tempo" to gain opposition.
        //     Example: Kf5-e5-f6 while enemy king stuck
        //     Counterintuitive tempo loss that wins
        //
        // 27. ZUGZWANG
        //     Any move worsens the position. "Your turn." "I die."
        //     Example: All moves lose material or allow mate
        //     Common in endgames, rare in middlegames
        //
        // SPECIAL/DEFENSIVE TACTICS
        // --------------------------
        //
        // 28. PERPETUAL CHECK ✓ [IMPLEMENTED]
        //     Endless checks forcing a draw when losing.
        //     Example: Queen checking king repeatedly with no escape
        //     Last resort survival tactic when down material
        //
        // 29. STALEMATE TRAP
        //     Force draw by leaving opponent no legal moves (but not in check).
        //     Example: King in corner with no moves, all pieces blocked
        //     Refuge of the hopeless and the clever
        //
        // 30. EN PASSANT TACTICS [DISABLED - Too many false positives]
        //     Rare pawn capture where advancing 2 squares doesn't escape attack.
        //     Example: Pawn on e5, opponent plays f7-f5, you capture exf6 e.p.
        //     Requires move history tracking - cannot detect from notation alone
        //
        // 31. WINDMILL
        //     Repeated discovered checks winning massive material.
        //     Example: Rook on h7, bishop gives discovered check, king moves, rook captures piece, repeat
        //     If you fall for this, uninstall chess
        //
        // POSITIONAL/STRATEGIC WEAKNESSES
        // --------------------------------
        //
        // 32. WEAK BACK RANK [DISABLED - Too many false positives]
        //     No escape squares for king on back rank (no luft).
        //     Sets up back rank mates and tactics
        //     Needs more sophisticated detection (actual threats, not just position)
        //
        // 33. WEAK COLOR COMPLEX
        //     Squares of one color are chronically weak (usually from bishop trade/absence).
        //     Example: All dark squares weak after losing dark-squared bishop
        //     Knights dominate weak color complexes
        //
        // 34. LOOSE PIECES DROP OFF (LPDO) [DISABLED - Redundant]
        //     Undefended or poorly defended pieces eventually get captured.
        //     Already covered by "Hanging Piece" detection (#6)
        //     Tarrasch's principle: "The threat is stronger than the execution"
        //
        // =============================
        // NOTE: Tactics marked with ✓ are actively detected by this engine
        // =============================

        // Get material value of a piece (standard chess values)
        // Helper methods moved to ChessUtilities for code reuse

        // Generate a brief explanation for why the move is best
        public static string GenerateMoveExplanation(string bestMove, string fen, List<string> pvs, string evaluation)
        {
            try
            {
                // FORCED MATE: When there's a forced checkmate FOR US, no other explanation needed
                // "Mate in X" = we have mate, "Mate in -X" = opponent has mate against us
                if (!string.IsNullOrEmpty(evaluation) && evaluation.StartsWith("Mate in ") && !evaluation.Contains("-"))
                {
                    return $"forced checkmate ({evaluation.ToLower()})";
                }

                ChessBoard board = ChessBoard.FromFEN(fen);

                if (string.IsNullOrEmpty(bestMove) || bestMove.Length < 4)
                    return "";

                string source = bestMove.Substring(0, 2);
                string dest = bestMove.Substring(2, 2);

                // Validate source and dest strings
                if (source.Length != 2 || dest.Length != 2)
                    return "";

                // Validate that characters are in valid range
                if (source[0] < 'a' || source[0] > 'h' || source[1] < '1' || source[1] > '8')
                    return "";
                if (dest[0] < 'a' || dest[0] > 'h' || dest[1] < '1' || dest[1] > '8')
                    return "";

                int srcFile = source[0] - 'a';
                int srcRank = 8 - (source[1] - '0');
                int destFile = dest[0] - 'a';
                int destRank = 8 - (dest[1] - '0');

                // Double-check bounds
                if (srcFile < 0 || srcFile >= 8 || srcRank < 0 || srcRank >= 8)
                    return "";
                if (destFile < 0 || destFile >= 8 || destRank < 0 || destRank >= 8)
                    return "";

                char piece = board.GetPiece(srcRank, srcFile);
                char targetPiece = board.GetPiece(destRank, destFile);
                PieceType pieceType = PieceHelper.GetPieceType(piece);
                bool isWhite = char.IsUpper(piece);

                List<string> reasons = new List<string>();

                // Create a temporary board with the move applied to check for tactics
                ChessBoard tempBoard = new ChessBoard(board.GetArray());
                tempBoard.SetPiece(destRank, destFile, piece);
                tempBoard.SetPiece(srcRank, srcFile, '.');

                // =============================
                // STOCKFISH FEATURES - PRIORITY 1
                // Singular move and threat detection
                // =============================

                // SINGULAR MOVE DETECTION (only good move available)
                bool isSingular = false;
                if (pvs != null && pvs.Count >= 2 && !string.IsNullOrEmpty(evaluation))
                {
                    // Try to get second-best evaluation from PV line
                    string secondEval = pvs.Count >= 2 ? ExtractEvalFromPV(pvs[1]) : "";
                    isSingular = StockfishFeatures.IsSingularMove(pvs, evaluation, secondEval);

                    if (isSingular)
                        reasons.Add("only good move");
                }

                // FORCED MOVE DETECTION (in check with one legal move)
                bool isForced = StockfishFeatures.IsForcedMove(board, isWhite);
                if (isForced && reasons.Count < 2)
                    reasons.Add("forced move");

                // THREAT CREATION (attacking valuable piece with lower-value piece)
                if (reasons.Count < 2)
                {
                    string? threatCreation = StockfishFeatures.DetectThreatCreation(
                        board, tempBoard, destRank, destFile, piece, isWhite);

                    if (!string.IsNullOrEmpty(threatCreation))
                        reasons.Add(threatCreation);
                }

                // Check for tactical patterns SECOND (after singular/threat detection)
                // Pass both original board and temp board for discovered attack detection
                string? tacticalPattern = DetectTacticalPattern(board, tempBoard, srcRank, srcFile, destRank, destFile, piece, pieceType, isWhite);
                if (!string.IsNullOrEmpty(tacticalPattern))
                {
                    reasons.Add(tacticalPattern);
                }

                // PERPETUAL CHECK: Detect from PV lines
                if (pvs != null)
                {
                    string? perpetualCheckInfo = DetectPerpetualCheck(pvs);
                    if (!string.IsNullOrEmpty(perpetualCheckInfo))
                    {
                        reasons.Add(perpetualCheckInfo);
                    }
                }

                // SACRIFICE DETECTION: Check BEFORE regular capture detection
                // Exchange Sacrifice (Rook for minor piece)
                string? exchangeSacInfo = DetectExchangeSacrifice(tempBoard, destRank, destFile, piece, targetPiece, evaluation);
                if (!string.IsNullOrEmpty(exchangeSacInfo))
                {
                    reasons.Add(exchangeSacInfo);
                }

                // General Sacrifice (material for compensation)
                string? sacrificeInfo = DetectSacrifice(tempBoard, destRank, destFile, piece, targetPiece, isWhite, evaluation);
                if (!string.IsNullOrEmpty(sacrificeInfo))
                {
                    reasons.Add(sacrificeInfo);
                }

                // =============================
                // CAPTURE EVALUATION with SEE (Static Exchange Evaluation)
                // Inspired by Ethereal's movepicker.c
                // =============================
                if (targetPiece != '.' && reasons.Count < 2)
                {
                    // Use SEE to determine if capture wins material
                    string? seeInfo = MoveEvaluation.DetectWinningCapture(board, srcRank, srcFile,
                        destRank, destFile, piece, targetPiece, isWhite);

                    if (!string.IsNullOrEmpty(seeInfo))
                    {
                        reasons.Add(seeInfo);
                    }
                    else
                    {
                        // Fallback to simple capture description
                        string capturedPiece = ChessUtilities.GetPieceName(PieceHelper.GetPieceType(targetPiece));
                        reasons.Add($"captures {capturedPiece}");
                    }
                }

                // Check for pawn advances
                if (pieceType == PieceType.Pawn)
                {
                    int advancement = char.IsUpper(piece) ? srcRank - destRank : destRank - srcRank;
                    if (advancement == 2)
                    {
                        reasons.Add("aggressive pawn push");
                    }

                    // Check for promotion
                    if (bestMove.Length > 4)
                    {
                        reasons.Add("promotes to " + bestMove[4].ToString().ToUpper());
                    }
                }

                // =============================
                // POSITIONAL EVALUATION (Ethereal-inspired)
                // Analyze pawn structure, piece activity, king safety
                // =============================

                // PAWN STRUCTURE ANALYSIS
                if (pieceType == PieceType.Pawn && reasons.Count < 2)
                {
                    // Passed pawn detection (highest priority)
                    string? passedPawnInfo = PositionalEvaluation.DetectPassedPawn(tempBoard, destRank, destFile, isWhite);
                    if (!string.IsNullOrEmpty(passedPawnInfo))
                        reasons.Add(passedPawnInfo);

                    // Connected pawns (strength)
                    string? connectedInfo = PositionalEvaluation.DetectConnectedPawns(tempBoard, destRank, destFile, isWhite);
                    if (!string.IsNullOrEmpty(connectedInfo) && reasons.Count < 2)
                        reasons.Add(connectedInfo);

                    // NOTE: We do NOT show pawn structure weaknesses (isolated, doubled, backward) for our OWN pawns
                    // If the engine recommends a move that creates these weaknesses for us, it's still the best move
                    // and showing "creates doubled pawns" would confuse the user.
                    // Instead, we check for OPPONENT weaknesses when we capture (see below).
                }

                // Check if our capture creates pawn structure weaknesses for the OPPONENT
                // This is a positive - we're damaging their pawn structure!
                if (targetPiece != '.' && reasons.Count < 2)
                {
                    // Check if removing this piece leaves opponent with isolated/doubled pawns
                    string? opponentWeakness = DetectOpponentPawnWeakness(tempBoard, destRank, destFile, !isWhite);
                    if (!string.IsNullOrEmpty(opponentWeakness))
                        reasons.Add(opponentWeakness);
                }

                // PIECE ACTIVITY ANALYSIS (Knights, Bishops, Rooks, Queens)
                if (reasons.Count < 2 && (pieceType == PieceType.Knight || pieceType == PieceType.Bishop ||
                                          pieceType == PieceType.Rook || pieceType == PieceType.Queen))
                {
                    // Outpost detection (very strong positional feature)
                    string? outpostInfo = PositionalEvaluation.DetectOutpost(tempBoard, destRank, destFile, piece, isWhite);
                    if (!string.IsNullOrEmpty(outpostInfo))
                        reasons.Add(outpostInfo);

                    // Long diagonal control (bishops)
                    if (pieceType == PieceType.Bishop && reasons.Count < 2)
                    {
                        string? diagonalInfo = PositionalEvaluation.DetectLongDiagonalControl(tempBoard, destRank, destFile, piece, isWhite);
                        if (!string.IsNullOrEmpty(diagonalInfo))
                            reasons.Add(diagonalInfo);
                    }

                    // High mobility (active pieces)
                    if (reasons.Count < 2)
                    {
                        string? mobilityInfo = PositionalEvaluation.DetectHighMobility(tempBoard, destRank, destFile, piece, isWhite);
                        if (!string.IsNullOrEmpty(mobilityInfo))
                            reasons.Add(mobilityInfo);
                    }

                    // Central control
                    if (reasons.Count < 2)
                    {
                        string? centralInfo = PositionalEvaluation.DetectCentralControl(tempBoard, destRank, destFile, piece, isWhite);
                        if (!string.IsNullOrEmpty(centralInfo))
                            reasons.Add(centralInfo);
                    }
                }

                // KING SAFETY ANALYSIS
                // Only report POSITIVE king safety changes (improvements)
                // Negative messages like "exposes king" are confusing on best moves -
                // the engine already factored this in, showing it makes users think it's bad
                if (pieceType == PieceType.King && reasons.Count < 2)
                {
                    // Compare shelter BEFORE and AFTER the move
                    int beforeShelter = CountPawnShield(board, srcRank, srcFile, isWhite);
                    int afterShelter = CountPawnShield(tempBoard, destRank, destFile, isWhite);

                    // Only show POSITIVE messages for king moves
                    if (afterShelter > beforeShelter)
                    {
                        reasons.Add("improves king safety");
                    }
                    // Don't show "exposes king" - if it's the best move, the tradeoff is worth it
                    // and showing this message confuses users into thinking the move is bad
                }

                // BASIC POSITIONAL CONSIDERATIONS (fallback if still no reasons)
                if (reasons.Count < 2)
                {
                    // Check if moving to center (basic centralization)
                    if (pieceType == PieceType.Knight || pieceType == PieceType.Bishop)
                    {
                        if (destFile >= 2 && destFile <= 5 && destRank >= 2 && destRank <= 5)
                        {
                            reasons.Add("centralizes piece");
                        }
                    }
                }

                // Development moves
                if (reasons.Count < 2 && (srcRank == 0 || srcRank == 7) &&
                    (pieceType == PieceType.Knight || pieceType == PieceType.Bishop))
                {
                    reasons.Add("develops piece");
                }

                // Castling
                if (pieceType == PieceType.King && Math.Abs(destFile - srcFile) == 2)
                {
                    reasons.Add(destFile > srcFile ? "castles kingside for safety" : "castles queenside");
                }

                // =============================
                // ENDGAME-SPECIFIC ANALYSIS (Ethereal-inspired)
                // Detect special endgame patterns and characteristics
                // =============================
                if (reasons.Count < 2)
                {
                    // Detect specific endgame types
                    string? kpvk = EndgameAnalysis.DetectKPvK(tempBoard, isWhite);
                    if (!string.IsNullOrEmpty(kpvk))
                        reasons.Add(kpvk);

                    string? oppBishops = EndgameAnalysis.DetectOppositeBishops(tempBoard);
                    if (!string.IsNullOrEmpty(oppBishops) && reasons.Count < 2)
                        reasons.Add(oppBishops);

                    string? rookEndgame = EndgameAnalysis.DetectRookEndgame(tempBoard);
                    if (!string.IsNullOrEmpty(rookEndgame) && reasons.Count < 2)
                        reasons.Add(rookEndgame);

                    string? queenEndgame = EndgameAnalysis.DetectQueenEndgame(tempBoard);
                    if (!string.IsNullOrEmpty(queenEndgame) && reasons.Count < 2)
                        reasons.Add(queenEndgame);

                    string? bareKing = EndgameAnalysis.DetectBareKing(tempBoard, !isWhite);
                    if (!string.IsNullOrEmpty(bareKing) && reasons.Count < 2)
                        reasons.Add(bareKing);

                    // Material imbalance detection
                    string? materialImbalance = EndgameAnalysis.DetectMaterialImbalance(tempBoard);
                    if (!string.IsNullOrEmpty(materialImbalance) && reasons.Count < 2)
                        reasons.Add(materialImbalance);

                    string? qualityImbalance = EndgameAnalysis.DetectQualityImbalance(tempBoard);
                    if (!string.IsNullOrEmpty(qualityImbalance) && reasons.Count < 2)
                        reasons.Add(qualityImbalance);

                    // Zugzwang detection (only if no other explanation)
                    if (reasons.Count == 0 && EndgameAnalysis.IsPotentialZugzwang(tempBoard, isWhite))
                        reasons.Add("zugzwang position (any move worsens position)");

                    // TABLEBASE INTEGRATION (Advanced endgame knowledge)
                    if (reasons.Count < 2)
                    {
                        string? tablebaseInfo = AdvancedAnalysis.GetTablebaseExplanation(tempBoard, isWhite);
                        if (!string.IsNullOrEmpty(tablebaseInfo))
                            reasons.Add(tablebaseInfo);
                    }
                }

                // =============================
                // OPENING MOVE ANALYSIS
                // Low-ply history tracking for opening-specific patterns
                // =============================
                if (reasons.Count < 2)
                {
                    // Estimate current ply from FEN (simplified - count material changes)
                    int estimatedPly = 2; // Default to early game
                    string? openingInfo = AdvancedAnalysis.GetOpeningMoveDescription(bestMove, estimatedPly);
                    if (!string.IsNullOrEmpty(openingInfo))
                        reasons.Add(openingInfo);
                }

                // =============================
                // EVALUATION CONTEXT (Game phase aware + Win Rate Model)
                // Inspired by Ethereal's phase-based evaluation + Stockfish's WDL
                // =============================
                if (reasons.Count == 0)
                {
                    double? eval = ParseEvaluation(evaluation);
                    if (eval.HasValue)
                    {
                        string gamePhase = EndgameAnalysis.GetGamePhase(tempBoard);
                        bool isEndgame = EndgameAnalysis.IsEndgame(tempBoard);
                        int materialCount = EndgameAnalysis.CountTotalPieces(tempBoard);

                        // WIN RATE MODEL INTEGRATION (Stockfish-style)
                        // Convert eval to winning percentage for user-friendly explanations
                        string winningChanceDesc = AdvancedAnalysis.GetWinningChanceDescription(
                            Math.Abs(eval.Value), materialCount);

                        // Position complexity
                        string? complexityDesc = AdvancedAnalysis.GetComplexityDescription(tempBoard, eval.Value);

                        // Decisive advantage (>= 3 pawns)
                        if (Math.Abs(eval.Value) >= 3.0)
                        {
                            if (eval.Value > 0)
                            {
                                if (complexityDesc != null)
                                    reasons.Add($"{winningChanceDesc} ({complexityDesc})");
                                else
                                    reasons.Add(isEndgame ? "winning endgame" : winningChanceDesc);
                            }
                            else
                            {
                                reasons.Add(isEndgame ? "defensive endgame play" : "fights back in difficult position");
                            }
                        }
                        // Significant advantage (1.5-3 pawns)
                        else if (Math.Abs(eval.Value) >= 1.5)
                        {
                            if (eval.Value > 0)
                                reasons.Add(isEndgame ? "better endgame" : winningChanceDesc);
                            else
                                reasons.Add(isEndgame ? "holds endgame" : "reduces disadvantage");
                        }
                        // Small advantage (0.5-1.5 pawns)
                        else if (Math.Abs(eval.Value) >= 0.5)
                        {
                            if (eval.Value > 0)
                                reasons.Add(winningChanceDesc);
                            else
                                reasons.Add("equalizes");
                        }
                        // Balanced position (<0.5 pawns)
                        else if (Math.Abs(eval.Value) < 0.5)
                        {
                            reasons.Add(isEndgame ? "balanced endgame" : "maintains balance");
                        }
                    }
                }

                // Ultimate fallback
                if (reasons.Count == 0)
                {
                    // Context-aware generic response
                    if (EndgameAnalysis.IsEndgame(tempBoard))
                        return "improves endgame position";
                    else
                        return "improves position";
                }

                return string.Join(", ", reasons.Take(2)); // Limit to 2 reasons for brevity
            }
            catch
            {
                return "best move by engine";
            }
        }

        // Parse evaluation string to numeric value for comparison
        public static double? ParseEvaluation(string evalStr)
        {
            if (string.IsNullOrEmpty(evalStr)) return null;

            // Handle mate scores
            if (evalStr.Contains("Mate"))
            {
                // Treat mate as very high advantage (100 for checkmate advantage)
                return evalStr.Contains("-") ? -100.0 : 100.0;
            }

            // Parse numeric evaluation
            if (double.TryParse(evalStr.TrimStart('+'), out double eval))
            {
                return eval;
            }

            return null;
        }

        // Extract evaluation from PV line (format: "e4 e5 Nf3 (+0.5)")
        private static string ExtractEvalFromPV(string pvLine)
        {
            try
            {
                if (string.IsNullOrEmpty(pvLine)) return "";

                // Look for evaluation in parentheses at end
                int lastParen = pvLine.LastIndexOf('(');
                if (lastParen >= 0)
                {
                    int closeParen = pvLine.IndexOf(')', lastParen);
                    if (closeParen > lastParen)
                    {
                        string evalPart = pvLine.Substring(lastParen + 1, closeParen - lastParen - 1).Trim();
                        return evalPart;
                    }
                }

                return "";
            }
            catch
            {
                return "";
            }
        }

        // Detect if there was a blunder based on evaluation change
        // whoMovedLast: true = White just moved, false = Black just moved
        public static (bool isBlunder, string blunderType, double evalDrop, bool whiteBlundered) DetectBlunder(
            double? currentEval, double? prevEval, bool whoMovedLast)
        {
            if (!currentEval.HasValue || !prevEval.HasValue)
                return (false, "", 0, false);

            // Calculate evaluation change from the previous position to the current position
            // Positive evalChange means the position got better for White
            // Negative evalChange means the position got better for Black
            double evalChange = currentEval.Value - prevEval.Value;

            // Determine who blundered based on who moved and how the eval changed
            bool whiteBlundered = false;
            double evalDrop = 0;

            if (whoMovedLast) // White just moved
            {
                // If White moved and eval dropped (became more negative), White blundered
                if (evalChange < 0)
                {
                    evalDrop = Math.Abs(evalChange);
                    whiteBlundered = true;
                }
            }
            else // Black just moved
            {
                // If Black moved and eval increased (became more positive for White), Black blundered
                if (evalChange > 0)
                {
                    evalDrop = Math.Abs(evalChange);
                    whiteBlundered = false;
                }
            }

            // Blunder thresholds
            if (evalDrop >= 3.0) return (true, "Blunder", evalDrop, whiteBlundered);
            if (evalDrop >= 1.5) return (true, "Mistake", evalDrop, whiteBlundered);
            if (evalDrop >= 0.75) return (true, "Inaccuracy", evalDrop, whiteBlundered);

            return (false, "", evalDrop, whiteBlundered);
        }

        // Detect tactical patterns like forks, pins, skewers, discovered attacks
        private static string? DetectTacticalPattern(ChessBoard originalBoard, ChessBoard board, int srcRow, int srcCol, int pieceRow, int pieceCol, char piece, PieceType pieceType, bool isWhite)
        {
            try
            {
                // Validate piece position
                if (pieceRow < 0 || pieceRow >= 8 || pieceCol < 0 || pieceCol >= 8)
                    return null;

                List<(int row, int col, PieceType type)> attackedPieces = new List<(int, int, PieceType)>();

                // Find all enemy pieces this piece attacks
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char target = board.GetPiece(r, c);
                        if (target == '.') continue;
                        if (char.IsUpper(target) == isWhite) continue; // Same color

                        // Check if our piece attacks this square
                        if (ChessUtilities.CanAttackSquare(board, pieceRow, pieceCol, piece, r, c))
                        {
                            attackedPieces.Add((r, c, PieceHelper.GetPieceType(target)));
                        }
                    }
                }

                // DOUBLE CHECK: Check if moving this piece reveals a check from another piece
                var doubleCheckInfo = DetectDoubleCheck(board, pieceRow, pieceCol, piece, isWhite);
                if (!string.IsNullOrEmpty(doubleCheckInfo))
                {
                    return doubleCheckInfo;
                }

                // DISCOVERED ATTACK: Check if this piece reveals an attack from another piece
                // Use ORIGINAL board to check if piece was blocking, and NEW board to see what's revealed
                var discoveredAttackInfo = DetectDiscoveredAttack(originalBoard, board, srcRow, srcCol, pieceRow, pieceCol, piece, isWhite);
                if (!string.IsNullOrEmpty(discoveredAttackInfo))
                {
                    return discoveredAttackInfo;
                }

                // PIN: Check if this piece pins an enemy piece (highest priority after discovered checks)
                var pinInfo = DetectPin(board, pieceRow, pieceCol, piece, isWhite);
                if (!string.IsNullOrEmpty(pinInfo))
                {
                    return pinInfo;
                }

                // SKEWER: Check if this piece skewers an enemy piece
                var skewerInfo = DetectSkewer(board, pieceRow, pieceCol, piece, isWhite);
                if (!string.IsNullOrEmpty(skewerInfo))
                {
                    return skewerInfo;
                }

                // FORK: One piece attacks two or more enemy pieces at once
                if (attackedPieces.Count >= 2)
                {
                    bool hasKing = attackedPieces.Any(p => p.type == PieceType.King);

                    // Royal fork: Knight forks king and queen/rook/bishop/knight
                    // King must move when in check, so we can guarantee capturing the other piece
                    if (hasKing && pieceType == PieceType.Knight)
                    {
                        var otherPieces = attackedPieces.Where(p => p.type != PieceType.King).ToList();

                        // Check if we can actually win material from the other piece
                        foreach (var other in otherPieces)
                        {
                            int otherValue = ChessUtilities.GetPieceValue(other.type);
                            int forkingPieceValue = ChessUtilities.GetPieceValue(pieceType); // Knight = 3

                            if (other.type == PieceType.Queen)
                            {
                                // Queen is always worth winning, even if defended (9 for 3 = +6)
                                return "royal fork (king and queen)";
                            }
                            else if (other.type == PieceType.Rook)
                            {
                                // Rook (5) for Knight (3) = +2 material - always good
                                return "forks king and rook";
                            }
                            else if (other.type == PieceType.Bishop || other.type == PieceType.Knight)
                            {
                                // Bishop/Knight (3) for Knight (3) = equal trade
                                // Only report as fork if:
                                // 1. Target is UNDEFENDED (free piece), OR
                                // 2. Target has NO SAFE ESCAPE SQUARES (trapped by fork)
                                bool isDefended = ChessUtilities.IsSquareDefended(board, other.row, other.col, !isWhite);

                                if (!isDefended)
                                {
                                    return $"forks king and {ChessUtilities.GetPieceName(other.type)}";
                                }

                                // If defended, check if it has escape squares
                                // King must move from check, so forked piece must decide: stay and die, or run
                                if (isDefended)
                                {
                                    int safeSquares = CountSafeSquaresForPiece(board, other.row, other.col,
                                        board.GetPiece(other.row, other.col), !isWhite);

                                    // If piece has no safe squares to escape to, it's still a winning fork
                                    if (safeSquares == 0)
                                    {
                                        return $"forks king and {ChessUtilities.GetPieceName(other.type)}";
                                    }
                                }
                                // If defended AND has escape squares, it's just an equal trade, not a fork
                            }
                        }
                    }

                    // Family fork: Attacks king, queen, and rook
                    // King must move, so we can guarantee capturing queen or rook
                    if (attackedPieces.Count >= 3 && hasKing &&
                        attackedPieces.Any(p => p.type == PieceType.Queen) &&
                        attackedPieces.Any(p => p.type == PieceType.Rook))
                    {
                        return "family fork (king, queen, and rook)";
                    }

                    // Regular fork: Attack multiple valuable pieces
                    // Only report if we can actually win material
                    var valuableTargets = attackedPieces
                        .Where(p => ChessUtilities.GetPieceValue(p.type) >= 3) // Knights, bishops, rooks, queens, king
                        .OrderByDescending(p => ChessUtilities.GetPieceValue(p.type))
                        .Take(2)
                        .ToList();

                    if (valuableTargets.Count >= 2)
                    {
                        // Check if at least one of the forked pieces can be profitably captured
                        int forkingPieceValue = ChessUtilities.GetPieceValue(pieceType);
                        bool canWinMaterial = false;

                        foreach (var target in valuableTargets)
                        {
                            // If one of the targets is a king, it must move (guaranteed win of the other piece)
                            if (target.type == PieceType.King)
                            {
                                canWinMaterial = true;
                                break;
                            }

                            // Check if the forked piece can recapture our forking piece
                            bool targetCanRecapture = ChessUtilities.CanAttackSquare(board, target.row, target.col,
                                board.GetPiece(target.row, target.col), pieceRow, pieceCol);

                            // If target can recapture our forking piece, we need the trade to be profitable
                            if (targetCanRecapture)
                            {
                                // Only valid if target is worth more than our forking piece (profitable trade)
                                if (ChessUtilities.GetPieceValue(target.type) > forkingPieceValue)
                                {
                                    // But also check: is there another forked piece that can't recapture?
                                    // If so, we can capture that one instead
                                    var otherTargets = valuableTargets.Where(t => t.row != target.row || t.col != target.col);
                                    foreach (var other in otherTargets)
                                    {
                                        bool otherCanRecapture = ChessUtilities.CanAttackSquare(board, other.row, other.col,
                                            board.GetPiece(other.row, other.col), pieceRow, pieceCol);

                                        if (!otherCanRecapture)
                                        {
                                            // This piece can't recapture, so we can win it for free
                                            canWinMaterial = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Target can't recapture - check if it's defended by another piece
                                bool isDefended = ChessUtilities.IsSquareDefended(board, target.row, target.col, !isWhite);

                                // Can win material if:
                                // 1. Target is undefended (free capture), OR
                                // 2. Target is defended but worth more than the forking piece (profitable trade)
                                if (!isDefended || ChessUtilities.GetPieceValue(target.type) > forkingPieceValue)
                                {
                                    canWinMaterial = true;
                                    break;
                                }
                            }

                            if (canWinMaterial) break;
                        }

                        if (canWinMaterial)
                        {
                            string pieces = string.Join(" and ", valuableTargets.Select(p => ChessUtilities.GetPieceName(p.type)));
                            return $"forks {pieces}";
                        }
                    }
                }

                // REMOVAL OF DEFENDER: Check if capturing this piece removes a key defender
                var removalInfo = DetectRemovalOfDefender(board, pieceRow, pieceCol, isWhite);
                if (!string.IsNullOrEmpty(removalInfo))
                {
                    return removalInfo;
                }

                // TRAPPED PIECE: Check if this move traps an enemy piece
                var trappedInfo = DetectTrappedPiece(board, pieceRow, pieceCol, isWhite);
                if (!string.IsNullOrEmpty(trappedInfo))
                {
                    return trappedInfo;
                }

                // HANGING PIECE: Winning an undefended piece
                var hangingInfo = DetectHangingPiece(attackedPieces, board, isWhite, pieceType, pieceRow, pieceCol);
                if (!string.IsNullOrEmpty(hangingInfo))
                {
                    return hangingInfo;
                }

                // OVERLOADING: Check if this move exploits an overloaded defender
                var overloadInfo = DetectOverloading(board, pieceRow, pieceCol, isWhite);
                if (!string.IsNullOrEmpty(overloadInfo))
                {
                    return overloadInfo;
                }

                // BACK RANK WEAKNESS: Check if this threatens back rank mate
                var backRankInfo = DetectBackRankThreat(board, pieceRow, pieceCol, piece, isWhite);
                if (!string.IsNullOrEmpty(backRankInfo))
                {
                    return backRankInfo;
                }

                // DEFLECTION: Check if capturing this piece deflects a key defender
                var deflectionInfo = DetectDeflection(board, pieceRow, pieceCol, isWhite);
                if (!string.IsNullOrEmpty(deflectionInfo))
                {
                    return deflectionInfo;
                }

                // PROMOTION THREAT: Check if pawn is close to promoting
                var promotionInfo = DetectPromotionThreat(board, pieceRow, pieceCol, piece, isWhite);
                if (!string.IsNullOrEmpty(promotionInfo))
                {
                    return promotionInfo;
                }

                // SMOTHERED MATE: Knight delivers mate to smothered king
                var smotheredMateInfo = DetectSmotheredMate(board, pieceRow, pieceCol, piece, isWhite);
                if (!string.IsNullOrEmpty(smotheredMateInfo))
                {
                    return smotheredMateInfo;
                }

                // DOUBLE ATTACK: Attack two pieces with different threats
                var doubleAttackInfo = DetectDoubleAttack(board, pieceRow, pieceCol, piece, isWhite);
                if (!string.IsNullOrEmpty(doubleAttackInfo))
                {
                    return doubleAttackInfo;
                }

                // X-RAY ATTACK: Attack through another piece
                var xrayInfo = DetectXRayAttack(board, pieceRow, pieceCol, piece, isWhite);
                if (!string.IsNullOrEmpty(xrayInfo))
                {
                    return xrayInfo;
                }

                // DECOY: Sacrifice to lure piece to bad square
                var decoyInfo = DetectDecoy(board, pieceRow, pieceCol, isWhite);
                if (!string.IsNullOrEmpty(decoyInfo))
                {
                    return decoyInfo;
                }

                // Check for check
                if (IsGivingCheck(board, pieceRow, pieceCol, piece, isWhite))
                {
                    if (attackedPieces.Count >= 1)
                    {
                        return "check with attack"; // Check + attacking another piece
                    }
                    return "gives check";
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DetectTacticalPattern: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        // Check if a piece can attack a specific square
        // Moved to ChessUtilities.CanAttackSquare and ChessUtilities.IsPathClear

        // Detect if this piece is pinning an enemy piece
        // PIN: "A pin is a tactic where a long-range piece attacks an opponent's piece, preventing it
        // from moving because it would expose a more valuable piece or the king behind it."
        // IMPORTANT: If the piece behind is defended and equal/lesser value trade, it's not exploitable
        private static string? DetectPin(ChessBoard board, int pieceRow, int pieceCol, char piece, bool isWhite)
        {
            try
            {
                PieceType pieceType = PieceHelper.GetPieceType(piece);
                int pinnerValue = ChessUtilities.GetPieceValue(pieceType);

                // Only sliding pieces (Queen, Rook, Bishop) can pin
                if (pieceType != PieceType.Bishop && pieceType != PieceType.Rook && pieceType != PieceType.Queen)
                    return null;

                // Check all 8 directions
                int[][] directions = pieceType == PieceType.Bishop ? new[] { new[] { 1, 1 }, new[] { 1, -1 }, new[] { -1, 1 }, new[] { -1, -1 } } :
                                     pieceType == PieceType.Rook ? new[] { new[] { 0, 1 }, new[] { 0, -1 }, new[] { 1, 0 }, new[] { -1, 0 } } :
                                     new[] { new[] { 1, 1 }, new[] { 1, -1 }, new[] { -1, 1 }, new[] { -1, -1 }, new[] { 0, 1 }, new[] { 0, -1 }, new[] { 1, 0 }, new[] { -1, 0 } };

                foreach (var dir in directions)
                {
                    int dR = dir[0];
                    int dF = dir[1];
                    int r = pieceRow + dR;
                    int c = pieceCol + dF;

                    char? firstPiece = null;
                    int firstPieceRow = -1, firstPieceCol = -1;

                    // Scan along the direction
                    while (r >= 0 && r < 8 && c >= 0 && c < 8)
                    {
                        char target = board.GetPiece(r, c);
                        if (target != '.')
                        {
                            if (firstPiece == null)
                            {
                                // Found first piece - must be enemy
                                if (char.IsUpper(target) != isWhite) // Enemy piece
                                {
                                    firstPiece = target;
                                    firstPieceRow = r;
                                    firstPieceCol = c;
                                }
                                else
                                {
                                    break; // Blocked by own piece
                                }
                            }
                            else
                            {
                                // Found second piece behind the first
                                if (char.IsUpper(target) != isWhite) // Enemy piece
                                {
                                    PieceType pinnedPieceType = PieceHelper.GetPieceType(firstPiece.Value);
                                    PieceType behindPieceType = PieceHelper.GetPieceType(target);

                                    int pinnedValue = ChessUtilities.GetPieceValue(pinnedPieceType);
                                    int behindValue = ChessUtilities.GetPieceValue(behindPieceType);

                                    // ABSOLUTE PIN: The piece behind is the King - always report
                                    if (behindPieceType == PieceType.King)
                                    {
                                        return $"pins {ChessUtilities.GetPieceName(pinnedPieceType)} to king (absolute)";
                                    }

                                    // RELATIVE PIN: The piece behind must be more valuable than the pinned piece
                                    // BUT we need to check if exploiting the pin is actually profitable
                                    if (behindValue > pinnedValue)
                                    {
                                        // Check if the piece behind is defended
                                        int defendersOfBehind = CountDefenders(board, r, c, !isWhite);

                                        // Calculate material gain if we capture the behind piece after pinned piece moves
                                        // If behind piece is defended, we'd lose our pinner after capturing
                                        int materialGain = behindValue - (defendersOfBehind > 0 ? pinnerValue : 0);

                                        // Only report pin if:
                                        // 1. Behind piece is undefended (free capture), OR
                                        // 2. Material gain is significant (e.g., win Queen for Rook = +4)
                                        if (defendersOfBehind == 0)
                                        {
                                            // Undefended - definite exploitable pin
                                            return $"pins {ChessUtilities.GetPieceName(pinnedPieceType)} to {ChessUtilities.GetPieceName(behindPieceType)}";
                                        }
                                        else if (materialGain >= 4)
                                        {
                                            // Defended but still very profitable to capture (e.g., Queen behind, we'd get +6)
                                            // Only report as "winning" if we gain at least 4 points (significant advantage)
                                            return $"pins {ChessUtilities.GetPieceName(pinnedPieceType)} to {ChessUtilities.GetPieceName(behindPieceType)}";
                                        }
                                        // If defended and gain is small, don't report - pinned piece can just move
                                    }
                                }
                                break;
                            }
                        }

                        r += dR;
                        c += dF;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // Detect if this piece is skewering enemy pieces
        // SKEWER: A long-range piece attacking TWO pieces in a line, where the MORE VALUABLE piece
        // is in FRONT and forced to move, exposing a LESS VALUABLE piece behind it to capture
        private static string? DetectSkewer(ChessBoard board, int pieceRow, int pieceCol, char piece, bool isWhite)
        {
            try
            {
                PieceType pieceType = PieceHelper.GetPieceType(piece);

                // Only sliding pieces (Queen, Rook, Bishop) can skewer
                if (pieceType != PieceType.Bishop && pieceType != PieceType.Rook && pieceType != PieceType.Queen)
                    return null;

                // Check all 8 directions
                int[][] directions = pieceType == PieceType.Bishop ? new[] { new[] { 1, 1 }, new[] { 1, -1 }, new[] { -1, 1 }, new[] { -1, -1 } } :
                                     pieceType == PieceType.Rook ? new[] { new[] { 0, 1 }, new[] { 0, -1 }, new[] { 1, 0 }, new[] { -1, 0 } } :
                                     new[] { new[] { 1, 1 }, new[] { 1, -1 }, new[] { -1, 1 }, new[] { -1, -1 }, new[] { 0, 1 }, new[] { 0, -1 }, new[] { 1, 0 }, new[] { -1, 0 } };

                foreach (var dir in directions)
                {
                    int dR = dir[0];
                    int dF = dir[1];
                    int r = pieceRow + dR;
                    int c = pieceCol + dF;

                    char? frontPiece = null;
                    int frontRow = -1, frontCol = -1;

                    // Scan along the direction
                    while (r >= 0 && r < 8 && c >= 0 && c < 8)
                    {
                        char target = board.GetPiece(r, c);
                        if (target != '.')
                        {
                            if (frontPiece == null)
                            {
                                // Found first piece - must be enemy
                                if (char.IsUpper(target) != isWhite) // Enemy piece
                                {
                                    frontPiece = target;
                                    frontRow = r;
                                    frontCol = c;
                                }
                                else
                                {
                                    break; // Blocked by own piece
                                }
                            }
                            else
                            {
                                // Found second piece behind the first
                                if (char.IsUpper(target) != isWhite) // Enemy piece
                                {
                                    PieceType frontPieceType = PieceHelper.GetPieceType(frontPiece.Value);
                                    PieceType behindPieceType = PieceHelper.GetPieceType(target);
                                    int frontValue = ChessUtilities.GetPieceValue(frontPieceType);
                                    int behindValue = ChessUtilities.GetPieceValue(behindPieceType);

                                    // CRITICAL: SKEWER requires front piece MORE valuable than behind piece
                                    // King is always considered most valuable for skewers (even against Queen)
                                    bool isSkewerPattern = false;

                                    if (frontPieceType == PieceType.King)
                                    {
                                        // King in front is ALWAYS a skewer (must move, exposes piece behind)
                                        isSkewerPattern = true;
                                    }
                                    else if (frontValue > behindValue)
                                    {
                                        // Front piece more valuable than behind piece
                                        isSkewerPattern = true;
                                    }

                                    if (isSkewerPattern)
                                    {
                                        // Check if the piece behind is undefended (will be won)
                                        // OR if even when defended, capturing it wins material
                                        bool behindDefended = ChessUtilities.IsSquareDefended(board, r, c, !isWhite);

                                        if (!behindDefended)
                                        {
                                            // Undefended - clean skewer
                                            if (frontPieceType == PieceType.King)
                                                return $"skewers king, winning {ChessUtilities.GetPieceName(behindPieceType)}";
                                            else
                                                return $"skewers {ChessUtilities.GetPieceName(frontPieceType)}, winning {ChessUtilities.GetPieceName(behindPieceType)}";
                                        }
                                        else
                                        {
                                            // Defended - check if we still win material after trade
                                            // (This happens when front piece moves and we can favorably capture behind)
                                            int defenders = CountDefenders(board, r, c, !isWhite);

                                            // If only defended once and behind piece value >= 3, still report
                                            if (defenders == 1 && behindValue >= 3)
                                            {
                                                if (frontPieceType == PieceType.King)
                                                    return $"skewers king, wins material";
                                                else
                                                    return $"skewers {ChessUtilities.GetPieceName(frontPieceType)}, wins material";
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                        }

                        r += dR;
                        c += dF;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // Check if this piece is giving check to enemy king
        private static bool IsGivingCheck(ChessBoard board, int pieceRow, int pieceCol, char piece, bool isWhite)
        {
            try
            {
                // Find enemy king
                char enemyKing = isWhite ? 'k' : 'K';

                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        if (board.GetPiece(r, c) == enemyKing)
                        {
                            return ChessUtilities.CanAttackSquare(board, pieceRow, pieceCol, piece, r, c);
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        // Check if a square is defended by pieces of the specified color
        // Moved to ChessUtilities.IsSquareDefended

        // Detect discovered attack: when moving a piece reveals an attack from another piece
        private static string? DetectDiscoveredAttack(ChessBoard originalBoard, ChessBoard newBoard, int srcRow, int srcCol, int destRow, int destCol, char movedPiece, bool isWhite)
        {
            try
            {
                // Check all friendly pieces that could potentially create a discovered attack
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        if (r == destRow && c == destCol) continue; // Skip the piece that just moved (at new position)

                        char piece = newBoard.GetPiece(r, c);
                        if (piece == '.') continue;
                        if (char.IsUpper(piece) != isWhite) continue; // Only check friendly pieces

                        PieceType pieceType = PieceHelper.GetPieceType(piece);

                        // Only sliding pieces can create discovered attacks
                        if (pieceType != PieceType.Bishop && pieceType != PieceType.Rook && pieceType != PieceType.Queen)
                            continue;

                        // Check if the moved piece was blocking this piece's line of attack
                        // Find enemy pieces that this sliding piece now attacks (on NEW board)
                        for (int targetR = 0; targetR < 8; targetR++)
                        {
                            for (int targetC = 0; targetC < 8; targetC++)
                            {
                                char target = newBoard.GetPiece(targetR, targetC);
                                if (target == '.') continue;
                                if (char.IsUpper(target) == isWhite) continue; // Must be enemy

                                // Check if this piece can attack the target on the NEW board (after move)
                                bool canAttackNow = ChessUtilities.CanAttackSquare(newBoard, r, c, piece, targetR, targetC);

                                // Check if this piece could NOT attack the target on ORIGINAL board (was blocked)
                                bool wasBlockedBefore = !ChessUtilities.CanAttackSquare(originalBoard, r, c, piece, targetR, targetC);

                                // DISCOVERED ATTACK: Could not attack before (blocked), can attack now (revealed)
                                if (canAttackNow && wasBlockedBefore)
                                {
                                    // Verify the moved piece was on the line between attacker and target
                                    if (WasOnLine(r, c, targetR, targetC, srcRow, srcCol))
                                    {
                                        PieceType targetType = PieceHelper.GetPieceType(target);
                                        PieceType movingPieceType = PieceHelper.GetPieceType(movedPiece);

                                        if (targetType == PieceType.King)
                                        {
                                            // Check if the moving piece also attacks something valuable
                                            // Example: Bh3+ (discovered check) and the bishop also attacks the rook on h1
                                            for (int attR = 0; attR < 8; attR++)
                                            {
                                                for (int attC = 0; attC < 8; attC++)
                                                {
                                                    char attackedPiece = newBoard.GetPiece(attR, attC);
                                                    if (attackedPiece == '.' || char.IsUpper(attackedPiece) == isWhite) continue;

                                                    if (ChessUtilities.CanAttackSquare(newBoard, destRow, destCol, movedPiece, attR, attC))
                                                    {
                                                        PieceType attackedType = PieceHelper.GetPieceType(attackedPiece);
                                                        if (ChessUtilities.GetPieceValue(attackedType) >= 5) // Rook or Queen
                                                        {
                                                            return $"discovered check, wins {ChessUtilities.GetPieceName(attackedType)}";
                                                        }
                                                    }
                                                }
                                            }
                                            return "discovered check";
                                        }
                                        else if (ChessUtilities.GetPieceValue(targetType) >= 5) // Rook or Queen
                                            return $"discovered attack on {ChessUtilities.GetPieceName(targetType)}";
                                    }
                                }
                            }
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // Check if a point is on the line between two other points
        private static bool WasOnLine(int fromR, int fromC, int toR, int toC, int pointR, int pointC)
        {
            // Check if point is between from and to
            int dR = Math.Sign(toR - fromR);
            int dC = Math.Sign(toC - fromC);

            int r = fromR + dR;
            int c = fromC + dC;

            while (r != toR || c != toC)
            {
                if (r == pointR && c == pointC)
                    return true;

                r += dR;
                c += dC;
            }

            return false;
        }

        // Detect double check: moving piece gives check AND reveals check from another piece
        private static string? DetectDoubleCheck(ChessBoard board, int movedRow, int movedCol, char movedPiece, bool isWhite)
        {
            try
            {
                // First, check if the moved piece itself gives check
                if (!IsGivingCheck(board, movedRow, movedCol, movedPiece, isWhite))
                    return null;

                // Now check if there's also a discovered check
                char enemyKing = isWhite ? 'k' : 'K';
                int kingR = -1, kingC = -1;

                // Find enemy king
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        if (board.GetPiece(r, c) == enemyKing)
                        {
                            kingR = r;
                            kingC = c;
                            break;
                        }
                    }
                    if (kingR != -1) break;
                }

                if (kingR == -1) return null;

                // Check if any other friendly piece is also giving check
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        if (r == movedRow && c == movedCol) continue;

                        char piece = board.GetPiece(r, c);
                        if (piece == '.') continue;
                        if (char.IsUpper(piece) != isWhite) continue;

                        if (ChessUtilities.CanAttackSquare(board, r, c, piece, kingR, kingC))
                        {
                            return "double check!";
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // Detect removal of defender: capturing a piece that was defending something important
        private static string? DetectRemovalOfDefender(ChessBoard board, int pieceRow, int pieceCol, bool isWhite)
        {
            try
            {
                char targetPiece = board.GetPiece(pieceRow, pieceCol);
                if (targetPiece == '.') return null;
                if (char.IsUpper(targetPiece) == isWhite) return null; // Can't capture own piece

                // Check what this enemy piece is defending
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char defendedPiece = board.GetPiece(r, c);
                        if (defendedPiece == '.') continue;
                        if (char.IsUpper(defendedPiece) == isWhite) continue; // Must be enemy piece

                        // Check if target piece defends this square
                        if (ChessUtilities.CanAttackSquare(board, pieceRow, pieceCol, targetPiece, r, c))
                        {
                            // Check if the defended piece would be hanging after removal
                            // Count defenders after removing this piece
                            int defenderCount = 0;
                            for (int dr = 0; dr < 8; dr++)
                            {
                                for (int dc = 0; dc < 8; dc++)
                                {
                                    if (dr == pieceRow && dc == pieceCol) continue; // Skip piece being removed

                                    char defender = board.GetPiece(dr, dc);
                                    if (defender == '.') continue;
                                    if (char.IsUpper(defender) == isWhite) continue; // Must be enemy

                                    if (ChessUtilities.CanAttackSquare(board, dr, dc, defender, r, c))
                                        defenderCount++;
                                }
                            }

                            if (defenderCount == 0) // Would be undefended
                            {
                                PieceType defendedType = PieceHelper.GetPieceType(defendedPiece);
                                if (ChessUtilities.GetPieceValue(defendedType) >= 3) // Valuable piece
                                {
                                    return $"removes defender of {ChessUtilities.GetPieceName(defendedType)}";
                                }
                            }
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // Detect trapped piece: an enemy piece with no safe squares
        // NOTE: This check is only meaningful if our move is what CAUSED the trap
        // We should only look at pieces that are:
        // 1. Directly attacked by our moved piece, OR
        // 2. Very close to our moved piece (within 2 squares)
        private static string? DetectTrappedPiece(ChessBoard board, int pieceRow, int pieceCol, bool isWhite)
        {
            try
            {
                // Only check pieces near our moved piece (within 2-3 squares)
                // This prevents false positives where we report trapping a piece on the other side of the board
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char targetPiece = board.GetPiece(r, c);
                        if (targetPiece == '.') continue;
                        if (char.IsUpper(targetPiece) == isWhite) continue; // Must be enemy

                        PieceType targetType = PieceHelper.GetPieceType(targetPiece);

                        // Check high-value pieces (not pawns)
                        if (ChessUtilities.GetPieceValue(targetType) < 3) continue;

                        // CRITICAL FIX: Only check pieces that are close to our moved piece
                        // Calculate Manhattan distance (sum of row and column differences)
                        int distance = Math.Abs(r - pieceRow) + Math.Abs(c - pieceCol);

                        // Only check pieces within 3 squares (Manhattan distance)
                        // This ensures we only report trapping if our move is likely involved
                        if (distance > 3) continue;

                        // Count safe AND profitable escape squares for this piece
                        int safeSquares = 0;
                        int targetPieceValue = ChessUtilities.GetPieceValue(targetType);

                        // Check all possible moves for the potentially trapped piece
                        for (int dr = -7; dr <= 7; dr++)
                        {
                            for (int dc = -7; dc <= 7; dc++)
                            {
                                if (dr == 0 && dc == 0) continue;

                                int newR = r + dr;
                                int newC = c + dc;

                                if (newR < 0 || newR >= 8 || newC < 0 || newC >= 8) continue;

                                // Check if piece can move to this square
                                if (ChessUtilities.CanAttackSquare(board, r, c, targetPiece, newR, newC))
                                {
                                    char destPiece = board.GetPiece(newR, newC);

                                    // Can't move to square with friendly piece (same color as trapped piece)
                                    if (destPiece != '.' && char.IsUpper(destPiece) == char.IsUpper(targetPiece))
                                        continue;

                                    // Check if square would be safe (not attacked by our pieces)
                                    bool isSquareSafe = !ChessUtilities.IsSquareDefended(board, newR, newC, isWhite);

                                    if (isSquareSafe)
                                    {
                                        // Square is safe - piece can escape for free
                                        safeSquares++;
                                    }
                                    else
                                    {
                                        // Square is attacked - check if it's a favorable/equal trade
                                        // Example: Bishop can capture pawn on b6, but bishop (3) for pawn (1) is a losing trade
                                        if (destPiece != '.')
                                        {
                                            int capturedValue = ChessUtilities.GetPieceValue(PieceHelper.GetPieceType(destPiece));
                                            // Only count as escape if the trade is favorable or equal
                                            // (capturing piece worth >= our piece value)
                                            if (capturedValue >= targetPieceValue)
                                            {
                                                safeSquares++;
                                            }
                                        }
                                        // If empty square but defended, it's not safe - don't count it
                                    }
                                }
                            }
                        }

                        // CRITICAL: A piece is only "trapped" if:
                        // 1. It has no safe escape squares, AND
                        // 2. It's actually under attack (threatened)
                        // A well-defended piece with no mobility is NOT trapped
                        if (safeSquares == 0 && targetType != PieceType.Pawn)
                        {
                            // Check if the piece is actually under attack
                            bool isUnderAttack = ChessUtilities.IsSquareDefended(board, r, c, isWhite);

                            if (isUnderAttack)
                            {
                                // Now check if it's adequately defended
                                // If defenders >= attackers, it's not really trapped
                                int defenders = CountDefenders(board, r, c, !isWhite);
                                int attackers = CountDefenders(board, r, c, isWhite);

                                // Only report as trapped if:
                                // - Under attack AND
                                // - Either undefended OR attackers outnumber defenders
                                if (defenders == 0 || attackers > defenders)
                                {
                                    return $"traps {ChessUtilities.GetPieceName(targetType)}";
                                }
                            }
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // Detect hanging piece: winning an undefended valuable piece
        // NOTE: This method is called AFTER the move is made on tempBoard, so we check the destination square
        private static string? DetectHangingPiece(List<(int row, int col, PieceType type)> attackedPieces,
            ChessBoard board, bool isWhite, PieceType attackerType, int attackerRow, int attackerCol)
        {
            try
            {
                int attackerValue = ChessUtilities.GetPieceValue(attackerType);

                foreach (var target in attackedPieces)
                {
                    int targetValue = ChessUtilities.GetPieceValue(target.type);

                    // Only report if we're winning material (target worth more or equal and undefended)
                    if (targetValue >= attackerValue || targetValue >= 3)
                    {
                        // Check if target is defended
                        bool isDefended = ChessUtilities.IsSquareDefended(board, target.row, target.col, !isWhite);

                        if (!isDefended && target.type != PieceType.King)
                        {
                            // CRITICAL: Check if our attacking piece can be recaptured at its NEW position
                            // This prevents false positives like "wins undefended knight" when we can be recaptured
                            bool weCanBeRecaptured = ChessUtilities.IsSquareDefended(board, attackerRow, attackerCol, !isWhite);

                            // ADDITIONAL CHECK: Can the target piece escape to a safe square?
                            // Example: Knight attacked by pawn, but knight has safe squares to retreat to
                            char targetPiece = board.GetPiece(target.row, target.col);
                            int safeEscapeSquares = CountSafeSquaresForPiece(board, target.row, target.col, targetPiece, !isWhite);

                            // Only report as "wins undefended piece" if:
                            // 1. Target has NO safe escape squares (trapped), AND
                            // 2. Either we can't be recaptured OR we're winning the trade
                            if (safeEscapeSquares == 0)
                            {
                                if (weCanBeRecaptured)
                                {
                                    // We can be recaptured, so only report if we're winning the trade
                                    if (targetValue > attackerValue)
                                    {
                                        return $"wins undefended {ChessUtilities.GetPieceName(target.type)}";
                                    }
                                }
                                else
                                {
                                    // We can't be recaptured, so it's a free piece
                                    return $"wins undefended {ChessUtilities.GetPieceName(target.type)}";
                                }
                            }
                            // If target has escape squares, it's not really hanging - it can just move away
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // Detect overloading: a piece defending multiple critical pieces/squares
        private static string? DetectOverloading(ChessBoard board, int pieceRow, int pieceCol, bool isWhite)
        {
            try
            {
                char targetPiece = board.GetPiece(pieceRow, pieceCol);
                if (targetPiece == '.') return null;
                if (char.IsUpper(targetPiece) == isWhite) return null; // Can't capture own piece

                // Find what this enemy piece is defending
                List<(int row, int col, PieceType type)> defendedPieces = new List<(int, int, PieceType)>();

                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char defendedPiece = board.GetPiece(r, c);
                        if (defendedPiece == '.') continue;
                        if (char.IsUpper(defendedPiece) == isWhite) continue; // Must be enemy piece

                        // Check if target piece defends this square AND the piece is under attack
                        if (ChessUtilities.CanAttackSquare(board, pieceRow, pieceCol, targetPiece, r, c))
                        {
                            // Check if this defended piece is also under attack by us
                            bool isUnderAttack = ChessUtilities.IsSquareDefended(board, r, c, isWhite);
                            if (isUnderAttack)
                            {
                                PieceType defendedType = PieceHelper.GetPieceType(defendedPiece);
                                if (ChessUtilities.GetPieceValue(defendedType) >= 3) // Valuable piece
                                {
                                    defendedPieces.Add((r, c, defendedType));
                                }
                            }
                        }
                    }
                }

                // If defending 2+ valuable pieces under attack, it's overloaded
                if (defendedPieces.Count >= 2)
                {
                    var pieces = string.Join(" and ", defendedPieces.Take(2).Select(p => ChessUtilities.GetPieceName(p.type)));
                    return $"overloads defender of {pieces}";
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // Detect back rank threat: rook/queen on back rank with trapped king
        private static string? DetectBackRankThreat(ChessBoard board, int pieceRow, int pieceCol, char piece, bool isWhite)
        {
            try
            {
                PieceType pieceType = PieceHelper.GetPieceType(piece);

                // Only rooks and queens can deliver back rank threats
                if (pieceType != PieceType.Rook && pieceType != PieceType.Queen)
                    return null;

                // Check if we're on the enemy's back rank
                int enemyBackRank = isWhite ? 0 : 7;
                if (pieceRow != enemyBackRank)
                    return null;

                // Find enemy king
                char enemyKing = isWhite ? 'k' : 'K';
                int kingRow = -1, kingCol = -1;

                for (int c = 0; c < 8; c++)
                {
                    if (board.GetPiece(enemyBackRank, c) == enemyKing)
                    {
                        kingRow = enemyBackRank;
                        kingCol = c;
                        break;
                    }
                }

                if (kingRow == -1) return null;

                // Check if king is trapped (no escape squares on rank 2/7)
                int escapeRank = isWhite ? 1 : 6;
                int safeEscapes = 0;

                for (int dc = -1; dc <= 1; dc++)
                {
                    int checkCol = kingCol + dc;
                    if (checkCol < 0 || checkCol >= 8) continue;

                    char escapeSquare = board.GetPiece(escapeRank, checkCol);
                    // Empty or enemy piece, and not defended
                    if ((escapeSquare == '.' || char.IsUpper(escapeSquare) == isWhite) &&
                        !ChessUtilities.IsSquareDefended(board, escapeRank, checkCol, isWhite))
                    {
                        safeEscapes++;
                    }
                }

                // If king has no/few escape squares and we're attacking the back rank
                if (safeEscapes == 0)
                {
                    // Check if we're giving check or threatening mate
                    if (ChessUtilities.CanAttackSquare(board, pieceRow, pieceCol, piece, kingRow, kingCol))
                        return "back rank mate threat";
                    else
                        return "threatens back rank";
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // Detect deflection: forcing a key defender away
        private static string? DetectDeflection(ChessBoard board, int pieceRow, int pieceCol, bool isWhite)
        {
            try
            {
                char targetPiece = board.GetPiece(pieceRow, pieceCol);
                if (targetPiece == '.') return null;
                if (char.IsUpper(targetPiece) == isWhite) return null; // Can't capture own piece

                // Very similar to removal of defender, but specifically when capturing forces the piece away
                // Check if this piece is defending the king or a critical square
                char enemyKing = isWhite ? 'k' : 'K';

                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char piece = board.GetPiece(r, c);
                        if (piece != enemyKing) continue;

                        // Check if target piece defends a square around the king
                        for (int dr = -1; dr <= 1; dr++)
                        {
                            for (int dc = -1; dc <= 1; dc++)
                            {
                                if (dr == 0 && dc == 0) continue;
                                int defRow = r + dr;
                                int defCol = c + dc;

                                if (defRow < 0 || defRow >= 8 || defCol < 0 || defCol >= 8) continue;

                                // Check if target defends this square near the king
                                if (ChessUtilities.CanAttackSquare(board, pieceRow, pieceCol, targetPiece, defRow, defCol))
                                {
                                    // Check if we're attacking that same square
                                    if (ChessUtilities.IsSquareDefended(board, defRow, defCol, isWhite))
                                    {
                                        return "deflects key defender";
                                    }
                                }
                            }
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // Detect promotion threat: pawn close to queening
        private static string? DetectPromotionThreat(ChessBoard board, int pieceRow, int pieceCol, char piece, bool isWhite)
        {
            try
            {
                PieceType pieceType = PieceHelper.GetPieceType(piece);
                if (pieceType != PieceType.Pawn) return null;

                // Check how far from promotion
                int promotionRank = isWhite ? 0 : 7;
                int distanceToPromotion = Math.Abs(promotionRank - pieceRow);

                if (distanceToPromotion == 1)
                {
                    return "threatens promotion";
                }
                else if (distanceToPromotion == 2)
                {
                    // Check if path is clear
                    int direction = isWhite ? -1 : 1;
                    int nextRow = pieceRow + direction;

                    if (board.GetPiece(nextRow, pieceCol) == '.')
                    {
                        return "advances passed pawn";
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // DOUBLE ATTACK - Attack two or more pieces simultaneously (general case, not just forks)
        private static string? DetectDoubleAttack(ChessBoard board, int pieceRow, int pieceCol, char piece, bool isWhite)
        {
            try
            {
                PieceType pieceType = PieceHelper.GetPieceType(piece);
                var attackedPieces = new List<(int row, int col, PieceType type)>();

                // Find all pieces this piece attacks
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char targetPiece = board.GetPiece(r, c);
                        if (targetPiece == '.' || char.IsWhiteSpace(targetPiece)) continue;
                        bool targetIsWhite = char.IsUpper(targetPiece);
                        if (targetIsWhite == isWhite) continue; // Skip own pieces

                        if (ChessUtilities.CanAttackSquare(board, pieceRow, pieceCol, piece, r, c))
                        {
                            attackedPieces.Add((r, c, PieceHelper.GetPieceType(targetPiece)));
                        }
                    }
                }

                // Check for double attack (2+ pieces attacked) with different threats
                if (attackedPieces.Count >= 2)
                {
                    // Check if one threat is checkmate and another is material
                    bool hasCheckThreat = attackedPieces.Any(p => p.type == PieceType.King);
                    bool hasMaterialThreat = attackedPieces.Any(p => ChessUtilities.GetPieceValue(p.type) >= 3);

                    if (hasCheckThreat && hasMaterialThreat)
                    {
                        return "double attack: check and wins material";
                    }

                    // Two valuable pieces attacked
                    var valuableTargets = attackedPieces.Where(p => ChessUtilities.GetPieceValue(p.type) >= 3).ToList();
                    if (valuableTargets.Count >= 2)
                    {
                        return "double attack on multiple pieces";
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // DECOY SACRIFICE - Intentionally sacrifice a piece to lure an opponent's piece
        // to a vulnerable square, setting up a decisive blow (fork, pin, checkmate)
        // Definition: "Forces the opponent's piece to move to a 'poisoned' square, making it
        // a target for greater material gain or a winning attack"
        // NOTE: This is a complex tactic that requires looking ahead in the PV line.
        // Current implementation is conservative to avoid false positives.
        private static string? DetectDecoy(ChessBoard board, int pieceRow, int pieceCol, bool isWhite)
        {
            try
            {
                char piece = board.GetPiece(pieceRow, pieceCol);
                PieceType pieceType = PieceHelper.GetPieceType(piece);
                int pieceValue = ChessUtilities.GetPieceValue(pieceType);

                // CRITICAL: For it to be a decoy sacrifice, our piece must be:
                // 1. Under attack or can be captured (it's a sacrifice)
                // 2. We're offering it to lure a piece to a bad square

                // First check: Is this piece actually attacked? (prerequisite for sacrifice)
                int theirAttackers = CountDefenders(board, pieceRow, pieceCol, !isWhite);
                int ourDefenders = CountDefenders(board, pieceRow, pieceCol, isWhite);

                // If we're not under attack and not offering a free piece, it's not a decoy sacrifice
                if (theirAttackers == 0)
                    return null;

                // If we have equal or more defenders, capturing would be an even trade, not a sacrifice
                if (ourDefenders >= theirAttackers && pieceValue <= 3)
                    return null;

                // Check what pieces are attacking us - those are the pieces we might be "luring"
                // For a true decoy, if the attacker captures our piece:
                // 1. They move to our square
                // 2. That square becomes vulnerable to a follow-up tactic

                // Create a hypothetical board where enemy captured our piece
                ChessBoard tempBoard = new ChessBoard(board.GetArray());
                char capturer = '.';
                int capturerRow = -1, capturerCol = -1;

                // Find an attacker that could capture
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char enemyPiece = board.GetPiece(r, c);
                        if (enemyPiece == '.') continue;
                        bool enemyIsWhite = char.IsUpper(enemyPiece);
                        if (enemyIsWhite == isWhite) continue;

                        if (ChessUtilities.CanAttackSquare(board, r, c, enemyPiece, pieceRow, pieceCol))
                        {
                            capturer = enemyPiece;
                            capturerRow = r;
                            capturerCol = c;
                            break;
                        }
                    }
                    if (capturer != '.') break;
                }

                if (capturer == '.')
                    return null;

                // Simulate the capture
                tempBoard.SetPiece(pieceRow, pieceCol, capturer);
                tempBoard.SetPiece(capturerRow, capturerCol, '.');

                // Check if the capturer on its new square is now vulnerable to a winning tactic
                // 1. Is it now forkable?
                // 2. Is it now pinnable?
                // 3. Is it trapped (very few safe squares)?

                int capturerSafeSquares = CountSafeSquaresForPiece(tempBoard, pieceRow, pieceCol, capturer, !isWhite);
                PieceType capturerType = PieceHelper.GetPieceType(capturer);
                int capturerValue = ChessUtilities.GetPieceValue(capturerType);

                // If after capturing, the piece has very few safe squares, it might be trapped
                if (capturerSafeSquares <= 1 && capturerValue >= 3)
                {
                    // Check if we can attack this trapped piece
                    if (ChessUtilities.IsSquareDefended(tempBoard, pieceRow, pieceCol, isWhite))
                    {
                        return "decoy sacrifice (traps piece)";
                    }
                }

                // For king attacks specifically: check if king is lured to a worse square
                if (capturerType == PieceType.King)
                {
                    var safeBefore = GetKingSafeSquares(board, capturerRow, capturerCol, !isWhite);
                    var safeAfter = GetKingSafeSquares(tempBoard, pieceRow, pieceCol, !isWhite);

                    // If king has significantly fewer escape squares after capture
                    if (safeAfter.Count <= 2 && safeAfter.Count < safeBefore.Count)
                    {
                        return "decoy sacrifice";
                    }
                }

                // NOTE: Without access to the PV line, we can't fully validate decoy sacrifices
                // that set up complex follow-up tactics. Conservative approach to avoid false positives.
                return null;
            }
            catch
            {
                return null;
            }
        }

        // X-RAY ATTACK - An indirect attack or defense where a long-range piece exerts influence
        // THROUGH an intervening piece, like "Superman vision"
        // Key: The intervening piece could move, revealing the attack on the piece behind
        // IMPORTANT: Only report if we would actually WIN the piece behind (not just attack it)
        private static string? DetectXRayAttack(ChessBoard board, int pieceRow, int pieceCol, char piece, bool isWhite)
        {
            try
            {
                PieceType pieceType = PieceHelper.GetPieceType(piece);
                int attackerValue = ChessUtilities.GetPieceValue(pieceType);

                // Only sliding pieces can create x-ray attacks
                if (pieceType != PieceType.Bishop && pieceType != PieceType.Rook && pieceType != PieceType.Queen)
                    return null;

                // Check all directions this piece can move
                int[][] directions = pieceType switch
                {
                    PieceType.Rook => new[] { new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 } },
                    PieceType.Bishop => new[] { new[] { 1, 1 }, new[] { 1, -1 }, new[] { -1, 1 }, new[] { -1, -1 } },
                    PieceType.Queen => new[] { new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 },
                                              new[] { 1, 1 }, new[] { 1, -1 }, new[] { -1, 1 }, new[] { -1, -1 } },
                    _ => Array.Empty<int[]>()
                };

                foreach (var dir in directions)
                {
                    int firstPieceRow = -1, firstPieceCol = -1;
                    int secondPieceRow = -1, secondPieceCol = -1;
                    char firstPiece = '.', secondPiece = '.';

                    // Scan along the line
                    for (int step = 1; step < 8; step++)
                    {
                        int r = pieceRow + dir[0] * step;
                        int c = pieceCol + dir[1] * step;
                        if (r < 0 || r >= 8 || c < 0 || c >= 8) break;

                        char targetPiece = board.GetPiece(r, c);
                        if (targetPiece != '.' && !char.IsWhiteSpace(targetPiece))
                        {
                            if (firstPieceRow == -1)
                            {
                                firstPieceRow = r;
                                firstPieceCol = c;
                                firstPiece = targetPiece;
                            }
                            else
                            {
                                secondPieceRow = r;
                                secondPieceCol = c;
                                secondPiece = targetPiece;
                                break;
                            }
                        }
                    }

                    // CRITICAL: X-ray attack is very specific
                    // Pattern 1: Our piece → Enemy piece → Enemy piece (x-ray attack)
                    // Pattern 2: Our piece → Our piece → Enemy piece (x-ray defense)
                    // The key is that the FIRST piece could move, revealing the attack/defense
                    if (secondPieceRow != -1 && firstPieceRow != -1)
                    {
                        bool firstIsWhite = char.IsUpper(firstPiece);
                        bool secondIsWhite = char.IsUpper(secondPiece);

                        // Pattern 1: X-ray ATTACK through enemy piece
                        // Both pieces are enemy, and if first piece moves, second is attacked
                        if (firstIsWhite != isWhite && secondIsWhite != isWhite)
                        {
                            PieceType firstType = PieceHelper.GetPieceType(firstPiece);
                            PieceType secondType = PieceHelper.GetPieceType(secondPiece);

                            int firstValue = ChessUtilities.GetPieceValue(firstType);
                            int secondValue = ChessUtilities.GetPieceValue(secondType);

                            // X-ray attack only meaningful if:
                            // 1. Second piece is the KING (absolute - illegal to leave in check)
                            // 2. OR we would WIN the second piece if first piece moved
                            if (secondType == PieceType.King)
                            {
                                // Check if first piece is actually attacked (has reason to move)
                                bool firstPieceAttacked = ChessUtilities.IsSquareDefended(board, firstPieceRow, firstPieceCol, isWhite);

                                if (firstPieceAttacked && firstValue <= 100) // Always true for king behind
                                {
                                    return "x-ray attack on king";
                                }
                            }
                            else if (secondValue >= 5)
                            {
                                // Check if first piece is actually attacked (has reason to move)
                                bool firstPieceAttacked = ChessUtilities.IsSquareDefended(board, firstPieceRow, firstPieceCol, isWhite);

                                if (firstPieceAttacked && firstValue <= secondValue)
                                {
                                    // Create a temp board to see if we'd win the second piece
                                    ChessBoard tempBoard = new ChessBoard(board.GetArray());
                                    tempBoard.SetPiece(firstPieceRow, firstPieceCol, '.'); // Remove blocking piece

                                    // Check if the second piece would be defended after blocking piece moves
                                    bool secondPieceDefended = ChessUtilities.IsSquareDefended(tempBoard, secondPieceRow, secondPieceCol, !isWhite);

                                    // Only report x-ray if we'd actually win the piece
                                    // Either: piece is undefended, OR we trade up (our value < their value)
                                    if (!secondPieceDefended)
                                    {
                                        return $"x-ray attack on {ChessUtilities.GetPieceName(secondType)}";
                                    }
                                    else if (secondValue > attackerValue)
                                    {
                                        // We'd trade up (e.g., our rook for their queen)
                                        return $"x-ray attack on {ChessUtilities.GetPieceName(secondType)}";
                                    }
                                    // If defended by piece of equal or lesser value, NOT a real x-ray threat
                                }
                            }
                        }

                        // Pattern 2: X-ray DEFENSE through own piece
                        // First piece is ours, second is enemy - we defend through our own piece
                        // This is VERY rare and usually only notable if defending against mate threat
                        // For now, we'll skip this pattern to avoid false positives
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // SMOTHERED MATE - King surrounded by own pieces, knight delivers mate
        private static string? DetectSmotheredMate(ChessBoard board, int pieceRow, int pieceCol, char piece, bool isWhite)
        {
            try
            {
                PieceType pieceType = PieceHelper.GetPieceType(piece);
                if (pieceType != PieceType.Knight) return null;

                // Find enemy king
                char enemyKing = isWhite ? 'k' : 'K';
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        if (board.GetPiece(r, c) == enemyKing)
                        {
                            // Check if knight attacks king
                            if (ChessUtilities.CanAttackSquare(board, pieceRow, pieceCol, piece, r, c))
                            {
                                // Check if king is smothered (all squares blocked by own pieces)
                                var safeSquares = GetKingSafeSquares(board, r, c, !isWhite);

                                if (safeSquares.Count == 0)
                                {
                                    // Check if all surrounding squares occupied by own pieces
                                    int ownPiecesSurrounding = 0;
                                    for (int dr = -1; dr <= 1; dr++)
                                    {
                                        for (int dc = -1; dc <= 1; dc++)
                                        {
                                            if (dr == 0 && dc == 0) continue;
                                            int nr = r + dr, nc = c + dc;
                                            if (nr < 0 || nr >= 8 || nc < 0 || nc >= 8) continue;

                                            char p = board.GetPiece(nr, nc);
                                            if (p != '.' && !char.IsWhiteSpace(p))
                                            {
                                                bool pIsWhite = char.IsUpper(p);
                                                if (pIsWhite == !isWhite) // King's own pieces
                                                    ownPiecesSurrounding++;
                                            }
                                        }
                                    }

                                    if (ownPiecesSurrounding >= 6) // Heavily smothered
                                    {
                                        return "smothered mate";
                                    }
                                }
                            }
                            break;
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // PERPETUAL CHECK - Endless checking sequence (detected from PV lines)
        private static string? DetectPerpetualCheck(List<string> pvLines)
        {
            try
            {
                if (pvLines == null || pvLines.Count == 0) return null;

                // Look at the main PV line
                string mainPv = pvLines[0];
                if (string.IsNullOrEmpty(mainPv)) return null;

                var moves = mainPv.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // Need at least 6 moves to detect repetition pattern
                if (moves.Length < 6) return null;

                // Count how many moves are checks (indicated by '+' in PV)
                int checkCount = moves.Count(m => m.Contains('+'));

                // If majority of moves are checks and we see repetition
                if (checkCount >= moves.Length * 0.6 && moves.Length >= 8)
                {
                    // Check for move repetition (e.g., Qh4+ Kg8 Qg5+ Kh8 Qh4+...)
                    var positionMoves = new Dictionary<string, int>();
                    for (int i = 0; i < Math.Min(moves.Length, 12); i += 2)
                    {
                        if (i + 1 < moves.Length)
                        {
                            string movePattern = moves[i] + moves[i + 1];
                            if (positionMoves.ContainsKey(movePattern))
                            {
                                return "perpetual check";
                            }
                            positionMoves[movePattern] = 1;
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // EN PASSANT TACTICS - REMOVED
        // Requires full move history tracking to detect properly
        // Cannot reliably detect from UCI notation alone (false positives on normal diagonal captures)

        // WEAK BACK RANK - REMOVED
        // Too many false positives (triggers on move 1, opening positions)
        // Needs more sophisticated checking (actual back rank threats, rook placement, etc.)

        // LPDO (Loose Pieces Drop Off) - REMOVED
        // Redundant with existing "Hanging Piece" detection
        // Was causing false positives (e.g., "wins undefended pawn" when pawn is defended)

        // GREEK GIFT SACRIFICE - REMOVED
        // Too many false positives - IsSquareDefended doesn't account for:
        // 1. King can't recapture if moving into check (queen battery)
        // 2. Pinned pieces that can't actually defend
        // 3. Complex tactical situations where "defended" square is actually free
        // Better to rely on general tactical detection and let sacrifice logic handle edge cases

        // EXCHANGE SACRIFICE - Rook for Bishop/Knight
        private static string? DetectExchangeSacrifice(ChessBoard board, int destRow, int destCol, char piece, char targetPiece, string evaluation)
        {
            try
            {
                PieceType pieceType = PieceHelper.GetPieceType(piece);
                if (pieceType != PieceType.Rook) return null;

                if (targetPiece == '.' || char.IsWhiteSpace(targetPiece)) return null;

                PieceType targetType = PieceHelper.GetPieceType(targetPiece);

                // Exchange sacrifice: Rook (5) for Bishop/Knight (3)
                if (targetType != PieceType.Bishop && targetType != PieceType.Knight) return null;

                // CRITICAL FIX: Check if this is actually a sacrifice or just a recapture/trade
                // For it to be a sacrifice, we need to:
                // 1. Target must be defended (otherwise just winning free piece)
                // 2. We must be losing material overall (not a fair trade)
                bool isWhite = char.IsUpper(piece);
                bool targetIsDefended = ChessUtilities.IsSquareDefended(board, destRow, destCol, !isWhite);

                if (!targetIsDefended)
                {
                    // Not a sacrifice - just capturing a free piece
                    return null;
                }

                // Create board after capture to check SEE
                ChessBoard tempBoard = new ChessBoard(board.GetArray());
                tempBoard.SetPiece(destRow, destCol, piece);

                // Use SEE to check if we're actually losing material
                // If SEE is positive or neutral, it's NOT a sacrifice (we're trading or winning)
                int seeValue = MoveEvaluation.StaticExchangeEvaluation(tempBoard, destRow, destCol, piece, isWhite);

                // CRITICAL: If SEE shows we win or trade evenly, this is NOT a sacrifice
                // This catches the recapture case: even though Rook > Knight in value,
                // if the knight just took our piece, we're just recapturing (trade)
                if (seeValue >= -1)
                {
                    // Not sacrificing - either trading or winning
                    // (Allow -1 tolerance for rounding/evaluation differences)
                    return null;
                }

                // This is materially losing (confirmed by SEE < -1)
                // Only report if engine evaluation is still good (positional compensation)
                double? eval = ParseEvaluation(evaluation);

                if (eval.HasValue)
                {
                    // If giving up the exchange but evaluation is still decent, it's an exchange sacrifice
                    if ((isWhite && eval.Value > -0.5) || (!isWhite && eval.Value < 0.5))
                    {
                        return "exchange sacrifice (rook for minor piece)";
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // GENERAL SACRIFICE - Intentional material loss with compensation
        // A sacrifice is when we LOSE material overall, not just when we capture with a more valuable piece
        private static string? DetectSacrifice(ChessBoard board, int destRow, int destCol, char piece, char targetPiece, bool isWhite, string evaluation)
        {
            try
            {
                // Only detect if we're capturing something
                if (targetPiece == '.' || char.IsWhiteSpace(targetPiece)) return null;

                PieceType pieceType = PieceHelper.GetPieceType(piece);
                PieceType targetType = PieceHelper.GetPieceType(targetPiece);

                int pieceValue = ChessUtilities.GetPieceValue(pieceType);
                int targetValue = ChessUtilities.GetPieceValue(targetType);

                // If we're capturing something of equal or greater value, never a sacrifice
                if (pieceValue <= targetValue) return null;

                // CRITICAL: For it to be a sacrifice, the target should be defended
                // If undefended, we're just winning material (not a sacrifice)
                bool targetIsDefended = ChessUtilities.IsSquareDefended(board, destRow, destCol, !isWhite);

                if (!targetIsDefended)
                {
                    // Not a sacrifice - just winning a free piece
                    return null;
                }

                // USE SEE to determine if we actually LOSE material
                // Create temp board after our capture
                ChessBoard tempBoard = new ChessBoard(board.GetArray());
                tempBoard.SetPiece(destRow, destCol, piece);

                int seeValue = MoveEvaluation.StaticExchangeEvaluation(tempBoard, destRow, destCol, piece, isWhite);

                // If SEE is >= 0, we don't lose material, so it's NOT a sacrifice
                // A sacrifice means we intentionally lose material for positional compensation
                if (seeValue >= 0)
                {
                    // We break even or win material - not a sacrifice
                    return null;
                }

                // We DO lose material (SEE < 0). Check if the loss is significant
                int materialLoss = -seeValue; // Convert to positive number

                // Only consider it a sacrifice if we lose at least 2 points of material
                if (materialLoss < 2)
                {
                    return null;
                }

                // ADDITIONAL CHECK: If we're giving check, it's often not a real sacrifice
                bool givingCheck = IsGivingCheck(board, destRow, destCol, piece, isWhite);
                if (givingCheck)
                {
                    // With check, opponent might not be able to recapture safely
                    // Be more conservative - require larger material loss
                    if (materialLoss < 3)
                    {
                        return null;
                    }
                }

                // Parse evaluation to see if we have compensation
                double? eval = ParseEvaluation(evaluation);

                if (eval.HasValue)
                {
                    // If we're sacrificing material but evaluation is still good, it's a sound sacrifice
                    bool hasCompensation = (isWhite && eval.Value > 0.5) || (!isWhite && eval.Value < -0.5);

                    if (hasCompensation)
                    {
                        // Classify by material lost
                        if (materialLoss >= 8)
                        {
                            return "queen sacrifice";
                        }
                        else if (materialLoss >= 4)
                        {
                            return "rook sacrifice";
                        }
                        else if (materialLoss >= 2)
                        {
                            return "piece sacrifice";
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // Helper: Get king's safe squares
        private static List<(int, int)> GetKingSafeSquares(ChessBoard board, int kingRow, int kingCol, bool isWhite)
        {
            var safeSquares = new List<(int, int)>();

            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;

                    int r = kingRow + dr;
                    int c = kingCol + dc;

                    if (r < 0 || r >= 8 || c < 0 || c >= 8) continue;

                    char piece = board.GetPiece(r, c);

                    // Skip occupied squares by own pieces
                    if (piece != '.' && !char.IsWhiteSpace(piece))
                    {
                        bool pieceIsWhite = char.IsUpper(piece);
                        if (pieceIsWhite == isWhite) continue;
                    }

                    // Check if square is attacked by enemy
                    bool isAttacked = false;
                    for (int er = 0; er < 8; er++)
                    {
                        for (int ec = 0; ec < 8; ec++)
                        {
                            char enemyPiece = board.GetPiece(er, ec);
                            if (enemyPiece == '.' || char.IsWhiteSpace(enemyPiece)) continue;

                            bool enemyIsWhite = char.IsUpper(enemyPiece);
                            if (enemyIsWhite == isWhite) continue; // Not an enemy

                            if (ChessUtilities.CanAttackSquare(board, er, ec, enemyPiece, r, c))
                            {
                                isAttacked = true;
                                break;
                            }
                        }
                        if (isAttacked) break;
                    }

                    if (!isAttacked)
                    {
                        safeSquares.Add((r, c));
                    }
                }
            }

            return safeSquares;
        }

        // Helper: Count safe squares where a piece can move to escape attack
        private static int CountSafeSquaresForPiece(ChessBoard board, int pieceRow, int pieceCol, char piece, bool isWhite)
        {
            try
            {
                PieceType pieceType = PieceHelper.GetPieceType(piece);
                int safeCount = 0;

                // Check all possible moves for this piece
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        if (r == pieceRow && c == pieceCol) continue;

                        // Check if piece can move to this square
                        if (ChessUtilities.CanAttackSquare(board, pieceRow, pieceCol, piece, r, c))
                        {
                            char destPiece = board.GetPiece(r, c);

                            // Can't move to square with friendly piece
                            if (destPiece != '.' && char.IsUpper(destPiece) == isWhite)
                                continue;

                            // Check if this square would be safe (not attacked by enemy)
                            bool isSafe = !ChessUtilities.IsSquareDefended(board, r, c, !isWhite);

                            if (isSafe)
                            {
                                safeCount++;
                            }
                        }
                    }
                }

                return safeCount;
            }
            catch
            {
                return 0;
            }
        }

        // Helper: Count how many pawns form a shield in front of the king
        private static int CountPawnShield(ChessBoard board, int kingRow, int kingCol, bool isWhite)
        {
            try
            {
                char friendlyPawn = isWhite ? 'P' : 'p';
                int pawnShieldRow = isWhite ? kingRow + 1 : kingRow - 1;

                if (pawnShieldRow < 0 || pawnShieldRow >= 8) return 0;

                int pawnShield = 0;

                // Check three files in front of king
                for (int fileOffset = -1; fileOffset <= 1; fileOffset++)
                {
                    int checkFile = kingCol + fileOffset;
                    if (checkFile < 0 || checkFile >= 8) continue;

                    if (board.GetPiece(pawnShieldRow, checkFile) == friendlyPawn)
                        pawnShield++;
                }

                return pawnShield;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Detects if our capture/move creates pawn structure weaknesses for the OPPONENT.
        /// This is a positive - we're damaging their pawn structure!
        /// Checks for: isolated pawns, doubled pawns left behind after we capture
        /// </summary>
        private static string? DetectOpponentPawnWeakness(ChessBoard board, int captureRow, int captureCol, bool opponentIsWhite)
        {
            try
            {
                char opponentPawn = opponentIsWhite ? 'P' : 'p';

                // Check adjacent files for opponent pawns that might now be isolated or doubled
                for (int fileOffset = -1; fileOffset <= 1; fileOffset++)
                {
                    int checkFile = captureCol + fileOffset;
                    if (checkFile < 0 || checkFile >= 8) continue;

                    // Look for opponent pawns on this file
                    for (int row = 0; row < 8; row++)
                    {
                        if (board.GetPiece(row, checkFile) == opponentPawn)
                        {
                            // Found an opponent pawn - check if it's now isolated
                            bool hasLeftSupport = false;
                            bool hasRightSupport = false;

                            // Check left file for supporting pawns
                            if (checkFile > 0)
                            {
                                for (int r = 0; r < 8; r++)
                                {
                                    if (board.GetPiece(r, checkFile - 1) == opponentPawn)
                                    {
                                        hasLeftSupport = true;
                                        break;
                                    }
                                }
                            }

                            // Check right file for supporting pawns
                            if (checkFile < 7)
                            {
                                for (int r = 0; r < 8; r++)
                                {
                                    if (board.GetPiece(r, checkFile + 1) == opponentPawn)
                                    {
                                        hasRightSupport = true;
                                        break;
                                    }
                                }
                            }

                            // If no support on either side, it's an isolated pawn
                            if (!hasLeftSupport && !hasRightSupport)
                            {
                                char file = (char)('a' + checkFile);
                                return $"creates isolated {file}-pawn for opponent";
                            }
                        }
                    }

                    // Check for doubled pawns on this file
                    int pawnCount = 0;
                    for (int row = 0; row < 8; row++)
                    {
                        if (board.GetPiece(row, checkFile) == opponentPawn)
                            pawnCount++;
                    }

                    if (pawnCount >= 2)
                    {
                        char file = (char)('a' + checkFile);
                        return $"creates doubled {file}-pawns for opponent";
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // Helper: Count how many pieces of a given color defend a square
        private static int CountDefenders(ChessBoard board, int row, int col, bool byWhite)
        {
            try
            {
                int defenderCount = 0;

                // Check all squares on the board
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char piece = board.GetPiece(r, c);
                        if (piece == '.') continue;

                        // Check if this piece is the right color
                        bool pieceIsWhite = char.IsUpper(piece);
                        if (pieceIsWhite != byWhite) continue;

                        // Check if this piece can attack the target square
                        if (ChessUtilities.CanAttackSquare(board, r, c, piece, row, col))
                        {
                            defenderCount++;
                        }
                    }
                }

                return defenderCount;
            }
            catch
            {
                return 0;
            }
        }
    }
}