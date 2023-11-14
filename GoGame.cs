using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Go
{
    public class GoGame
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

        /// <summary>
        /// A list of the moves made this game as
        /// (stoneLetter, sourcePosition, destinationPosition, promotionHappened, dropHappened)
        /// </summary>
        // TODO: Remove unneeded parameters
        public List<(string, Point, Point, bool, bool)> Moves { get; }
        public List<string> JapaneseMoveText { get; }
        public List<string> WesternMoveText { get; }
        public GoGame? PreviousGameState { get; private set; }
        // TODO: Replace with captured stone counts
        public Dictionary<Type, int> BlackStoneDrops { get; }
        public Dictionary<Type, int> WhiteStoneDrops { get; }

        // Used to detect repetition
        public Dictionary<string, int> BoardCounts { get; }

        /// <summary>
        /// Create a new standard go game with all values at their defaults
        /// </summary>
        public GoGame(int boardWidth, int boardHeight)
        {
            CurrentTurnBlack = true;
            GameOver = false;

            Moves = new List<(string, Point, Point, bool, bool)>();
            // TODO: Rename to just MoveText
            WesternMoveText = new List<string>();

            BoardCounts = new Dictionary<string, int>();

            Board = new bool?[boardWidth, boardHeight];

            InitialState = ToString();
        }

        /// <summary>
        /// Create a new instance of a go game, setting each game parameter to a non-default value
        /// </summary>
        public GoGame(bool?[,] board, bool currentTurnBlack, bool gameOver,
            List<(string, Point, Point, bool, bool)> moves, List<string> japaneseMoveText,
            List<string> westernMoveText, Dictionary<Type, int>? blackStoneDrops,
            Dictionary<Type, int>? whiteStoneDrops, Dictionary<string, int> boardCounts,
            string? initialState, GoGame? previousGameState)
        {
            Board = board;

            CurrentTurnBlack = currentTurnBlack;
            GameOver = gameOver;
            Moves = moves;
            JapaneseMoveText = japaneseMoveText;
            WesternMoveText = westernMoveText;
            BlackStoneDrops = blackStoneDrops ?? new Dictionary<Type, int>();
            WhiteStoneDrops = whiteStoneDrops ?? new Dictionary<Type, int>();
            BoardCounts = boardCounts;

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

            return new GoGame(boardClone, CurrentTurnBlack, GameOver, new(Moves), new(JapaneseMoveText),
                new(WesternMoveText), new Dictionary<Type, int>(BlackStoneDrops),
                new Dictionary<Type, int>(WhiteStoneDrops), new(BoardCounts), InitialState, PreviousGameState?.Clone());
        }

        /// <summary>
        /// Determine whether a drop of the given stone type to the given destination is valid or not.
        /// </summary>
        // TODO: Remove drop type, add suicide check
        public bool IsDropPossible(Type dropType, Point destination)
        {
            if (destination.X < 0 || destination.Y < 0
                || destination.X >= Board.GetLength(0) || destination.Y >= Board.GetLength(1))
            {
                return false;
            }
            if ((CurrentTurnBlack && BlackStoneDrops[dropType] == 0)
                || (!CurrentTurnBlack && WhiteStoneDrops[dropType] == 0))
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
        /// Move a stone on the board from a <paramref name="source"/> coordinate to a <paramref name="destination"/> coordinate.
        /// To perform a stone drop, set <paramref name="source"/> to a value within <see cref="StoneDropSources"/>.
        /// </summary>
        /// <param name="doPromotion">
        /// If a stone can be promoted, should it be? <see langword="null"/> means the user should be prompted.
        /// </param>
        /// <param name="updateMoveText">
        /// Whether the move should update the game move text and update <see cref="PreviousGameState"/>. This should usually be <see langword="true"/>,
        /// but may be set to <see langword="false"/> for performance optimisations in clone games for analysis.
        /// </param>
        /// <returns><see langword="true"/> if the move was valid and executed, <see langword="false"/> otherwise</returns>
        /// <remarks>This method will check if the move is completely valid, unless <paramref name="forceMove"/> is <see langword="true"/>. No other validity checks are required.</remarks>
        // TODO: Replace with new drop stone method
        public bool MoveStone(Point source, Point destination, bool forceMove = false, bool updateMoveText = true)
        {
            if (!forceMove && (GameOver || !IsDropPossible(null, destination)))
            {
                return false;
            }

            // Used for generating new move text and move undoing
            GoGame? oldGame = null;
            if (updateMoveText)
            {
                oldGame = Clone();
                PreviousGameState = oldGame;
            }
            Moves.Add(("stone", source, destination, false, source.X == -1));

            Board[destination.X, destination.Y] = CurrentTurnBlack;
            if (source.X != -1)
            {
                Board[source.X, source.Y] = null;
            }

            CurrentTurnBlack = !CurrentTurnBlack;

            string newBoardString = ToString();
            if (BoardCounts.ContainsKey(newBoardString))
            {
                BoardCounts[newBoardString]++;
            }
            else
            {
                BoardCounts[newBoardString] = 1;
            }

            if (updateMoveText)
            {
                string newWesternMove = CurrentTurnBlack ? "B" : "W";

                newWesternMove += source.X == -1 ? '*'
                    : oldGame.Board[destination.X, destination.Y] is not null ? 'x'
                    : '-';
                newWesternMove += $"{Board.GetLength(0) - destination.X}{Board.GetLength(1) - destination.Y}";

                WesternMoveText.Add(newWesternMove);
            }

            return true;
        }

        /// <summary>
        /// Get a string representation of the given board.
        /// </summary>
        // TODO: Create new format to represent game as string
        public string ToString()
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

            Dictionary<Type, int> blackStoneDrops = new();
            Dictionary<Type, int> whiteStoneDrops = new();

            // Board string doesn't define what the previous moves were, so they moves list starts empty
            return new GoGame(board, currentTurnBlack, false,  // TODO: Format should contain field for whether game is over
                new(), new(), new(), blackStoneDrops, whiteStoneDrops, new(), null, null);
        }
    }
}
