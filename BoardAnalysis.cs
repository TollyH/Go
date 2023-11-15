﻿using System;
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
        public static readonly Point[] Liberties = new Point[4]
        {
            new(-1, 0), new(1, 0), new(0, -1), new(0, 1)
        };

        /// <summary>
        /// Calculate the value of the given game based on the stones on the board and in hand
        /// </summary>
        /// <returns>
        /// A <see cref="double"/> representing the total stone value of the game.
        /// Positive means black has stronger material, negative means white does.
        /// </returns>
        // TODO: New logic for Go, include parameter for whether to use Japanese or Chinese scoring
        public static double CalculateGameValue(GoGame game)
        {
            double inHandTotal = 0;
            return inHandTotal + game.Board.OfType<bool>().Sum(p => p ? 1 : -1);
        }

        /// <summary>
        /// Get the list of stones on a board that have been captured and need to be removed.
        /// </summary>
        public static Point[] GetSurroundedStones(bool?[,] board, bool currentTurnBlack)
        {
            List<Point> surroundedStones = new();
            HashSet<Point> scannedPoints = new();
            bool anyBlackCaptured = false;
            bool anyWhiteCaptured = false;
            for (int x = 0; x < board.GetLength(0); x++)
            {
                for (int y = 0; y < board.GetLength(1); y++)
                {
                    Point startPoint = new(x, y);
                    bool? startingColour = board[x, y];
                    if (startingColour is null || scannedPoints.Contains(startPoint))
                    {
                        continue;
                    }
                    // The current group is points of the same colour that are connected to the starting stone
                    List<Point> currentGroup = new() { startPoint };
                    // Will be set to false if any empty liberties are found surrounding the current group
                    bool fullySurrounded = true;
                    Queue<Point> pointsQueue = new();
                    pointsQueue.Enqueue(startPoint);
                    while (pointsQueue.TryDequeue(out Point pt))
                    {
                        if (!scannedPoints.Add(pt))
                        {
                            // Skip already scanned points
                            continue;
                        }

                        // Scan surrounding liberties
                        foreach (Point diff in Liberties)
                        {
                            Point adjPoint = new(pt.X + diff.X, pt.Y + diff.Y);
                            if (adjPoint.X < 0 || adjPoint.Y < 0
                                || adjPoint.X >= board.GetLength(0) || adjPoint.Y >= board.GetLength(1))
                            {
                                // Out of bounds
                                continue;
                            }

                            bool? adjColour = board[adjPoint.X, adjPoint.Y];
                            if (adjColour is null)
                            {
                                // If there is an empty liberty surrounding the current group, it cannot be a capture
                                fullySurrounded = false;
                            }
                            else if (adjColour == startingColour)
                            {
                                // If the surrounding piece is the same colour as the group we're scanning,
                                // add it to the group and scan its surroundings too
                                currentGroup.Add(adjPoint);
                                pointsQueue.Enqueue(adjPoint);
                            }
                        }
                    }
                    if (fullySurrounded)
                    {
                        if (startingColour.Value)
                        {
                            anyBlackCaptured = true;
                        }
                        else
                        {
                            anyWhiteCaptured = true;
                        }
                        surroundedStones.AddRange(currentGroup);
                    }
                }
            }
            if (anyBlackCaptured && anyWhiteCaptured)
            {
                // If the last move caused both black and white pieces to be apparently captured,
                // stop the pieces of the colour who's turn it was from being captured
                return surroundedStones.Where(s => board[s.X, s.Y] != currentTurnBlack).ToArray();
            }
            return surroundedStones.ToArray();
        }

        public readonly struct PossibleMove
        {
            public Point Destination { get; }
            public double EvaluatedFutureValue { get; }
            public List<Point> BestLine { get; }

            public PossibleMove(Point destination, double evaluatedFutureValue, List<Point> bestLine)
            {
                Destination = destination;
                EvaluatedFutureValue = evaluatedFutureValue;
                BestLine = bestLine;
            }
        }

        /// <summary>
        /// Use <see cref="EvaluatePossibleMoves"/> to find the best possible move in the current state of the game
        /// </summary>
        /// <param name="maxDepth">The maximum number of half-moves in the future to search</param>
        public static async Task<PossibleMove?> EstimateBestPossibleMove(GoGame game, int maxDepth, CancellationToken cancellationToken)
        {
            PossibleMove[] moves = await EvaluatePossibleMoves(game, maxDepth, cancellationToken);
            PossibleMove? bestMove = null;
            foreach (PossibleMove potentialMove in moves)
            {
                if (game.CurrentTurnBlack)
                {
                    if (bestMove is null || double.IsNegativeInfinity(bestMove.Value.EvaluatedFutureValue)
                        || potentialMove.EvaluatedFutureValue > bestMove.Value.EvaluatedFutureValue)
                    {
                        bestMove = potentialMove;
                    }
                }
                else
                {
                    if (bestMove is null || double.IsPositiveInfinity(bestMove.Value.EvaluatedFutureValue)
                        || potentialMove.EvaluatedFutureValue < bestMove.Value.EvaluatedFutureValue)
                    {
                        bestMove = potentialMove;
                    }
                }
            }
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
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

            // TODO: Also test passing
            for (int x = 0; x < game.Board.GetLength(0); x++)
            {
                for (int y = 0; y < game.Board.GetLength(1); y++)
                {
                    Point pt = new(x, y);
                    if (game.IsPlacementPossible(pt))
                    {
                        remainingThreads++;
                        Point thisPlacement = pt;
                        GoGame gameClone = game.Clone();
                        List<Point> thisLine = new() { thisPlacement };
                        _ = gameClone.PlaceStone(thisPlacement, true, updateMoveText: false);

                        Thread processThread = new(() =>
                        {
                            PossibleMove? bestSubMove = MinimaxMove(gameClone,
                                double.NegativeInfinity, double.PositiveInfinity, 1, maxDepth, thisLine, cancellationToken);
                            // Don't include default value in results
                            if (bestSubMove is not null)
                            {
                                possibleMoves.Add(new PossibleMove(thisPlacement,
                                    bestSubMove.Value.EvaluatedFutureValue, bestSubMove.Value.BestLine));
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

        private static PossibleMove? MinimaxMove(GoGame game, double alpha, double beta, int depth, int maxDepth,
            List<Point> currentLine, CancellationToken cancellationToken)
        {
            Point lastMoveDst = game.Moves.Last();
            if (game.GameOver)
            {
                // TODO: Check if game is won and who by
                if (false)
                {
                    return new PossibleMove(lastMoveDst, double.NegativeInfinity, currentLine);
                }
                else if (false)
                {
                    return new PossibleMove(lastMoveDst, double.PositiveInfinity, currentLine);
                }
                else
                {
                    // Draw
                    return new PossibleMove(lastMoveDst, 0, currentLine);
                }
            }
            if (depth > maxDepth)
            {
                return new PossibleMove( lastMoveDst, CalculateGameValue(game), currentLine);
            }

            PossibleMove? bestMove = null;

            // TODO: Also test passing
            for (int x = 0; x < game.Board.GetLength(0); x++)
            {
                for (int y = 0; y < game.Board.GetLength(1); y++)
                {
                    Point pt = new(x, y);
                    if (game.IsPlacementPossible(pt))
                    {
                        GoGame gameClone = game.Clone();
                        List<Point> newLine = new(currentLine) { pt };
                        _ = gameClone.PlaceStone(pt, true, updateMoveText: false);
                        PossibleMove? potentialMove = MinimaxMove(gameClone, alpha, beta, depth + 1, maxDepth, newLine, cancellationToken);
                        if (potentialMove is null)
                        {
                            continue;
                        }
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return bestMove;
                        }
                        if (game.CurrentTurnBlack)
                        {
                            if (bestMove is null || double.IsNegativeInfinity(bestMove.Value.EvaluatedFutureValue)
                                || potentialMove.Value.EvaluatedFutureValue > bestMove.Value.EvaluatedFutureValue)
                            {
                                bestMove = new PossibleMove(pt, potentialMove.Value.EvaluatedFutureValue, potentialMove.Value.BestLine);
                            }
                            if (potentialMove.Value.EvaluatedFutureValue >= beta)
                            {
                                return bestMove;
                            }
                            if (potentialMove.Value.EvaluatedFutureValue > alpha)
                            {
                                alpha = potentialMove.Value.EvaluatedFutureValue;
                            }
                        }
                        else
                        {
                            if (bestMove is null || double.IsPositiveInfinity(bestMove.Value.EvaluatedFutureValue)
                                || potentialMove.Value.EvaluatedFutureValue < bestMove.Value.EvaluatedFutureValue)
                            {
                                bestMove = new PossibleMove(pt, potentialMove.Value.EvaluatedFutureValue, potentialMove.Value.BestLine);
                            }
                            if (potentialMove.Value.EvaluatedFutureValue <= alpha)
                            {
                                return bestMove;
                            }
                            if (potentialMove.Value.EvaluatedFutureValue < beta)
                            {
                                beta = potentialMove.Value.EvaluatedFutureValue;
                            }
                        }
                    }
                }
            }

            return bestMove;
        }
    }
}
