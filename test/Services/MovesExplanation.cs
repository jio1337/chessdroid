using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ChessDroid.Models;

namespace ChessDroid.Services
{
    public class MovesExplanation
    {
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
        private static int GetPieceValue(PieceType pieceType)
        {
            return pieceType switch
            {
                PieceType.Pawn => 1,
                PieceType.Knight => 3,
                PieceType.Bishop => 3,
                PieceType.Rook => 5,
                PieceType.Queen => 9,
                PieceType.King => 100, // King is invaluable
                _ => 0
            };
        }

        private static string GetPieceName(PieceType pieceType)
        {
            return pieceType switch
            {
                PieceType.Pawn => "pawn",
                PieceType.Knight => "knight",
                PieceType.Bishop => "bishop",
                PieceType.Rook => "rook",
                PieceType.Queen => "queen",
                PieceType.King => "king",
                _ => "piece"
            };
        }

        // Generate a brief explanation for why the move is best
        public static string GenerateMoveExplanation(string bestMove, string fen, List<string> pvs, string evaluation)
        {
            try
            {
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

                // Check for tactical patterns FIRST (highest priority)
                // Pass both original board and temp board for discovered attack detection
                string? tacticalPattern = DetectTacticalPattern(board, tempBoard, srcRank, srcFile, destRank, destFile, piece, pieceType, isWhite);
                if (!string.IsNullOrEmpty(tacticalPattern))
                {
                    reasons.Add(tacticalPattern);
                }

                // PERPETUAL CHECK: Detect from PV lines
                string? perpetualCheckInfo = DetectPerpetualCheck(pvs);
                if (!string.IsNullOrEmpty(perpetualCheckInfo))
                {
                    reasons.Add(perpetualCheckInfo);
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

                // Check if it's a capture
                if (targetPiece != '.')
                {
                    string capturedPiece = GetPieceName(PieceHelper.GetPieceType(targetPiece));
                    reasons.Add($"captures {capturedPiece}");
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

                // Positional considerations
                if (pieceType == PieceType.Knight || pieceType == PieceType.Bishop)
                {
                    // Check if moving to center
                    if (destFile >= 2 && destFile <= 5 && destRank >= 2 && destRank <= 5)
                    {
                        reasons.Add("centralizes piece");
                    }
                }

                // Development moves
                if ((srcRank == 0 || srcRank == 7) && (pieceType == PieceType.Knight || pieceType == PieceType.Bishop))
                {
                    reasons.Add("develops piece");
                }

                // Castling
                if (pieceType == PieceType.King && Math.Abs(destFile - srcFile) == 2)
                {
                    reasons.Add(destFile > srcFile ? "castles kingside for safety" : "castles queenside");
                }

                // Check evaluation to add context (only if no tactical reason found)
                if (reasons.Count == 0)
                {
                    double? eval = ParseEvaluation(evaluation);
                    if (eval.HasValue)
                    {
                        if (Math.Abs(eval.Value) > 3.0)
                        {
                            reasons.Add(eval.Value > 0 ? "maintains winning advantage" : "fights back in difficult position");
                        }
                        else if (Math.Abs(eval.Value) < 0.3)
                        {
                            reasons.Add("maintains balance");
                        }
                    }
                }

                if (reasons.Count == 0)
                    return "improves position";

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
                        if (CanAttackSquare(board, pieceRow, pieceCol, piece, r, c))
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
                            int otherValue = GetPieceValue(other.type);
                            int forkingPieceValue = GetPieceValue(pieceType); // Knight = 3

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
                                bool isDefended = IsSquareDefended(board, other.row, other.col, !isWhite);

                                if (!isDefended)
                                {
                                    return $"forks king and {GetPieceName(other.type)}";
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
                                        return $"forks king and {GetPieceName(other.type)}";
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
                        .Where(p => GetPieceValue(p.type) >= 3) // Knights, bishops, rooks, queens, king
                        .OrderByDescending(p => GetPieceValue(p.type))
                        .Take(2)
                        .ToList();

                    if (valuableTargets.Count >= 2)
                    {
                        // Check if at least one of the forked pieces can be profitably captured
                        int forkingPieceValue = GetPieceValue(pieceType);
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
                            bool targetCanRecapture = CanAttackSquare(board, target.row, target.col,
                                board.GetPiece(target.row, target.col), pieceRow, pieceCol);

                            // If target can recapture our forking piece, we need the trade to be profitable
                            if (targetCanRecapture)
                            {
                                // Only valid if target is worth more than our forking piece (profitable trade)
                                if (GetPieceValue(target.type) > forkingPieceValue)
                                {
                                    // But also check: is there another forked piece that can't recapture?
                                    // If so, we can capture that one instead
                                    var otherTargets = valuableTargets.Where(t => t.row != target.row || t.col != target.col);
                                    foreach (var other in otherTargets)
                                    {
                                        bool otherCanRecapture = CanAttackSquare(board, other.row, other.col,
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
                                bool isDefended = IsSquareDefended(board, target.row, target.col, !isWhite);

                                // Can win material if:
                                // 1. Target is undefended (free capture), OR
                                // 2. Target is defended but worth more than the forking piece (profitable trade)
                                if (!isDefended || GetPieceValue(target.type) > forkingPieceValue)
                                {
                                    canWinMaterial = true;
                                    break;
                                }
                            }

                            if (canWinMaterial) break;
                        }

                        if (canWinMaterial)
                        {
                            string pieces = string.Join(" and ", valuableTargets.Select(p => GetPieceName(p.type)));
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
        private static bool CanAttackSquare(ChessBoard board, int fromRow, int fromCol, char piece, int toRow, int toCol)
        {
            PieceType pieceType = PieceHelper.GetPieceType(piece);
            int dR = toRow - fromRow;
            int dF = toCol - fromCol;

            switch (pieceType)
            {
                case PieceType.Knight:
                    return (Math.Abs(dR) == 2 && Math.Abs(dF) == 1) || (Math.Abs(dR) == 1 && Math.Abs(dF) == 2);

                case PieceType.King:
                    return Math.Abs(dR) <= 1 && Math.Abs(dF) <= 1 && (dR != 0 || dF != 0);

                case PieceType.Pawn:
                    // Pawns attack diagonally
                    bool isWhite = char.IsUpper(piece);
                    int direction = isWhite ? -1 : 1;
                    return dR == direction && Math.Abs(dF) == 1;

                case PieceType.Bishop:
                    if (Math.Abs(dR) != Math.Abs(dF)) return false;
                    return IsPathClear(board, fromRow, fromCol, toRow, toCol);

                case PieceType.Rook:
                    if (dR != 0 && dF != 0) return false;
                    return IsPathClear(board, fromRow, fromCol, toRow, toCol);

                case PieceType.Queen:
                    if (dR != 0 && dF != 0 && Math.Abs(dR) != Math.Abs(dF)) return false;
                    return IsPathClear(board, fromRow, fromCol, toRow, toCol);
            }

            return false;
        }

        // Check if path is clear between two squares (for sliding pieces)
        private static bool IsPathClear(ChessBoard board, int fromRow, int fromCol, int toRow, int toCol)
        {
            try
            {
                int dR = Math.Sign(toRow - fromRow);
                int dF = Math.Sign(toCol - fromCol);

                int r = fromRow + dR;
                int c = fromCol + dF;

                while (r != toRow || c != toCol)
                {
                    // Check bounds
                    if (r < 0 || r >= 8 || c < 0 || c >= 8)
                        return false;

                    if (board.GetPiece(r, c) != '.') return false;
                    r += dR;
                    c += dF;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        // Detect if this piece is pinning an enemy piece
        // PIN: Attacking a less valuable piece that shields a more valuable piece/King
        private static string? DetectPin(ChessBoard board, int pieceRow, int pieceCol, char piece, bool isWhite)
        {
            try
            {
                PieceType pieceType = PieceHelper.GetPieceType(piece);

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

                                    int pinnedValue = GetPieceValue(pinnedPieceType);
                                    int behindValue = GetPieceValue(behindPieceType);

                                    // PIN: The piece behind must be more valuable than the pinned piece
                                    if (behindPieceType == PieceType.King)
                                    {
                                        // Absolute pin: can't legally move (exposes King)
                                        return $"pins {GetPieceName(pinnedPieceType)} to king (absolute)";
                                    }
                                    else if (behindValue > pinnedValue)
                                    {
                                        // Relative pin: can move but loses material
                                        return $"pins {GetPieceName(pinnedPieceType)} to {GetPieceName(behindPieceType)}";
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
        // SKEWER: Attacking a more valuable piece that shields a less valuable piece
        private static string? DetectSkewer(ChessBoard board, int pieceRow, int pieceCol, char piece, bool isWhite)
        {
            // Skewer: Like a pin, but the high-value piece is in front, low-value behind.
            // Only sliding pieces (Queen, Rook, Bishop) can skewer.
            try
            {
                PieceType pieceType = PieceHelper.GetPieceType(piece);
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
                                    int frontValue = GetPieceValue(frontPieceType);
                                    int behindValue = GetPieceValue(behindPieceType);

                                    // SKEWER: The front piece must be more valuable than the one behind
                                    // (forcing it to move and exposing the less valuable piece)
                                    // Only report a skewer if the behind piece is not defended
                                    bool behindDefended = IsSquareDefended(board, r, c, !isWhite);
                                    if (!behindDefended)
                                    {
                                        if (frontPieceType == PieceType.King)
                                            return $"skewers king, winning {GetPieceName(behindPieceType)}";
                                        else if (frontValue > behindValue)
                                            return $"skewers {GetPieceName(frontPieceType)}, winning {GetPieceName(behindPieceType)}";
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
                            return CanAttackSquare(board, pieceRow, pieceCol, piece, r, c);
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
        private static bool IsSquareDefended(ChessBoard board, int row, int col, bool byWhite)
        {
            try
            {
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
                        if (CanAttackSquare(board, r, c, piece, row, col))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false; // If error, assume defended to be safe
            }
        }

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
                                bool canAttackNow = CanAttackSquare(newBoard, r, c, piece, targetR, targetC);

                                // Check if this piece could NOT attack the target on ORIGINAL board (was blocked)
                                bool wasBlockedBefore = !CanAttackSquare(originalBoard, r, c, piece, targetR, targetC);

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

                                                    if (CanAttackSquare(newBoard, destRow, destCol, movedPiece, attR, attC))
                                                    {
                                                        PieceType attackedType = PieceHelper.GetPieceType(attackedPiece);
                                                        if (GetPieceValue(attackedType) >= 5) // Rook or Queen
                                                        {
                                                            return $"discovered check, wins {GetPieceName(attackedType)}";
                                                        }
                                                    }
                                                }
                                            }
                                            return "discovered check";
                                        }
                                        else if (GetPieceValue(targetType) >= 5) // Rook or Queen
                                            return $"discovered attack on {GetPieceName(targetType)}";
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

                        if (CanAttackSquare(board, r, c, piece, kingR, kingC))
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
                        if (CanAttackSquare(board, pieceRow, pieceCol, targetPiece, r, c))
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

                                    if (CanAttackSquare(board, dr, dc, defender, r, c))
                                        defenderCount++;
                                }
                            }

                            if (defenderCount == 0) // Would be undefended
                            {
                                PieceType defendedType = PieceHelper.GetPieceType(defendedPiece);
                                if (GetPieceValue(defendedType) >= 3) // Valuable piece
                                {
                                    return $"removes defender of {GetPieceName(defendedType)}";
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
        private static string? DetectTrappedPiece(ChessBoard board, int pieceRow, int pieceCol, bool isWhite)
        {
            try
            {
                // Look for enemy pieces near our piece that might be trapped
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char targetPiece = board.GetPiece(r, c);
                        if (targetPiece == '.') continue;
                        if (char.IsUpper(targetPiece) == isWhite) continue; // Must be enemy

                        PieceType targetType = PieceHelper.GetPieceType(targetPiece);

                        // Check high-value pieces (not pawns)
                        if (GetPieceValue(targetType) < 3) continue;

                        // Count safe AND profitable escape squares for this piece
                        int safeSquares = 0;
                        int targetPieceValue = GetPieceValue(targetType);

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
                                if (CanAttackSquare(board, r, c, targetPiece, newR, newC))
                                {
                                    char destPiece = board.GetPiece(newR, newC);

                                    // Can't move to square with friendly piece (same color as trapped piece)
                                    if (destPiece != '.' && char.IsUpper(destPiece) == char.IsUpper(targetPiece))
                                        continue;

                                    // Check if square would be safe (not attacked by our pieces)
                                    bool isSquareSafe = !IsSquareDefended(board, newR, newC, isWhite);

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
                                            int capturedValue = GetPieceValue(PieceHelper.GetPieceType(destPiece));
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

                        // Piece is trapped if it has no safe OR profitable escape squares
                        if (safeSquares == 0 && targetType != PieceType.Pawn)
                        {
                            return $"traps {GetPieceName(targetType)}";
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
                int attackerValue = GetPieceValue(attackerType);

                foreach (var target in attackedPieces)
                {
                    int targetValue = GetPieceValue(target.type);

                    // Only report if we're winning material (target worth more or equal and undefended)
                    if (targetValue >= attackerValue || targetValue >= 3)
                    {
                        // Check if target is defended
                        bool isDefended = IsSquareDefended(board, target.row, target.col, !isWhite);

                        if (!isDefended && target.type != PieceType.King)
                        {
                            // CRITICAL: Check if our attacking piece can be recaptured at its NEW position
                            // This prevents false positives like "wins undefended knight" when we can be recaptured
                            bool weCanBeRecaptured = IsSquareDefended(board, attackerRow, attackerCol, !isWhite);

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
                                        return $"wins undefended {GetPieceName(target.type)}";
                                    }
                                }
                                else
                                {
                                    // We can't be recaptured, so it's a free piece
                                    return $"wins undefended {GetPieceName(target.type)}";
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
                        if (CanAttackSquare(board, pieceRow, pieceCol, targetPiece, r, c))
                        {
                            // Check if this defended piece is also under attack by us
                            bool isUnderAttack = IsSquareDefended(board, r, c, isWhite);
                            if (isUnderAttack)
                            {
                                PieceType defendedType = PieceHelper.GetPieceType(defendedPiece);
                                if (GetPieceValue(defendedType) >= 3) // Valuable piece
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
                    var pieces = string.Join(" and ", defendedPieces.Take(2).Select(p => GetPieceName(p.type)));
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
                        !IsSquareDefended(board, escapeRank, checkCol, isWhite))
                    {
                        safeEscapes++;
                    }
                }

                // If king has no/few escape squares and we're attacking the back rank
                if (safeEscapes == 0)
                {
                    // Check if we're giving check or threatening mate
                    if (CanAttackSquare(board, pieceRow, pieceCol, piece, kingRow, kingCol))
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
                                if (CanAttackSquare(board, pieceRow, pieceCol, targetPiece, defRow, defCol))
                                {
                                    // Check if we're attacking that same square
                                    if (IsSquareDefended(board, defRow, defCol, isWhite))
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

                        if (CanAttackSquare(board, pieceRow, pieceCol, piece, r, c))
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
                    bool hasMaterialThreat = attackedPieces.Any(p => GetPieceValue(p.type) >= 3);

                    if (hasCheckThreat && hasMaterialThreat)
                    {
                        return "double attack: check and wins material";
                    }

                    // Two valuable pieces attacked
                    var valuableTargets = attackedPieces.Where(p => GetPieceValue(p.type) >= 3).ToList();
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

        // DECOY - Sacrifice to lure a piece to a bad square
        private static string? DetectDecoy(ChessBoard board, int pieceRow, int pieceCol, bool isWhite)
        {
            try
            {
                char piece = board.GetPiece(pieceRow, pieceCol);
                PieceType pieceType = PieceHelper.GetPieceType(piece);
                int pieceValue = GetPieceValue(pieceType);

                // CRITICAL: For it to be a decoy sacrifice, our piece must be:
                // 1. Undefended or inadequately defended (can be captured), AND
                // 2. Valuable (at least a minor piece)
                if (pieceValue < 3) return null; // Pawn decoys are not significant enough

                int ourDefenders = CountDefenders(board, pieceRow, pieceCol, isWhite);
                int theirAttackers = CountDefenders(board, pieceRow, pieceCol, !isWhite);

                // If we're adequately defended (2+ defenders), it's not a sacrifice
                if (ourDefenders >= 2 && ourDefenders >= theirAttackers) return null;

                // Look for pieces we're attacking
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char targetPiece = board.GetPiece(r, c);
                        if (targetPiece == '.' || char.IsWhiteSpace(targetPiece)) continue;
                        bool targetIsWhite = char.IsUpper(targetPiece);
                        if (targetIsWhite == isWhite) continue;

                        if (CanAttackSquare(board, pieceRow, pieceCol, piece, r, c))
                        {
                            PieceType targetType = PieceHelper.GetPieceType(targetPiece);

                            // If we're sacrificing valuable piece to attack king
                            if (targetType == PieceType.King && pieceValue >= 3)
                            {
                                // Check if king is forced to bad square
                                bool kingHasLimitedMoves = GetKingSafeSquares(board, r, c, !isWhite).Count <= 2;
                                if (kingHasLimitedMoves)
                                {
                                    return "decoy sacrifice";
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

        // X-RAY ATTACK - Attack through another piece
        private static string? DetectXRayAttack(ChessBoard board, int pieceRow, int pieceCol, char piece, bool isWhite)
        {
            try
            {
                PieceType pieceType = PieceHelper.GetPieceType(piece);

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

                    // X-ray exists if we found two pieces and the second is enemy and valuable
                    if (secondPieceRow != -1)
                    {
                        bool secondIsWhite = char.IsUpper(secondPiece);
                        if (secondIsWhite != isWhite)
                        {
                            PieceType secondType = PieceHelper.GetPieceType(secondPiece);
                            if (GetPieceValue(secondType) >= 3)
                            {
                                return "x-ray attack";
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
                            if (CanAttackSquare(board, pieceRow, pieceCol, piece, r, c))
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

                // CRITICAL: For it to be a sacrifice, the target must be defended
                // If the target is undefended, we're just winning a free piece (not a sacrifice)
                bool isWhite = char.IsUpper(piece);
                bool targetIsDefended = IsSquareDefended(board, destRow, destCol, !isWhite);

                if (!targetIsDefended)
                {
                    // Not a sacrifice - just capturing a free piece
                    return null;
                }

                // This is materially losing (5 for 3 = -2)
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
        private static string? DetectSacrifice(ChessBoard board, int destRow, int destCol, char piece, char targetPiece, bool isWhite, string evaluation)
        {
            try
            {
                // Only detect if we're capturing something
                if (targetPiece == '.' || char.IsWhiteSpace(targetPiece)) return null;

                PieceType pieceType = PieceHelper.GetPieceType(piece);
                PieceType targetType = PieceHelper.GetPieceType(targetPiece);

                int pieceValue = GetPieceValue(pieceType);
                int targetValue = GetPieceValue(targetType);

                // Sacrifice = giving up more value than we gain
                if (pieceValue <= targetValue) return null; // Not a sacrifice, equal or winning trade

                // CRITICAL: For it to be a sacrifice, the target should be defended
                // If undefended, we're just winning material (not a sacrifice)
                bool targetIsDefended = IsSquareDefended(board, destRow, destCol, !isWhite);

                if (!targetIsDefended)
                {
                    // Not a sacrifice - just a favorable trade or free piece
                    return null;
                }

                // ADDITIONAL CHECK: Count OUR defenders vs THEIR defenders
                // If we have equal or more defenders, it's a fair trade, not a sacrifice
                int ourDefenders = CountDefenders(board, destRow, destCol, isWhite);
                int theirDefenders = CountDefenders(board, destRow, destCol, !isWhite);

                // If we have 2+ defenders, we're not really sacrificing (adequately protected)
                if (ourDefenders >= 2 && ourDefenders >= theirDefenders)
                {
                    // This is a protected piece, not a sacrifice
                    return null;
                }

                // ADDITIONAL CHECK: If we're giving check, it's often not a real sacrifice
                // Example: Bxh7+ where king can't recapture due to queen battery
                // The piece appears "defended" by the king, but king can't actually take
                bool givingCheck = IsGivingCheck(board, destRow, destCol, piece, isWhite);
                if (givingCheck)
                {
                    // If giving check and we can be recaptured, we need very strong compensation
                    // Otherwise it's likely a tactical shot (free piece with check), not a sacrifice
                    bool weCanBeRecaptured = IsSquareDefended(board, destRow, destCol, !isWhite);

                    if (!weCanBeRecaptured)
                    {
                        // We give check and can't be recaptured - this is a free piece, not a sacrifice!
                        return null;
                    }
                }

                // Material loss
                int materialLoss = pieceValue - targetValue;

                // Parse evaluation to see if we have compensation
                double? eval = ParseEvaluation(evaluation);

                if (eval.HasValue)
                {
                    // If we're sacrificing material but evaluation is still good/improving, it's a sound sacrifice
                    bool hasCompensation = (isWhite && eval.Value > 0.5) || (!isWhite && eval.Value < -0.5);

                    if (hasCompensation)
                    {
                        // Classify by piece sacrificed
                        if (pieceType == PieceType.Queen)
                        {
                            return "queen sacrifice";
                        }
                        else if (pieceType == PieceType.Rook && materialLoss >= 2)
                        {
                            return "rook sacrifice";
                        }
                        else if ((pieceType == PieceType.Bishop || pieceType == PieceType.Knight) && materialLoss >= 2)
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

                            if (CanAttackSquare(board, er, ec, enemyPiece, r, c))
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
                        if (CanAttackSquare(board, pieceRow, pieceCol, piece, r, c))
                        {
                            char destPiece = board.GetPiece(r, c);

                            // Can't move to square with friendly piece
                            if (destPiece != '.' && char.IsUpper(destPiece) == isWhite)
                                continue;

                            // Check if this square would be safe (not attacked by enemy)
                            bool isSafe = !IsSquareDefended(board, r, c, !isWhite);

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
                        if (CanAttackSquare(board, r, c, piece, row, col))
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
