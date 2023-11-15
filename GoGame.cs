using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Go
{
    public sealed class GoGame
    {
        public static readonly Point[] Liberties = new Point[4]
        {
            new(-1, 0), new(1, 0), new(0, -1), new(0, 1)
        };

        /// <summary>
        /// <see langword="null"/> = no stone,
        /// <see langword="true"/> = black stone,
        /// <see langword="false"/> = white stone
        /// </summary>
        public bool?[,] Board { get; }
        public string InitialState { get; }

        public bool CurrentTurnBlack { get; private set; }
        public bool GameOver { get; private set; }

        public List<Point> Moves { get; }
        public List<string> MoveText { get; }
        public GoGame? PreviousGameState { get; private set; }
        /// <summary>
        /// The number of captures black has made (i.e. the number of white stones removed)
        /// </summary>
        public int BlackCaptures { get; set; }
        /// <summary>
        /// The number of captures white has made (i.e. the number of black stones removed)
        /// </summary>
        public int WhiteCaptures { get; set; }

        // Used to detect repetition
        public HashSet<string> PreviousBoards { get; }

        /// <summary>
        /// Create a new standard go game with all values at their defaults
        /// </summary>
        public GoGame(int boardWidth, int boardHeight)
        {
            CurrentTurnBlack = true;
            GameOver = false;

            Moves = new List<Point>();
            MoveText = new List<string>();

            PreviousBoards = new HashSet<string>();

            Board = new bool?[boardWidth, boardHeight];

            InitialState = ToString();
        }

        /// <summary>
        /// Create a new instance of a go game, setting each game parameter to a non-default value
        /// </summary>
        public GoGame(bool?[,] board, bool currentTurnBlack, bool gameOver,
            List<Point> moves, List<string> moveText, int blackCaptures,
            int whiteCaptures, HashSet<string> previousBoards,
            string? initialState, GoGame? previousGameState)
        {
            Board = board;

            CurrentTurnBlack = currentTurnBlack;
            GameOver = gameOver;
            Moves = moves;
            MoveText = moveText;
            BlackCaptures = blackCaptures;
            WhiteCaptures = whiteCaptures;
            PreviousBoards = previousBoards;

            InitialState = initialState ?? ToString();
            PreviousGameState = previousGameState;
        }

        /// <summary>
        /// Create a deep copy of all parameters to this go game
        /// </summary>
        public GoGame Clone()
        {
            bool?[,] boardClone = new bool?[Board.GetLength(0), Board.GetLength(1)];
            for (int x = 0; x < boardClone.GetLength(0); x++)
            {
                for (int y = 0; y < boardClone.GetLength(1); y++)
                {
                    boardClone[x, y] = Board[x, y];
                }
            }

            return new GoGame(boardClone, CurrentTurnBlack, GameOver, new List<Point>(Moves),
                new List<string>(MoveText), BlackCaptures, WhiteCaptures,
                new HashSet<string>(PreviousBoards), InitialState, PreviousGameState?.Clone());
        }

        /// <summary>
        /// Get a list of stones on the board that have been captured and need to be removed.
        /// </summary>
        public Point[] GetSurroundedStones()
        {
            List<Point> surroundedStones = new();
            HashSet<Point> scannedPoints = new();
            bool anyBlackCaptured = false;
            bool anyWhiteCaptured = false;
            for (int x = 0; x < Board.GetLength(0); x++)
            {
                for (int y = 0; y < Board.GetLength(1); y++)
                {
                    Point startPoint = new(x, y);
                    bool? startingColour = Board[x, y];
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
                                || adjPoint.X >= Board.GetLength(0) || adjPoint.Y >= Board.GetLength(1))
                            {
                                // Out of bounds
                                continue;
                            }

                            bool? adjColour = Board[adjPoint.X, adjPoint.Y];
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
                return surroundedStones.Where(s => Board[s.X, s.Y] != CurrentTurnBlack).ToArray();
            }
            return surroundedStones.ToArray();
        }

        /// <summary>
        /// Remove fully surrounded stones from the board.
        /// </summary>
        /// <returns>The number of black and white stones removed respectively.</returns>
        public (int RemovedBlack, int RemovedWhite) RemoveSurroundedStones()
        {
            int removedBlack = 0;
            int removedWhite = 0;
            foreach (Point coord in GetSurroundedStones())
            {
                bool? stone = Board[coord.X, coord.Y];
                if (stone is not null)
                {
                    if (stone.Value)
                    {
                        removedBlack++;
                    }
                    else
                    {
                        removedWhite++;
                    }
                }
                Board[coord.X, coord.Y] = null;
            }
            return (removedBlack, removedWhite);
        }

        /// <summary>
        /// Determine whether a placement of a stone to the given destination is valid or not.
        /// </summary>
        public bool IsPlacementPossible(Point destination)
        {
            if (destination.X < 0 || destination.Y < 0
                || destination.X >= Board.GetLength(0) || destination.Y >= Board.GetLength(1))
            {
                return false;
            }
            if (Board[destination.X, destination.Y] is not null)
            {
                return false;
            }

            // Temporarily set the piece at the destination to run capture check to check for suicide
            Board[destination.X, destination.Y] = CurrentTurnBlack;
            bool suicide = GetSurroundedStones().Any(s => Board[s.X, s.Y] == CurrentTurnBlack);
            Board[destination.X, destination.Y] = null;
            return !suicide;
        }

        /// <summary>
        /// Place a stone of the current player's colour on the board at the specified <paramref name="destination"/> coordinate.
        /// </summary>
        /// <param name="updateMoveText">
        /// Whether the move should update the game move text and update <see cref="PreviousGameState"/>. This should usually be <see langword="true"/>,
        /// but may be set to <see langword="false"/> for performance optimisations in clone games for analysis.
        /// </param>
        /// <returns><see langword="true"/> if the move was valid and executed, <see langword="false"/> otherwise</returns>
        /// <remarks>This method will check if the move is completely valid, unless <paramref name="forceMove"/> is <see langword="true"/>. No other validity checks are required.</remarks>
        public bool PlaceStone(Point destination, bool forceMove = false, bool updateMoveText = true)
        {
            if (!forceMove && (GameOver || !IsPlacementPossible(destination)))
            {
                return false;
            }

            if (updateMoveText)
            {
                // Used for move undoing
                PreviousGameState = Clone();
            }
            Moves.Add(destination);

            Board[destination.X, destination.Y] = CurrentTurnBlack;

            (int removedBlack, int removedWhite) = RemoveSurroundedStones();
            BlackCaptures += removedWhite;
            WhiteCaptures += removedBlack;

            CurrentTurnBlack = !CurrentTurnBlack;
            _ = PreviousBoards.Add(ToString());

            if (updateMoveText)
            {
                // CurrentTurnBlack has already flipped at this point
                string newMove = CurrentTurnBlack ? "W" : "B";
                newMove += $"{Board.GetLength(0) - destination.X}-{Board.GetLength(1) - destination.Y}";
                MoveText.Add(newMove);
            }

            return true;
        }

        /// <summary>
        /// Get a string representation of the given board.
        /// </summary>
        // TODO: Create new format to represent game as string
        public override string ToString()
        {
            StringBuilder result = new(90);  // TODO: Calculate max length of new format

            for (int y = Board.GetLength(1) - 1; y >= 0; y--)
            {
                int consecutiveNull = 0;
                for (int x = 0; x < Board.GetLength(0); x++)
                {
                    bool? stone = Board[x, y];
                    if (stone is null)
                    {
                        consecutiveNull++;
                    }
                    else
                    {
                        if (consecutiveNull > 0)
                        {
                            _ = result.Append(consecutiveNull);
                            consecutiveNull = 0;
                        }
                        _ = result.Append(stone.Value ? 'b' : 'w');
                    }
                }
                if (consecutiveNull > 0)
                {
                    _ = result.Append(consecutiveNull);
                }
                if (y > 0)
                {
                    _ = result.Append('/');
                }
            }

            _ = result.Append(CurrentTurnBlack ? " b" : " w");

            return result.ToString();
        }

        /// <summary>
        /// Convert a string representing a Go board to a go game instance.
        /// </summary>
        // TODO: Create new format to represent game as string
        public static GoGame FromBoardString(string boardString)
        {
            string[] fields = boardString.Split(' ');
            if (fields.Length != 3)
            {
                throw new FormatException("A valid board text state requires 3 fields separated by spaces");
            }

            string[] ranks = fields[0].Split('/');

            // TODO: Variable size
            int maxIndex = 18;

            bool?[,] board = new bool?[19, 19];
            for (int r = 0; r < ranks.Length; r++)
            {
                int fileIndex = 0;
                foreach (char stoneChar in ranks[r])
                {
                    switch (stoneChar)
                    {
                        case 'K':
                            board[fileIndex, maxIndex - r] = true;
                            break;
                        case 'G':
                            board[fileIndex, maxIndex - r] = true;
                            break;
                        case 'S':
                            board[fileIndex, maxIndex - r] = true;
                            break;
                        case 'R':
                            board[fileIndex, maxIndex - r] = true;
                            break;
                        case 'B':
                            board[fileIndex, maxIndex - r] = true;
                            break;
                        case 'N':
                            board[fileIndex, maxIndex - r] = true;
                            break;
                        case 'L':
                            board[fileIndex, maxIndex - r] = true;
                            break;
                        case 'P':
                            board[fileIndex, maxIndex - r] = true;
                            break;
                        case 'k':
                            board[fileIndex, maxIndex - r] = false;
                            break;
                        case 'g':
                            board[fileIndex, maxIndex - r] = false;
                            break;
                        case 's':
                            board[fileIndex, maxIndex - r] = false;
                            break;
                        case 'r':
                            board[fileIndex, maxIndex - r] = false;
                            break;
                        case 'b':
                            board[fileIndex, maxIndex - r] = false;
                            break;
                        case 'n':
                            board[fileIndex, maxIndex - r] = false;
                            break;
                        case 'l':
                            board[fileIndex, maxIndex - r] = false;
                            break;
                        case 'p':
                            board[fileIndex, maxIndex - r] = false;
                            break;
                        default:
                            if (stoneChar is > '0' and <= '9')
                            {
                                // char - '0' gets numeric value of ASCII number
                                // Leaves the specified number of squares as null
                                fileIndex += stoneChar - '0' - 1; 
                                // Subtract 1 as fileIndex gets incremented by 1 as well later
                            }
                            else
                            {
                                throw new FormatException($"{stoneChar} is not a valid stone character");
                            }
                            break;
                    }
                    fileIndex++;
                }
                // TODO: Check size is consistent
                if (false)
                {
                    throw new FormatException("Each rank in a board definition must contain definitions for 9 or 5 files");
                }
            }

            bool currentTurnBlack = fields[1] == "b" || (fields[1] == "w" ? false
                : throw new FormatException("Current turn specifier must be either w or b"));

            // Board string doesn't define what the previous moves were, so they moves list starts empty
            return new GoGame(board, currentTurnBlack, false,  // TODO: Format should contain field for whether game is over
                new List<Point>(), new List<string>(), 0, 0,  // TODO: Format should contain field for number of captures
                new HashSet<string>(), null, null);
        }
    }
}
