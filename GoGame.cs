using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Go
{
    public sealed class GoGame
    {
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
        public int BlackCaptures { get; }
        public int WhiteCaptures { get; }

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

            // TODO: Go drop rules

            return true;
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

            // TODO: Capture check

            CurrentTurnBlack = !CurrentTurnBlack;
            _ = PreviousBoards.Add(ToString());

            if (updateMoveText)
            {
                string newMove = CurrentTurnBlack ? "B" : "W";
                newMove += $"{Board.GetLength(0) - destination.X}{Board.GetLength(1) - destination.Y}";
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
