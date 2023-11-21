using System;
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

        private static readonly Random rng = new();

        public static T[,] TwoDimensionalClone<T>(this T[,] array)
        {
            T[,] arrayClone = new T[array.GetLength(0), array.GetLength(1)];
            for (int x = 0; x < array.GetLength(0); x++)
            {
                for (int y = 0; y < array.GetLength(1); y++)
                {
                    arrayClone[x, y] = array[x, y];
                }
            }
            return arrayClone;
        }
        /// <summary>
        /// Fill in empty board intersections that are surrounded by a single colour with that colour.
        /// </summary>
        /// <param name="copyExistingStones">
        /// Whether or not stones on the source board should be copied to the new board.
        /// Usually <see langword="true"/> for <see cref="ScoringSystem.Area"/>
        /// and <see langword="false"/> for <see cref="ScoringSystem.Territory"/>
        /// </param>
        /// <remarks>This method does not directly modify the <paramref name="board"/> parameter.</remarks>
        public static bool?[,] FillSurroundedAreas(bool?[,] board, bool copyExistingStones)
        {
            int boardWidth = board.GetLength(0);
            int boardHeight = board.GetLength(1);

            bool?[,] newBoard = new bool?[boardWidth, boardHeight];
            HashSet<Point> scannedPoints = new();

            for (int x = 0; x < boardWidth; x++)
            {
                for (int y = 0; y < boardHeight; y++)
                {
                    Point startPoint = new(x, y);
                    bool? startingColour = board[x, y];
                    if (startingColour is not null)
                    {
                        if (copyExistingStones)
                        {
                            newBoard[x, y] = startingColour;
                        }
                        continue;
                    }
                    if (scannedPoints.Contains(startPoint))
                    {
                        continue;
                    }
                    // The current group is empty liberties that are connected to the starting liberty
                    List<Point> currentGroup = new() { startPoint };
                    int surroundingBlack = 0;
                    int surroundingWhite = 0;
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
                                || adjPoint.X >= boardWidth || adjPoint.Y >= boardHeight)
                            {
                                // Out of bounds
                                continue;
                            }

                            bool? adjColour = board[adjPoint.X, adjPoint.Y];
                            if (adjColour is null)
                            {
                                // If the surrounding liberty is also empty, add it to the group and scan its surroundings too
                                currentGroup.Add(adjPoint);
                                pointsQueue.Enqueue(adjPoint);
                            }
                            else if (adjColour.Value)
                            {
                                surroundingBlack++;
                            }
                            else
                            {
                                surroundingWhite++;
                            }
                        }
                    }
                    if (surroundingBlack == 0 && surroundingWhite > 0)
                    {
                        foreach (Point pt in currentGroup)
                        {
                            newBoard[pt.X, pt.Y] = false;
                        }
                    }
                    else if (surroundingWhite == 0 && surroundingBlack > 0)
                    {
                        foreach (Point pt in currentGroup)
                        {
                            newBoard[pt.X, pt.Y] = true;
                        }
                    }
                }
            }
            return newBoard;
        }

        /// <summary>
        /// Calculate the value of the given game with a given scoring system
        /// </summary>
        /// <returns>
        /// A <see cref="double"/> representing the total value of the game.
        /// Positive means black has more material, negative means white does.
        /// </returns>
        public static double CalculateGameValue(GoGame game, ScoringSystem scoring)
        {
            switch (scoring)
            {
                default:
                case ScoringSystem.Area:
                    bool?[,] filledBoard = FillSurroundedAreas(game.Board, true);
                    return filledBoard.OfType<bool>().Sum(p => p ? 1 : -1) - game.KomiCompensation;
                case ScoringSystem.Territory:
                    filledBoard = FillSurroundedAreas(game.Board, false);
                    int boardValue = filledBoard.OfType<bool>().Sum(p => p ? 1 : -1);
                    boardValue += game.BlackCaptures;
                    boardValue -= game.WhiteCaptures;
                    return boardValue - game.KomiCompensation;
                case ScoringSystem.Stone:
                    return game.Board.OfType<bool>().Sum(p => p ? 1 : -1) - game.KomiCompensation;
            }
        }

        /// <summary>
        /// Get the list of stones on a board that have been captured and need to be removed.
        /// </summary>
        public static Point[] GetSurroundedStones(bool?[,] board, bool currentTurnBlack)
        {
            int boardWidth = board.GetLength(0);
            int boardHeight = board.GetLength(1);

            List<Point> surroundedStones = new();
            HashSet<Point> scannedPoints = new();
            bool anyBlackCaptured = false;
            bool anyWhiteCaptured = false;
            for (int x = 0; x < boardWidth; x++)
            {
                for (int y = 0; y < boardHeight; y++)
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
                                || adjPoint.X >= boardWidth || adjPoint.Y >= boardHeight)
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

        /// <summary>
        /// Fill in a group of connected board intersections with a given value.
        /// </summary>
        /// <param name="sourceBoard">If not <see langword="null"/>, will be used as the board to check existing pieces.</param>
        /// <remarks>This method modifies the <paramref name="board"/> parameter in-place.</remarks>
        public static void FloodFillArea(bool?[,] board, Point startPoint, bool? fillValue, bool?[,]? sourceBoard = null)
        {
            int boardWidth = board.GetLength(0);
            int boardHeight = board.GetLength(1);
            sourceBoard ??= board;

            HashSet<Point> scannedPoints = new();
            bool? startingColour = sourceBoard[startPoint.X, startPoint.Y];
            Queue<Point> pointsQueue = new();
            pointsQueue.Enqueue(startPoint);
            while (pointsQueue.TryDequeue(out Point pt))
            {
                if (!scannedPoints.Add(pt))
                {
                    // Skip already scanned points
                    continue;
                }

                board[pt.X, pt.Y] = fillValue;

                // Scan surrounding liberties
                foreach (Point diff in Liberties)
                {
                    Point adjPoint = new(pt.X + diff.X, pt.Y + diff.Y);
                    if (adjPoint.X < 0 || adjPoint.Y < 0
                        || adjPoint.X >= boardWidth || adjPoint.Y >= boardHeight)
                    {
                        // Out of bounds
                        continue;
                    }

                    bool? adjColour = sourceBoard[adjPoint.X, adjPoint.Y];
                    if (adjColour == startingColour)
                    {
                        pointsQueue.Enqueue(adjPoint);
                    }
                }
            }
        }

        public readonly struct PossibleMove
        {
            /// <summary>
            /// A pass is represented by a destination of (-1, -1)
            /// </summary>
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
        /// <param name="randomise">Whether or not to randomise the order of moves that have the same score</param>
        public static async Task<PossibleMove?> EstimateBestPossibleMove(GoGame game, int maxDepth, bool randomise, CancellationToken cancellationToken)
        {
            PossibleMove[] moves = await EvaluatePossibleMoves(game, maxDepth, randomise, cancellationToken);
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
        /// <param name="randomise">Whether or not to randomise the order of moves that have the same score</param>
        /// <returns>An array of all possible moves, with information on board value and ability to checkmate</returns>
        public static async Task<PossibleMove[]> EvaluatePossibleMoves(GoGame game, int maxDepth, bool randomise, CancellationToken cancellationToken)
        {
            List<Task<PossibleMove?>> evaluationTasks = new();

            for (int x = 0; x < game.Board.GetLength(0); x++)
            {
                for (int y = 0; y < game.Board.GetLength(1); y++)
                {
                    Point pt = new(x, y);
                    if (game.IsPlacementPossible(pt))
                    {
                        Point thisPlacement = pt;
                        evaluationTasks.Add(Task.Run(() =>
                        {
                            GoGame gameClone = game.Clone(false);
                            List<Point> thisLine = new() { thisPlacement };
                            _ = gameClone.PlaceStone(thisPlacement, true, updateMoveText: false);

                            PossibleMove? bestSubMove = MinimaxMove(gameClone,
                                double.NegativeInfinity, double.PositiveInfinity, 1, maxDepth, thisLine, cancellationToken);
                            
                            return (PossibleMove?)(bestSubMove is null ? null : new PossibleMove(thisPlacement,
                                bestSubMove.Value.EvaluatedFutureValue, bestSubMove.Value.BestLine));
                        }, cancellationToken));
                    }
                }
            }

            // Spawn a task to test passing the turn
            evaluationTasks.Add(Task.Run(() =>
            {
                GoGame passGameClone = game.Clone(false);
                List<Point> passLine = new() { new Point(-1, -1) };
                _ = passGameClone.PassTurn(updateMoveText: false);

                PossibleMove? bestSubMove = MinimaxMove(passGameClone,
                    double.NegativeInfinity, double.PositiveInfinity, 1, maxDepth, passLine, cancellationToken);

                return (PossibleMove?)(bestSubMove is null ? null : new PossibleMove(new Point(-1, -1),
                    bestSubMove.Value.EvaluatedFutureValue, bestSubMove.Value.BestLine));
            }, cancellationToken));

            if (cancellationToken.IsCancellationRequested)
            {
                return Array.Empty<PossibleMove>();
            }
            try
            {
                IEnumerable<PossibleMove> moves =
                    (await Task.WhenAll(evaluationTasks)).Where(m => m is not null).Select(m => m!.Value);
                if (randomise)
                {
                    return moves.OrderBy(_ => rng.Next()).ToArray();
                }
                // Remove default moves from return value
                return moves.ToArray();
            }
            catch (TaskCanceledException)
            {
                return Array.Empty<PossibleMove>();
            }
        }

        private static PossibleMove? MinimaxMove(GoGame game, double alpha, double beta, int depth, int maxDepth,
            List<Point> currentLine, CancellationToken cancellationToken)
        {
            Point lastMoveDst = game.Moves.Last();
            if (depth > maxDepth || game.GameOver)
            {
                return new PossibleMove(lastMoveDst, CalculateGameValue(game, game.CurrentScoring), currentLine);
            }

            PossibleMove? bestMove = null;

            for (int x = 0; x < game.Board.GetLength(0); x++)
            {
                for (int y = 0; y < game.Board.GetLength(1); y++)
                {
                    Point pt = new(x, y);
                    if (game.IsPlacementPossible(pt))
                    {
                        GoGame gameClone = game.Clone(false);
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

            // Test passing the turn
            GoGame passGameClone = game.Clone(false);
            List<Point> passLine = new() { new Point(-1, -1) };
            _ = passGameClone.PassTurn(updateMoveText: false);
            PossibleMove? passMove = MinimaxMove(passGameClone, alpha, beta, depth + 1, maxDepth, passLine, cancellationToken);
            if (passMove is not null)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return bestMove;
                }
                if (game.CurrentTurnBlack)
                {
                    if (bestMove is null || double.IsNegativeInfinity(bestMove.Value.EvaluatedFutureValue)
                        || passMove.Value.EvaluatedFutureValue > bestMove.Value.EvaluatedFutureValue)
                    {
                        bestMove = new PossibleMove(new Point(-1, -1), passMove.Value.EvaluatedFutureValue, passMove.Value.BestLine);
                    }
                    if (passMove.Value.EvaluatedFutureValue >= beta)
                    {
                        return bestMove;
                    }
                }
                else
                {
                    if (bestMove is null || double.IsPositiveInfinity(bestMove.Value.EvaluatedFutureValue)
                        || passMove.Value.EvaluatedFutureValue < bestMove.Value.EvaluatedFutureValue)
                    {
                        bestMove = new PossibleMove(new Point(-1, -1), passMove.Value.EvaluatedFutureValue, passMove.Value.BestLine);
                    }
                    if (passMove.Value.EvaluatedFutureValue <= alpha)
                    {
                        return bestMove;
                    }
                }
            }

            return bestMove;
        }
    }
}
