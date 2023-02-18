﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shogi
{
    public static class BoardAnalysis
    {
        /// <summary>
        /// Determine whether a king can be reached by any of the opponents pieces
        /// </summary>
        /// <param name="board">The state of the board to check</param>
        /// <param name="isSente">Is the king to check sente?</param>
        /// <param name="target">Override the position of the king to check</param>
        /// <remarks><paramref name="target"/> should always be given if checking a not-yet-peformed king move, as the king's internally stored position will be incorrect.</remarks>
        public static bool IsKingReachable(Pieces.Piece?[,] board, bool isSente, Point? target = null)
        {
            target ??= board.OfType<Pieces.King>().Where(x => x.IsSente == isSente).First().Position;

            // King check
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dy != 0 || dx != 0)
                    {
                        Point newPos = new(target.Value.X + dx, target.Value.Y + dy);
                        if (newPos.X >= 0 && newPos.Y >= 0 && newPos.X < board.GetLength(0) && newPos.Y < board.GetLength(1)
                            && board[newPos.X, newPos.Y] is Pieces.King && board[newPos.X, newPos.Y]!.IsSente != isSente)
                        {
                            return true;
                        }
                    }
                }
            }

            // Straight checks (rook & queen)
            for (int dx = target.Value.X + 1; dx < board.GetLength(0); dx++)
            {
                Point newPos = new(dx, target.Value.Y);
                if (board[newPos.X, newPos.Y] is not null)
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != isSente &&
                        board[newPos.X, newPos.Y] is Pieces.Queen or Pieces.Rook)
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
                    if (board[newPos.X, newPos.Y]!.IsSente != isSente &&
                        board[newPos.X, newPos.Y] is Pieces.Queen or Pieces.Rook)
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
                    if (board[newPos.X, newPos.Y]!.IsSente != isSente &&
                        board[newPos.X, newPos.Y] is Pieces.Queen or Pieces.Rook)
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
                    if (board[newPos.X, newPos.Y]!.IsSente != isSente &&
                        board[newPos.X, newPos.Y] is Pieces.Queen or Pieces.Rook)
                    {
                        return true;
                    }
                    break;
                }
            }

            // Diagonal checks (bishop & queen)
            for (int dif = 1; target.Value.X + dif < board.GetLength(0) && target.Value.Y + dif < board.GetLength(1); dif++)
            {
                Point newPos = new(target.Value.X + dif, target.Value.Y + dif);
                if (board[newPos.X, newPos.Y] is not null)
                {
                    if (board[newPos.X, newPos.Y]!.IsSente != isSente &&
                        board[newPos.X, newPos.Y] is Pieces.Queen or Pieces.Bishop)
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
                    if (board[newPos.X, newPos.Y]!.IsSente != isSente &&
                        board[newPos.X, newPos.Y] is Pieces.Queen or Pieces.Bishop)
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
                    if (board[newPos.X, newPos.Y]!.IsSente != isSente &&
                        board[newPos.X, newPos.Y] is Pieces.Queen or Pieces.Bishop)
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
                    if (board[newPos.X, newPos.Y]!.IsSente != isSente &&
                        board[newPos.X, newPos.Y] is Pieces.Queen or Pieces.Bishop)
                    {
                        return true;
                    }
                    break;
                }
            }

            // Knight checks
            foreach (Point move in Pieces.Knight.Moves)
            {
                Point newPos = new(target.Value.X + move.X, target.Value.Y + move.Y);
                if (newPos.X >= 0 && newPos.Y >= 0 && newPos.X < board.GetLength(0) && newPos.Y < board.GetLength(1)
                    && board[newPos.X, newPos.Y] is Pieces.Knight && board[newPos.X, newPos.Y]!.IsSente != isSente)
                {
                    return true;
                }
            }

            // Pawn checks
            int pawnYDiff = isSente ? 1 : -1;
            int newY = target.Value.Y + pawnYDiff;
            if (newY < board.GetLength(1) && newY > 0)
            {
                if (board[target.Value.X, target.Value.Y] is null && board[target.Value.X, newY] is Pieces.Pawn
                    && board[target.Value.X, newY]!.IsSente != isSente)
                {
                    return true;
                }
                if (board[target.Value.X, target.Value.Y] is not null)
                {
                    if (target.Value.X > 0 && board[target.Value.X - 1, newY] is Pieces.Pawn
                        && board[target.Value.X - 1, newY]!.IsSente != isSente)
                    {
                        return true;
                    }
                    if (target.Value.X < board.GetLength(0) - 1 && board[target.Value.X + 1, newY] is Pieces.Pawn
                        && board[target.Value.X + 1, newY]!.IsSente != isSente)
                    {
                        return true;
                    }
                }
            }
            newY = target.Value.Y + (pawnYDiff * 2);
            if (newY == (isSente ? board.GetLength(1) - 2 : 1))
            {
                if (board[target.Value.X, target.Value.Y] is null && board[target.Value.X, target.Value.Y + pawnYDiff] is null
                    && board[target.Value.X, newY] is Pieces.Pawn && board[target.Value.X, newY]!.IsSente != isSente)
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
        /// This method will not detect states that depend on game history, such as three-fold repetition or the 50-move rule
        /// </remarks>
        public static GameState DetermineGameState(Pieces.Piece?[,] board, bool currentTurnSente,
            Point? senteKingPos = null, Point? goteKingPos = null)
        {
            IEnumerable<Pieces.Piece> sentePieces = board.OfType<Pieces.Piece>().Where(p => p.IsSente);
            IEnumerable<Pieces.Piece> gotePieces = board.OfType<Pieces.Piece>().Where(p => !p.IsSente);

            bool senteCheck = IsKingReachable(board, true, senteKingPos ?? null);
            // Sente and Gote cannot both be in check
            bool goteCheck = !senteCheck && IsKingReachable(board, false, goteKingPos ?? null);

            if (currentTurnSente && !sentePieces.SelectMany(p => p.GetValidMoves(board, true)).Any())
            {
                // Gote may only win if they have sente king in check, otherwise draw
                return senteCheck ? GameState.CheckMateSente : GameState.DrawStalemate;
            }
            if (!currentTurnSente && !gotePieces.SelectMany(p => p.GetValidMoves(board, true)).Any())
            {
                // Sente may only win if they have gote king in check, otherwise draw
                return goteCheck ? GameState.CheckMateGote : GameState.DrawStalemate;
            }

            int sentePiecesCount = sentePieces.Count();
            int gotePiecesCount = gotePieces.Count();
            if ((sentePiecesCount == 1 || (sentePiecesCount == 2
                    && sentePieces.Where(p => p is not Pieces.King).First() is Pieces.Bishop or Pieces.Knight))
                && (gotePiecesCount == 1 || (gotePiecesCount == 2
                    && gotePieces.Where(p => p is not Pieces.King).First() is Pieces.Bishop or Pieces.Knight)))
            {
                return GameState.DrawInsufficientMaterial;
            }

            if ((sentePiecesCount == 1 && gotePiecesCount == 3 && gotePieces.OfType<Pieces.Knight>().Count() == 2)
                || (gotePiecesCount == 1 && sentePiecesCount == 3 && sentePieces.OfType<Pieces.Knight>().Count() == 2))
            {
                return GameState.DrawInsufficientMaterial;
            }

            return senteCheck ? GameState.CheckSente : goteCheck ? GameState.CheckGote : GameState.StandardPlay;
        }

        /// <summary>
        /// Calculate the value of the given board based on the remaining pieces
        /// </summary>
        /// <returns>
        /// A <see cref="double"/> representing the total piece value of the entire board.
        /// Positive means sente has stronger material, negative means gote does.
        /// </returns>
        public static double CalculateBoardValue(Pieces.Piece?[,] board)
        {
            return board.OfType<Pieces.Piece>().Sum(p => p.IsSente ? p.Value : -p.Value);
        }

        public readonly struct PossibleMove
        {
            public Point Source { get; }
            public Point Destination { get; }
            public double EvaluatedFutureValue { get; }
            public bool SenteMateLocated { get; }
            public bool GoteMateLocated { get; }
            public int DepthToSenteMate { get; }
            public int DepthToGoteMate { get; }
            public Type? PromotionType { get; }
            public List<(Point, Point, Type)> BestLine { get;  }

            public PossibleMove(Point source, Point destination, double evaluatedFutureValue,
                bool senteMateLocated, bool goteMateLocated, int depthToSenteMate, int depthToGoteMate,
                Type? promotionType, List<(Point, Point, Type)> bestLine)
            {
                Source = source;
                Destination = destination;
                EvaluatedFutureValue = evaluatedFutureValue;
                SenteMateLocated = senteMateLocated;
                GoteMateLocated = goteMateLocated;
                DepthToSenteMate = depthToSenteMate;
                DepthToGoteMate = depthToGoteMate;
                PromotionType = promotionType;
                BestLine = bestLine;
            }
        }

        /// <summary>
        /// Use <see cref="EvaluatePossibleMoves"/> to find the best possible move in the current state of the game
        /// </summary>
        /// <param name="maxDepth">The maximum number of half-moves in the future to search</param>
        public static async Task<PossibleMove> EstimateBestPossibleMove(ShogiGame game, int maxDepth, CancellationToken cancellationToken)
        {
            PossibleMove[] moves = await EvaluatePossibleMoves(game, maxDepth, cancellationToken);
            PossibleMove bestMove = new(default, default,
                game.CurrentTurnSente ? double.NegativeInfinity : double.PositiveInfinity, false, false, 0, 0, typeof(Pieces.Queen), new());
            foreach (PossibleMove potentialMove in moves)
            {
                if (game.CurrentTurnSente)
                {
                    if (bestMove.EvaluatedFutureValue == double.NegativeInfinity
                        || (!bestMove.GoteMateLocated && potentialMove.GoteMateLocated)
                        || (!bestMove.GoteMateLocated && potentialMove.EvaluatedFutureValue > bestMove.EvaluatedFutureValue)
                        || (bestMove.GoteMateLocated && potentialMove.GoteMateLocated
                            && potentialMove.DepthToGoteMate < bestMove.DepthToGoteMate))
                    {
                        bestMove = potentialMove;
                    }
                }
                else
                {
                    if (bestMove.EvaluatedFutureValue == double.PositiveInfinity
                        || (!bestMove.SenteMateLocated && potentialMove.SenteMateLocated)
                        || (!bestMove.SenteMateLocated && potentialMove.EvaluatedFutureValue < bestMove.EvaluatedFutureValue)
                        || (bestMove.SenteMateLocated && potentialMove.SenteMateLocated
                            && potentialMove.DepthToSenteMate < bestMove.DepthToSenteMate))
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
        public static async Task<PossibleMove[]> EvaluatePossibleMoves(ShogiGame game, int maxDepth, CancellationToken cancellationToken)
        {
            ConcurrentBag<PossibleMove> possibleMoves = new();
            int remainingThreads = 0;

            foreach (Pieces.Piece? piece in game.Board)
            {
                if (piece is not null)
                {
                    if (piece.IsSente != game.CurrentTurnSente)
                    {
                        continue;
                    }

                    foreach (Point validMove in GetValidMovesForEval(game, piece))
                    {
                        remainingThreads++;
                        Point thisPosition = piece.Position;
                        Point thisValidMove = validMove;
                        ShogiGame gameClone = game.Clone();
                        List<(Point, Point, Type)> thisLine = new() { (piece.Position, validMove, typeof(Pieces.Queen)) };
                        _ = gameClone.MovePiece(piece.Position, validMove, true,
                            promotionType: typeof(Pieces.Queen), updateMoveText: false);

                        Thread processThread = new(() =>
                        {
                            PossibleMove bestSubMove = MinimaxMove(gameClone,
                                double.NegativeInfinity, double.PositiveInfinity, 1, maxDepth, thisLine, cancellationToken);
                            // Don't include default value in results
                            if (bestSubMove.Source != bestSubMove.Destination)
                            {
                                possibleMoves.Add(new PossibleMove(thisPosition, thisValidMove, bestSubMove.EvaluatedFutureValue,
                                    bestSubMove.SenteMateLocated, bestSubMove.GoteMateLocated,
                                    bestSubMove.DepthToSenteMate, bestSubMove.DepthToGoteMate, typeof(Pieces.Queen), bestSubMove.BestLine));
                            }
                            remainingThreads--;
                        });
                        processThread.Start();
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

        private static HashSet<Point> GetValidMovesForEval(ShogiGame game, Pieces.Piece piece)
        {
            HashSet<Point> allValidMoves = piece.GetValidMoves(game.Board, true);

            if (piece is Pieces.Pawn && game.EnPassantSquare is not null
                && Math.Abs(piece.Position.X - game.EnPassantSquare.Value.X) == 1
                && piece.Position.Y == (game.CurrentTurnSente ? 4 : 3)
                && !IsKingReachable(game.Board.AfterMove(piece.Position,
                        game.EnPassantSquare.Value), game.CurrentTurnSente))
            {
                _ = allValidMoves.Add(game.EnPassantSquare.Value);
            }

            return allValidMoves;
        }

        private static PossibleMove MinimaxMove(ShogiGame game, double alpha, double beta, int depth, int maxDepth,
            List<(Point, Point, Type)> currentLine, CancellationToken cancellationToken)
        {
            (Point, Point) lastMove = game.Moves.Last();
            if (game.GameOver)
            {
                GameState state = game.DetermineGameState();
                if (state == GameState.CheckMateSente)
                {
                    return new PossibleMove(lastMove.Item1, lastMove.Item2, double.NegativeInfinity, true, false, depth, 0, typeof(Pieces.Queen),
                        currentLine);
                }
                else if (state == GameState.CheckMateGote)
                {
                    return new PossibleMove(lastMove.Item1, lastMove.Item2, double.PositiveInfinity, false, true, 0, depth, typeof(Pieces.Queen),
                        currentLine);
                }
                else
                {
                    // Draw
                    return new PossibleMove(lastMove.Item1, lastMove.Item2, 0, false, false, 0, 0, typeof(Pieces.Queen), currentLine);
                }
            }
            if (depth > maxDepth)
            {
                return new PossibleMove(lastMove.Item1, lastMove.Item2, CalculateBoardValue(game.Board), false, false, 0, 0, typeof(Pieces.Queen), currentLine);
            }

            PossibleMove bestMove = new(default, default,
                game.CurrentTurnSente ? double.NegativeInfinity : double.PositiveInfinity, false, false, 0, 0, typeof(Pieces.Queen), new());

            foreach (Pieces.Piece? piece in game.Board)
            {
                if (piece is not null)
                {
                    if (piece.IsSente != game.CurrentTurnSente)
                    {
                        continue;
                    }

                    foreach (Point validMove in GetValidMovesForEval(game, piece))
                    {
                        ShogiGame gameClone = game.Clone();
                        List<(Point, Point, Type)> newLine = new(currentLine) { (piece.Position, validMove, typeof(Pieces.Queen)) };
                        _ = gameClone.MovePiece(piece.Position, validMove, true,
                            promotionType: typeof(Pieces.Queen), updateMoveText: false);
                        PossibleMove potentialMove = MinimaxMove(gameClone, alpha, beta, depth + 1, maxDepth, newLine, cancellationToken);
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return bestMove;
                        }
                        if (game.CurrentTurnSente)
                        {
                            if (bestMove.EvaluatedFutureValue == double.NegativeInfinity
                                || (!bestMove.GoteMateLocated && potentialMove.GoteMateLocated)
                                || (!bestMove.GoteMateLocated && potentialMove.EvaluatedFutureValue > bestMove.EvaluatedFutureValue)
                                || (bestMove.GoteMateLocated && potentialMove.GoteMateLocated
                                    && potentialMove.DepthToGoteMate < bestMove.DepthToGoteMate))
                            {
                                bestMove = new PossibleMove(piece.Position, validMove, potentialMove.EvaluatedFutureValue,
                                    potentialMove.SenteMateLocated, potentialMove.GoteMateLocated,
                                    potentialMove.DepthToSenteMate, potentialMove.DepthToGoteMate, typeof(Pieces.Queen), potentialMove.BestLine);
                            }
                            if (potentialMove.EvaluatedFutureValue >= beta && !bestMove.GoteMateLocated)
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
                                || (!bestMove.SenteMateLocated && potentialMove.SenteMateLocated)
                                || (!bestMove.SenteMateLocated && potentialMove.EvaluatedFutureValue < bestMove.EvaluatedFutureValue)
                                || (bestMove.SenteMateLocated && potentialMove.SenteMateLocated
                                    && potentialMove.DepthToSenteMate < bestMove.DepthToSenteMate))
                            {
                                bestMove = new PossibleMove(piece.Position, validMove, potentialMove.EvaluatedFutureValue,
                                    potentialMove.SenteMateLocated, potentialMove.GoteMateLocated,
                                    potentialMove.DepthToSenteMate, potentialMove.DepthToGoteMate, typeof(Pieces.Queen), potentialMove.BestLine);
                            }
                            if (potentialMove.EvaluatedFutureValue <= alpha && !bestMove.SenteMateLocated)
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

            return bestMove;
        }
    }
}
