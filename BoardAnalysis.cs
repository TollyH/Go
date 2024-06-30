using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Go
{
    public static class BoardAnalysis
    {
        public static readonly Point[] Liberties = new Point[4]
        {
            new(-1, 0), new(1, 0), new(0, -1), new(0, 1)
        };

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
        public static int?[,] FillSurroundedAreas(int?[,] board, bool copyExistingStones)
        {
            int boardWidth = board.GetLength(0);
            int boardHeight = board.GetLength(1);

            int?[,] newBoard = new int?[boardWidth, boardHeight];
            HashSet<Point> scannedPoints = new();

            for (int x = 0; x < boardWidth; x++)
            {
                for (int y = 0; y < boardHeight; y++)
                {
                    Point startPoint = new(x, y);
                    int? startingColour = board[x, y];
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
                    int[] surrounding = new int[10];
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

                            int? adjColour = board[adjPoint.X, adjPoint.Y];
                            if (adjColour is null)
                            {
                                // If the surrounding liberty is also empty, add it to the group and scan its surroundings too
                                currentGroup.Add(adjPoint);
                                pointsQueue.Enqueue(adjPoint);
                            }
                            else
                            {
                                surrounding[adjColour.Value]++;
                            }
                        }
                    }
                    if (surrounding.Count(s => s > 0) == 1)
                    {
                        // Get index of first non-zero item
                        int fillColour = surrounding.Select((s, i) => (s, i)).First(s => s.s != 0).i;
                        foreach (Point pt in currentGroup)
                        {
                            newBoard[pt.X, pt.Y] = fillColour;
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
        /// The scores of each player on the board.
        /// </returns>
        public static int[] CalculateGameValue(GoGame game, ScoringSystem scoring)
        {
            int[] scores = new int[game.PlayerCount];
            switch (scoring)
            {
                default:
                case ScoringSystem.Area:
                    int?[,] filledBoard = FillSurroundedAreas(game.Board, true);
                    foreach (int stone in filledBoard.OfType<int>())
                    {
                        scores[stone]++;
                    }
                    break;
                case ScoringSystem.Territory:
                    filledBoard = FillSurroundedAreas(game.Board, false);
                    foreach (int stone in filledBoard.OfType<int>())
                    {
                        scores[stone]++;
                    }

                    for (int i = 0; i < game.Captures.Length; i++)
                    {
                        scores[i] += game.Captures[i];
                    }
                    break;
                case ScoringSystem.Stone:
                    foreach (int stone in game.Board.OfType<int>())
                    {
                        scores[stone]++;
                    }
                    break;
            }
            return scores;
        }

        /// <summary>
        /// Get the list of stones on a board that have been captured and need to be removed.
        /// </summary>
        public static Point[] GetSurroundedStones(int?[,] board, int currentTurn)
        {
            int boardWidth = board.GetLength(0);
            int boardHeight = board.GetLength(1);

            List<Point> surroundedStones = new();
            HashSet<Point> scannedPoints = new();
            bool[] anyCaptured = new bool[10];
            for (int x = 0; x < boardWidth; x++)
            {
                for (int y = 0; y < boardHeight; y++)
                {
                    Point startPoint = new(x, y);
                    int? startingColour = board[x, y];
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

                            int? adjColour = board[adjPoint.X, adjPoint.Y];
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
                        anyCaptured[startingColour.Value] = true;
                        surroundedStones.AddRange(currentGroup);
                    }
                }
            }
            if (anyCaptured[currentTurn] && anyCaptured.Count(c => c) >= 2)
            {
                // If the last move caused multiple players' pieces to be apparently captured,
                // stop the pieces of the colour who's turn it was from being captured
                return surroundedStones.Where(s => board[s.X, s.Y] != currentTurn).ToArray();
            }
            return surroundedStones.ToArray();
        }

        /// <summary>
        /// Fill in a group of connected board intersections with a given value.
        /// </summary>
        /// <param name="sourceBoard">If not <see langword="null"/>, will be used as the board to check existing pieces.</param>
        /// <remarks>This method modifies the <paramref name="board"/> parameter in-place.</remarks>
        public static void FloodFillArea(int?[,] board, Point startPoint, int? fillValue, int?[,]? sourceBoard = null)
        {
            int boardWidth = board.GetLength(0);
            int boardHeight = board.GetLength(1);
            sourceBoard ??= board;

            HashSet<Point> scannedPoints = new();
            int? startingColour = sourceBoard[startPoint.X, startPoint.Y];
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

                    int? adjColour = sourceBoard[adjPoint.X, adjPoint.Y];
                    if (adjColour == startingColour)
                    {
                        pointsQueue.Enqueue(adjPoint);
                    }
                }
            }
        }
    }
}
