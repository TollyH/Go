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
            foreach ((Type dropType, int count) in game.BlackStoneDrops)
            {
                inHandTotal += count;
            }
            foreach ((Type dropType, int count) in game.WhiteStoneDrops)
            {
                inHandTotal -= count;
            }
            return inHandTotal + game.Board.OfType<bool>().Sum(p => p ? 1 : -1);
        }

        // TODO: Remove unnecessary data
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

            // TODO: Remove drop counts, just iterate whole board
            Dictionary<Type, int> dropCounts = game.CurrentTurnBlack ? game.BlackStoneDrops : game.WhiteStoneDrops;
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
                                GoGame gameClone = game.Clone();
                                List<(Point, Point, bool)> thisLine = new() { (thisDropPoint, thisDropPoint, false) };
                                _ = gameClone.MoveStone(thisDropPoint, thisDropPoint, true, updateMoveText: false);

                                Thread processThread = new(() =>
                                {
                                    PossibleMove bestSubMove = MinimaxMove(gameClone,
                                        double.NegativeInfinity, double.PositiveInfinity, 1, maxDepth, thisLine, cancellationToken);
                                    // Don't include default value in results
                                    if (bestSubMove.Source != bestSubMove.Destination)
                                    {
                                        possibleMoves.Add(new PossibleMove(thisDropPoint, pt,
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

        private static PossibleMove MinimaxMove(GoGame game, double alpha, double beta, int depth, int maxDepth,
            List<(Point, Point, bool)> currentLine, CancellationToken cancellationToken)
        {
            (_, Point lastMoveSrc, Point lastMoveDst, _, _) = game.Moves.Last();
            if (game.GameOver)
            {
                // TODO: Check if game is won and who by
                if (false)
                {
                    return new PossibleMove(lastMoveSrc, lastMoveDst, double.NegativeInfinity, true, false, depth, 0, false,
                        currentLine);
                }
                else if (false)
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

            // TODO: Remove drop counts, just iterate whole board
            Dictionary<Type, int> dropCounts = game.CurrentTurnBlack ? game.BlackStoneDrops : game.WhiteStoneDrops;
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
                                List<(Point, Point, bool)> newLine = new(currentLine) { (dropPoint, dropPoint, false) };
                                _ = gameClone.MoveStone(dropPoint, dropPoint, true, updateMoveText: false);
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
                                        bestMove = new PossibleMove(dropPoint, dropPoint, potentialMove.EvaluatedFutureValue,
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
                                        bestMove = new PossibleMove(dropPoint, dropPoint, potentialMove.EvaluatedFutureValue,
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
