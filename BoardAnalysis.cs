using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Go
{
    public static class BoardAnalysis
    {
        /// <summary>
        /// Determine whether a king can be reached by any of the opponents pieces
        /// </summary>
        /// <param name="board">The state of the board to check</param>
        /// <param name="isBlack">Is the king to check black?</param>
        /// <param name="target">Override the position of the king to check</param>
        /// <remarks><paramref name="target"/> should always be given if checking a not-yet-peformed king move, as the king's internally stored position will be incorrect.</remarks>
        public static bool IsKingReachable(Pieces.Piece?[,] board, bool isBlack, Point? target = null)
        {
            target ??= board.OfType<Pieces.King>().Where(x => x.IsBlack == isBlack).First().Position;

            int backwardsY = isBlack ? -1 : 1;
            // King, promoted bishop straights, promoted rook diagonals, gold general (and equivalents), silver general check
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dy != 0 || dx != 0)
                    {
                        Point newPos = new(target.Value.X + dx, target.Value.Y + dy);
                        if (newPos.X >= 0 && newPos.Y >= 0 && newPos.X < board.GetLength(0) && newPos.Y < board.GetLength(1)
                            && board[newPos.X, newPos.Y] is Pieces.Piece piece && board[newPos.X, newPos.Y]!.IsBlack != isBlack)
                        {
                            if (piece is Pieces.King or Pieces.PromotedBishop or Pieces.PromotedRook)
                            {
                                return true;
                            }
                            if (piece is Pieces.GoldGeneral or Pieces.PromotedKnight or Pieces.PromotedLance
                                or Pieces.PromotedPawn or Pieces.PromotedSilverGeneral && (dy != backwardsY || dx == 0))
                            {
                                return true;
                            }
                            if (piece is Pieces.SilverGeneral && (dy != backwardsY || dx != 0) && dy != 0)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            // Straight checks (rook & lance)
            for (int dx = target.Value.X + 1; dx < board.GetLength(0); dx++)
            {
                Point newPos = new(dx, target.Value.Y);
                if (board[newPos.X, newPos.Y] is not null)
                {
                    if (board[newPos.X, newPos.Y]!.IsBlack != isBlack &&
                        board[newPos.X, newPos.Y] is Pieces.Rook or Pieces.PromotedRook)
                    {
                        return true;
                    }
                    break;
                }
            }
            for (int dx = target.Value.X - 1; dx >= 0; dx--)
            {
                Point newPos = new(dx, target.Value.Y);
                if (board[newPos.X, newPos.Y] is not null)
                {
                    if (board[newPos.X, newPos.Y]!.IsBlack != isBlack &&
                        board[newPos.X, newPos.Y] is Pieces.Rook or Pieces.PromotedRook)
                    {
                        return true;
                    }
                    break;
                }
            }
            for (int dy = target.Value.Y + 1; dy < board.GetLength(1); dy++)
            {
                Point newPos = new(target.Value.X, dy);
                if (board[newPos.X, newPos.Y] is not null)
                {
                    if (board[newPos.X, newPos.Y]!.IsBlack != isBlack &&
                        (board[newPos.X, newPos.Y] is Pieces.Rook or Pieces.PromotedRook
                            || (board[newPos.X, newPos.Y] is Pieces.Lance lance && !lance.IsBlack)))
                    {
                        return true;
                    }
                    break;
                }
            }
            for (int dy = target.Value.Y - 1; dy >= 0; dy--)
            {
                Point newPos = new(target.Value.X, dy);
                if (board[newPos.X, newPos.Y] is not null)
                {
                    if (board[newPos.X, newPos.Y]!.IsBlack != isBlack &&
                        (board[newPos.X, newPos.Y] is Pieces.Rook or Pieces.PromotedRook
                            || (board[newPos.X, newPos.Y] is Pieces.Lance lance && lance.IsBlack)))
                    {
                        return true;
                    }
                    break;
                }
            }

            // Diagonal checks (bishop)
            for (int dif = 1; target.Value.X + dif < board.GetLength(0) && target.Value.Y + dif < board.GetLength(1); dif++)
            {
                Point newPos = new(target.Value.X + dif, target.Value.Y + dif);
                if (board[newPos.X, newPos.Y] is not null)
                {
                    if (board[newPos.X, newPos.Y]!.IsBlack != isBlack &&
                        board[newPos.X, newPos.Y] is Pieces.Bishop or Pieces.PromotedBishop)
                    {
                        return true;
                    }
                    break;
                }
            }
            for (int dif = 1; target.Value.X - dif >= 0 && target.Value.Y + dif < board.GetLength(1); dif++)
            {
                Point newPos = new(target.Value.X - dif, target.Value.Y + dif);
                if (board[newPos.X, newPos.Y] is not null)
                {
                    if (board[newPos.X, newPos.Y]!.IsBlack != isBlack &&
                        board[newPos.X, newPos.Y] is Pieces.Bishop or Pieces.PromotedBishop)
                    {
                        return true;
                    }
                    break;
                }
            }
            for (int dif = 1; target.Value.X - dif >= 0 && target.Value.Y - dif >= 0; dif++)
            {
                Point newPos = new(target.Value.X - dif, target.Value.Y - dif);
                if (board[newPos.X, newPos.Y] is not null)
                {
                    if (board[newPos.X, newPos.Y]!.IsBlack != isBlack &&
                        board[newPos.X, newPos.Y] is Pieces.Bishop or Pieces.PromotedBishop)
                    {
                        return true;
                    }
                    break;
                }
            }
            for (int dif = 1; target.Value.X + dif < board.GetLength(0) && target.Value.Y - dif >= 0; dif++)
            {
                Point newPos = new(target.Value.X + dif, target.Value.Y - dif);
                if (board[newPos.X, newPos.Y] is not null)
                {
                    if (board[newPos.X, newPos.Y]!.IsBlack != isBlack &&
                        board[newPos.X, newPos.Y] is Pieces.Bishop or Pieces.PromotedBishop)
                    {
                        return true;
                    }
                    break;
                }
            }

            // Knight checks
            int knightDY = isBlack ? 2 : -2;
            Point knightPos = new(target.Value.X + 1, target.Value.Y + knightDY);
            if (knightPos.X >= 0 && knightPos.Y >= 0 && knightPos.X < board.GetLength(0) && knightPos.Y < board.GetLength(1)
                && board[knightPos.X, knightPos.Y] is Pieces.Knight && board[knightPos.X, knightPos.Y]!.IsBlack != isBlack)
            {
                return true;
            }
            knightPos = new(target.Value.X - 1, target.Value.Y + knightDY);
            if (knightPos.X >= 0 && knightPos.Y >= 0 && knightPos.X < board.GetLength(0) && knightPos.Y < board.GetLength(1)
                && board[knightPos.X, knightPos.Y] is Pieces.Knight && board[knightPos.X, knightPos.Y]!.IsBlack != isBlack)
            {
                return true;
            }

            // Pawn checks
            int pawnYDiff = isBlack ? 1 : -1;
            int newY = target.Value.Y + pawnYDiff;
            if (newY < board.GetLength(1) && newY >= 0)
            {
                if (board[target.Value.X, newY] is Pieces.Pawn && board[target.Value.X, newY]!.IsBlack != isBlack)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determine the current state of the game with the given board.
        /// </summary>
        /// <remarks>
        /// This method will not detect states that depend on game history, such as repetition
        /// </remarks>
        public static GameState DetermineGameState(Pieces.Piece?[,] board, bool currentTurnBlack,
            Point? blackKingPos = null, Point? whiteKingPos = null)
        {
            IEnumerable<Pieces.Piece> blackPieces = board.OfType<Pieces.Piece>().Where(p => p.IsBlack);
            IEnumerable<Pieces.Piece> whitePieces = board.OfType<Pieces.Piece>().Where(p => !p.IsBlack);

            bool blackCheck = IsKingReachable(board, true, blackKingPos ?? null);
            // Black and White cannot both be in check
            bool whiteCheck = !blackCheck && IsKingReachable(board, false, whiteKingPos ?? null);

            if (currentTurnBlack && !blackPieces.SelectMany(p => p.GetValidMoves(board, true)).Any())
            {
                return blackCheck ? GameState.CheckMateBlack : GameState.StalemateBlack;
            }
            if (!currentTurnBlack && !whitePieces.SelectMany(p => p.GetValidMoves(board, true)).Any())
            {
                return whiteCheck ? GameState.CheckMateWhite : GameState.StalemateWhite;
            }

            return blackCheck ? GameState.CheckBlack : whiteCheck ? GameState.CheckWhite : GameState.StandardPlay;
        }

        /// <summary>
        /// Calculate the value of the given game based on the pieces on the board and in hand
        /// </summary>
        /// <returns>
        /// A <see cref="double"/> representing the total piece value of the game.
        /// Positive means black has stronger material, negative means white does.
        /// </returns>
        public static double CalculateGameValue(GoGame game)
        {
            double inHandTotal = 0;
            foreach ((Type dropType, int count) in game.BlackPieceDrops)
            {
                inHandTotal += count * Pieces.Piece.DefaultPieces[dropType].Value;
            }
            foreach ((Type dropType, int count) in game.WhitePieceDrops)
            {
                inHandTotal -= count * Pieces.Piece.DefaultPieces[dropType].Value;
            }
            return inHandTotal + game.Board.OfType<Pieces.Piece>().Sum(p => p.IsBlack ? p.Value : -p.Value);
        }

        public readonly struct PossibleMove
        {
            public Point Source { get; }
            public Point Destination { get; }
            public double EvaluatedFutureValue { get; }
            public bool BlackMateLocated { get; }
            public bool WhiteMateLocated { get; }
            public int DepthToBlackMate { get; }
            public int DepthToWhiteMate { get; }
            public bool DoPromotion { get; }
            public List<(Point, Point, bool)> BestLine { get;  }

            public PossibleMove(Point source, Point destination, double evaluatedFutureValue,
                bool blackMateLocated, bool whiteMateLocated, int depthToBlackMate, int depthToWhiteMate,
                bool doPromotion, List<(Point, Point, bool)> bestLine)
            {
                Source = source;
                Destination = destination;
                EvaluatedFutureValue = evaluatedFutureValue;
                BlackMateLocated = blackMateLocated;
                WhiteMateLocated = whiteMateLocated;
                DepthToBlackMate = depthToBlackMate;
                DepthToWhiteMate = depthToWhiteMate;
                DoPromotion = doPromotion;
                BestLine = bestLine;
            }
        }

        /// <summary>
        /// Use <see cref="EvaluatePossibleMoves"/> to find the best possible move in the current state of the game
        /// </summary>
        /// <param name="maxDepth">The maximum number of half-moves in the future to search</param>
        public static async Task<PossibleMove> EstimateBestPossibleMove(GoGame game, int maxDepth, CancellationToken cancellationToken)
        {
            PossibleMove[] moves = await EvaluatePossibleMoves(game, maxDepth, cancellationToken);
            PossibleMove bestMove = new(default, default,
                game.CurrentTurnBlack ? double.NegativeInfinity : double.PositiveInfinity, false, false, 0, 0, false, new());
            foreach (PossibleMove potentialMove in moves)
            {
                if (game.CurrentTurnBlack)
                {
                    if (bestMove.EvaluatedFutureValue == double.NegativeInfinity
                        || (!bestMove.WhiteMateLocated && potentialMove.WhiteMateLocated)
                        || (!bestMove.WhiteMateLocated && potentialMove.EvaluatedFutureValue > bestMove.EvaluatedFutureValue)
                        || (bestMove.WhiteMateLocated && potentialMove.WhiteMateLocated
                            && potentialMove.DepthToWhiteMate < bestMove.DepthToWhiteMate))
                    {
                        bestMove = potentialMove;
                    }
                }
                else
                {
                    if (bestMove.EvaluatedFutureValue == double.PositiveInfinity
                        || (!bestMove.BlackMateLocated && potentialMove.BlackMateLocated)
                        || (!bestMove.BlackMateLocated && potentialMove.EvaluatedFutureValue < bestMove.EvaluatedFutureValue)
                        || (bestMove.BlackMateLocated && potentialMove.BlackMateLocated
                            && potentialMove.DepthToBlackMate < bestMove.DepthToBlackMate))
                    {
                        bestMove = potentialMove;
                    }
                }
            }
            if (cancellationToken.IsCancellationRequested)
            {
                return default;
            }
            return bestMove;
        }

        /// <summary>
        /// Evaluate each possible move in the current state of the game
        /// </summary>
        /// <param name="maxDepth">The maximum number of half-moves in the future to search</param>
        /// <returns>An array of all possible moves, with information on board value and ability to checkmate</returns>
        public static async Task<PossibleMove[]> EvaluatePossibleMoves(GoGame game, int maxDepth, CancellationToken cancellationToken)
        {
            ConcurrentBag<PossibleMove> possibleMoves = new();
            int remainingThreads = 0;

            foreach (Pieces.Piece? piece in game.Board)
            {
                if (piece is not null)
                {
                    if (piece.IsBlack != game.CurrentTurnBlack)
                    {
                        continue;
                    }

                    foreach (Point validMove in GetValidMovesForEval(game, piece))
                    {
                        if (Pieces.Piece.PromotionMap.ContainsKey(piece.GetType())
                            && ((piece.IsBlack ? validMove.Y >= game.PromotionZoneBlackStart : validMove.Y <= game.PromotionZoneWhiteStart)
                                || (piece.IsBlack ? piece.Position.Y >= game.PromotionZoneBlackStart : piece.Position.Y <= game.PromotionZoneWhiteStart)))
                        {
                            remainingThreads++;
                            Point promotionPosition = piece.Position;
                            Point promotionMove = validMove;
                            GoGame promotionGameClone = game.Clone();
                            List<(Point, Point, bool)> promotionLine = new() { (piece.Position, validMove, true) };
                            _ = promotionGameClone.MovePiece(piece.Position, validMove, true,
                                doPromotion: true, updateMoveText: false);

                            Thread promotionThread = new(() =>
                            {
                                PossibleMove bestSubMove = MinimaxMove(promotionGameClone,
                                    double.NegativeInfinity, double.PositiveInfinity, 1, maxDepth, promotionLine, cancellationToken);
                                // Don't include default value in results
                                if (bestSubMove.Source != bestSubMove.Destination)
                                {
                                    possibleMoves.Add(new PossibleMove(promotionPosition, promotionMove, bestSubMove.EvaluatedFutureValue,
                                        bestSubMove.BlackMateLocated, bestSubMove.WhiteMateLocated,
                                        bestSubMove.DepthToBlackMate, bestSubMove.DepthToWhiteMate, true, bestSubMove.BestLine));
                                }
                                remainingThreads--;
                            });
                            promotionThread.Start();
                        }
                        if ((piece is not Pieces.Pawn and not Pieces.Lance || validMove.Y != (piece.IsBlack ? game.Board.GetLength(1) - 1 : 0))
                            && (piece is not Pieces.Knight || !(piece.IsBlack ? validMove.Y >= game.Board.GetLength(1) - 2 : validMove.Y <= 1)))
                        {
                            remainingThreads++;
                            Point thisPosition = piece.Position;
                            Point thisValidMove = validMove;
                            GoGame gameClone = game.Clone();
                            List<(Point, Point, bool)> thisLine = new() { (piece.Position, validMove, false) };
                            _ = gameClone.MovePiece(piece.Position, validMove, true,
                                doPromotion: false, updateMoveText: false);

                            Thread processThread = new(() =>
                            {
                                PossibleMove bestSubMove = MinimaxMove(gameClone,
                                    double.NegativeInfinity, double.PositiveInfinity, 1, maxDepth, thisLine, cancellationToken);
                                // Don't include default value in results
                                if (bestSubMove.Source != bestSubMove.Destination)
                                {
                                    possibleMoves.Add(new PossibleMove(thisPosition, thisValidMove, bestSubMove.EvaluatedFutureValue,
                                        bestSubMove.BlackMateLocated, bestSubMove.WhiteMateLocated,
                                        bestSubMove.DepthToBlackMate, bestSubMove.DepthToWhiteMate, false, bestSubMove.BestLine));
                                }
                                remainingThreads--;
                            });
                            processThread.Start();
                        }
                    }
                }
            }

            Dictionary<Type, int> dropCounts = game.CurrentTurnBlack ? game.BlackPieceDrops : game.WhitePieceDrops;
            foreach ((Type dropType, int count) in dropCounts)
            {
                if (count > 0)
                {
                    for (int x = 0; x < game.Board.GetLength(0); x++)
                    {
                        for (int y = 0; y < game.Board.GetLength(1); y++)
                        {
                            Point pt = new(x, y);
                            if (game.IsDropPossible(dropType, pt))
                            {
                                remainingThreads++;
                                Point thisDropPoint = pt;
                                Point thisDropSource = GoGame.PieceDropSources[dropType];
                                GoGame gameClone = game.Clone();
                                List<(Point, Point, bool)> thisLine = new() { (thisDropSource, thisDropPoint, false) };
                                _ = gameClone.MovePiece(thisDropSource, thisDropPoint, true,
                                    doPromotion: false, updateMoveText: false);

                                Thread processThread = new(() =>
                                {
                                    PossibleMove bestSubMove = MinimaxMove(gameClone,
                                        double.NegativeInfinity, double.PositiveInfinity, 1, maxDepth, thisLine, cancellationToken);
                                    // Don't include default value in results
                                    if (bestSubMove.Source != bestSubMove.Destination)
                                    {
                                        possibleMoves.Add(new PossibleMove(thisDropSource, pt,
                                            bestSubMove.EvaluatedFutureValue, bestSubMove.BlackMateLocated, bestSubMove.WhiteMateLocated,
                                            bestSubMove.DepthToBlackMate, bestSubMove.DepthToWhiteMate, false, bestSubMove.BestLine));
                                    }
                                    remainingThreads--;
                                });
                                processThread.Start();
                            }
                        }
                    }
                }
            }

            await Task.Run(async () =>
            {
                while (remainingThreads > 0 || cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(50);
                }
            }, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return Array.Empty<PossibleMove>();
            }
            return possibleMoves.ToArray();
        }

        private static HashSet<Point> GetValidMovesForEval(GoGame game, Pieces.Piece piece)
        {
            return piece.GetValidMoves(game.Board, true);
        }

        private static PossibleMove MinimaxMove(GoGame game, double alpha, double beta, int depth, int maxDepth,
            List<(Point, Point, bool)> currentLine, CancellationToken cancellationToken)
        {
            (_, Point lastMoveSrc, Point lastMoveDst, _, _) = game.Moves.Last();
            if (game.GameOver)
            {
                GameState state = game.DetermineGameState();
                if (state is GameState.CheckMateBlack or GameState.PerpetualCheckWhite or GameState.StalemateBlack)
                {
                    return new PossibleMove(lastMoveSrc, lastMoveDst, double.NegativeInfinity, true, false, depth, 0, false,
                        currentLine);
                }
                else if (state is GameState.CheckMateWhite or GameState.PerpetualCheckBlack or GameState.StalemateWhite)
                {
                    return new PossibleMove(lastMoveSrc, lastMoveDst, double.PositiveInfinity, false, true, 0, depth, false,
                        currentLine);
                }
                else
                {
                    // Draw
                    return new PossibleMove(lastMoveSrc, lastMoveDst, 0, false, false, 0, 0, false, currentLine);
                }
            }
            if (depth > maxDepth)
            {
                return new PossibleMove(lastMoveSrc, lastMoveDst, CalculateGameValue(game), false, false, 0, 0, false, currentLine);
            }

            PossibleMove bestMove = new(default, default,
                game.CurrentTurnBlack ? double.NegativeInfinity : double.PositiveInfinity, false, false, 0, 0, false, new());

            foreach (Pieces.Piece? piece in game.Board)
            {
                if (piece is not null)
                {
                    if (piece.IsBlack != game.CurrentTurnBlack)
                    {
                        continue;
                    }

                    foreach (Point validMove in GetValidMovesForEval(game, piece))
                    {
                        List<bool> availablePromotions = new(2);
                        if (Pieces.Piece.PromotionMap.ContainsKey(piece.GetType())
                            && piece.IsBlack ? validMove.Y >= game.PromotionZoneBlackStart : validMove.Y <= game.PromotionZoneWhiteStart)
                        {
                            availablePromotions.Add(true);
                        }
                        if ((piece is not Pieces.Pawn and not Pieces.Lance || (validMove.Y != 0 && validMove.Y != game.Board.GetLength(1) - 1))
                            && (piece is not Pieces.Knight || (validMove.Y < game.Board.GetLength(1) - 2 && validMove.Y > 1)))
                        {
                            availablePromotions.Add(false);
                        }
                        foreach (bool doPromotion in availablePromotions)
                        {
                            GoGame gameClone = game.Clone();
                            List<(Point, Point, bool)> newLine = new(currentLine) { (piece.Position, validMove, doPromotion) };
                            _ = gameClone.MovePiece(piece.Position, validMove, true,
                                doPromotion: doPromotion, updateMoveText: false);
                            PossibleMove potentialMove = MinimaxMove(gameClone, alpha, beta, depth + 1, maxDepth, newLine, cancellationToken);
                            if (cancellationToken.IsCancellationRequested)
                            {
                                return bestMove;
                            }
                            if (game.CurrentTurnBlack)
                            {
                                if (bestMove.EvaluatedFutureValue == double.NegativeInfinity
                                    || (!bestMove.WhiteMateLocated && potentialMove.WhiteMateLocated)
                                    || (!bestMove.WhiteMateLocated && potentialMove.EvaluatedFutureValue > bestMove.EvaluatedFutureValue)
                                    || (bestMove.WhiteMateLocated && potentialMove.WhiteMateLocated
                                        && potentialMove.DepthToWhiteMate < bestMove.DepthToWhiteMate))
                                {
                                    bestMove = new PossibleMove(piece.Position, validMove, potentialMove.EvaluatedFutureValue,
                                        potentialMove.BlackMateLocated, potentialMove.WhiteMateLocated,
                                        potentialMove.DepthToBlackMate, potentialMove.DepthToWhiteMate, potentialMove.DoPromotion, potentialMove.BestLine);
                                }
                                if (potentialMove.EvaluatedFutureValue >= beta && !bestMove.WhiteMateLocated)
                                {
                                    return bestMove;
                                }
                                if (potentialMove.EvaluatedFutureValue > alpha)
                                {
                                    alpha = potentialMove.EvaluatedFutureValue;
                                }
                            }
                            else
                            {
                                if (bestMove.EvaluatedFutureValue == double.PositiveInfinity
                                    || (!bestMove.BlackMateLocated && potentialMove.BlackMateLocated)
                                    || (!bestMove.BlackMateLocated && potentialMove.EvaluatedFutureValue < bestMove.EvaluatedFutureValue)
                                    || (bestMove.BlackMateLocated && potentialMove.BlackMateLocated
                                        && potentialMove.DepthToBlackMate < bestMove.DepthToBlackMate))
                                {
                                    bestMove = new PossibleMove(piece.Position, validMove, potentialMove.EvaluatedFutureValue,
                                        potentialMove.BlackMateLocated, potentialMove.WhiteMateLocated,
                                        potentialMove.DepthToBlackMate, potentialMove.DepthToWhiteMate, potentialMove.DoPromotion, potentialMove.BestLine);
                                }
                                if (potentialMove.EvaluatedFutureValue <= alpha && !bestMove.BlackMateLocated)
                                {
                                    return bestMove;
                                }
                                if (potentialMove.EvaluatedFutureValue < beta)
                                {
                                    beta = potentialMove.EvaluatedFutureValue;
                                }
                            }
                        }
                    }
                }
            }

            Dictionary<Type, int> dropCounts = game.CurrentTurnBlack ? game.BlackPieceDrops : game.WhitePieceDrops;
            foreach ((Type dropType, int count) in dropCounts)
            {
                if (count > 0)
                {
                    for (int x = 0; x < game.Board.GetLength(0); x++)
                    {
                        for (int y = 0; y < game.Board.GetLength(1); y++)
                        {
                            Point pt = new(x, y);
                            if (game.IsDropPossible(dropType, pt))
                            {
                                GoGame gameClone = game.Clone();
                                Point dropPoint = pt;
                                Point dropSource = GoGame.PieceDropSources[dropType];
                                List<(Point, Point, bool)> newLine = new(currentLine) { (dropSource, dropPoint, false) };
                                _ = gameClone.MovePiece(dropSource, dropPoint, true,
                                    doPromotion: true, updateMoveText: false);
                                PossibleMove potentialMove = MinimaxMove(gameClone, alpha, beta, depth + 1, maxDepth, newLine, cancellationToken);
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    return bestMove;
                                }
                                if (game.CurrentTurnBlack)
                                {
                                    if (bestMove.EvaluatedFutureValue == double.NegativeInfinity
                                        || (!bestMove.WhiteMateLocated && potentialMove.WhiteMateLocated)
                                        || (!bestMove.WhiteMateLocated && potentialMove.EvaluatedFutureValue > bestMove.EvaluatedFutureValue)
                                        || (bestMove.WhiteMateLocated && potentialMove.WhiteMateLocated
                                            && potentialMove.DepthToWhiteMate < bestMove.DepthToWhiteMate))
                                    {
                                        bestMove = new PossibleMove(dropSource, dropPoint, potentialMove.EvaluatedFutureValue,
                                            potentialMove.BlackMateLocated, potentialMove.WhiteMateLocated,
                                            potentialMove.DepthToBlackMate, potentialMove.DepthToWhiteMate, potentialMove.DoPromotion, potentialMove.BestLine);
                                    }
                                    if (potentialMove.EvaluatedFutureValue >= beta && !bestMove.WhiteMateLocated)
                                    {
                                        return bestMove;
                                    }
                                    if (potentialMove.EvaluatedFutureValue > alpha)
                                    {
                                        alpha = potentialMove.EvaluatedFutureValue;
                                    }
                                }
                                else
                                {
                                    if (bestMove.EvaluatedFutureValue == double.PositiveInfinity
                                        || (!bestMove.BlackMateLocated && potentialMove.BlackMateLocated)
                                        || (!bestMove.BlackMateLocated && potentialMove.EvaluatedFutureValue < bestMove.EvaluatedFutureValue)
                                        || (bestMove.BlackMateLocated && potentialMove.BlackMateLocated
                                            && potentialMove.DepthToBlackMate < bestMove.DepthToBlackMate))
                                    {
                                        bestMove = new PossibleMove(dropSource, dropPoint, potentialMove.EvaluatedFutureValue,
                                            potentialMove.BlackMateLocated, potentialMove.WhiteMateLocated,
                                            potentialMove.DepthToBlackMate, potentialMove.DepthToWhiteMate, potentialMove.DoPromotion, potentialMove.BestLine);
                                    }
                                    if (potentialMove.EvaluatedFutureValue <= alpha && !bestMove.BlackMateLocated)
                                    {
                                        return bestMove;
                                    }
                                    if (potentialMove.EvaluatedFutureValue < beta)
                                    {
                                        beta = potentialMove.EvaluatedFutureValue;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return bestMove;
        }
    }
}
