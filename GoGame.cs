using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Go
{
    public class GoGame
    {
        /// <summary>
        /// <see langword="null"/> = no piece,
        /// <see langword="true"/> = black piece,
        /// <see langword="false"/> = white piece
        /// </summary>
        public bool?[,] Board { get; }
        public string InitialState { get; }

        public bool CurrentTurnBlack { get; private set; }
        public bool GameOver { get; private set; }

        /// <summary>
        /// A list of the moves made this game as
        /// (pieceLetter, sourcePosition, destinationPosition, promotionHappened, dropHappened)
        /// </summary>
        // TODO: Remove unneeded parameters
        public List<(string, Point, Point, bool, bool)> Moves { get; }
        public List<string> JapaneseMoveText { get; }
        public List<string> WesternMoveText { get; }
        public GoGame? PreviousGameState { get; private set; }
        // TODO: Replace with captured piece counts
        public Dictionary<Type, int> BlackPieceDrops { get; }
        public Dictionary<Type, int> WhitePieceDrops { get; }

        // Used to detect repetition
        public Dictionary<string, int> BoardCounts { get; }

        /// <summary>
        /// Create a new standard go game with all values at their defaults
        /// </summary>
        public GoGame(bool minigo)
        {
            CurrentTurnBlack = true;
            GameOver = false;

            Moves = new List<(string, Point, Point, bool, bool)>();
            // TODO: Rename to just MoveText
            WesternMoveText = new List<string>();

            BoardCounts = new Dictionary<string, int>();

            // TODO: Custom size boards
            Board = minigo ? new bool?[5, 5] : new bool?[9, 9];

            InitialState = ToString();
        }

        /// <summary>
        /// Create a new instance of a go game, setting each game parameter to a non-default value
        /// </summary>
        public GoGame(bool?[,] board, bool currentTurnBlack, bool gameOver,
            List<(string, Point, Point, bool, bool)> moves, List<string> japaneseMoveText,
            List<string> westernMoveText, Dictionary<Type, int>? blackPieceDrops,
            Dictionary<Type, int>? whitePieceDrops, Dictionary<string, int> boardCounts,
            string? initialState, GoGame? previousGameState)
        {
            if (board.GetLength(0) is not 9 and not 5 || board.GetLength(1) is not 9 and not 5)
            {
                throw new ArgumentException("Boards must be 9x9 or 5x5 in size");
            }

            Board = board;

            CurrentTurnBlack = currentTurnBlack;
            GameOver = gameOver;
            Moves = moves;
            JapaneseMoveText = japaneseMoveText;
            WesternMoveText = westernMoveText;
            BlackPieceDrops = blackPieceDrops ?? new Dictionary<Type, int>();
            WhitePieceDrops = whitePieceDrops ?? new Dictionary<Type, int>();
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
                new(WesternMoveText), new Dictionary<Type, int>(BlackPieceDrops),
                new Dictionary<Type, int>(WhitePieceDrops), new(BoardCounts), InitialState, PreviousGameState?.Clone());
        }

        /// <summary>
        /// Determine whether a drop of the given piece type to the given destination is valid or not.
        /// </summary>
        // TODO: Remove drop type, add suicide check
        public bool IsDropPossible(Type dropType, Point destination)
        {
            if (destination.X < 0 || destination.Y < 0
                || destination.X >= Board.GetLength(0) || destination.Y >= Board.GetLength(1))
            {
                return false;
            }
            if ((CurrentTurnBlack && BlackPieceDrops[dropType] == 0)
                || (!CurrentTurnBlack && WhitePieceDrops[dropType] == 0))
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
        /// Move a piece on the board from a <paramref name="source"/> coordinate to a <paramref name="destination"/> coordinate.
        /// To perform a piece drop, set <paramref name="source"/> to a value within <see cref="PieceDropSources"/>.
        /// </summary>
        /// <param name="doPromotion">
        /// If a piece can be promoted, should it be? <see langword="null"/> means the user should be prompted.
        /// </param>
        /// <param name="updateMoveText">
        /// Whether the move should update the game move text and update <see cref="PreviousGameState"/>. This should usually be <see langword="true"/>,
        /// but may be set to <see langword="false"/> for performance optimisations in clone games for analysis.
        /// </param>
        /// <returns><see langword="true"/> if the move was valid and executed, <see langword="false"/> otherwise</returns>
        /// <remarks>This method will check if the move is completely valid, unless <paramref name="forceMove"/> is <see langword="true"/>. No other validity checks are required.</remarks>
        // TODO: Replace with new drop piece method
        public bool MovePiece(Point source, Point destination, bool forceMove = false, bool updateMoveText = true)
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
            Moves.Add(("piece", source, destination, false, source.X == -1));

            Board[destination.X, destination.Y] = CurrentTurnBlack;
            if (source.X != -1)
            {
                Board[source.X, source.Y] = null;
            }

            CurrentTurnBlack = !CurrentTurnBlack;

            string newBoardString = ToString(true);
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
        /// <remarks>The resulting string complies with the Forsyth–Edwards Notation standard</remarks>
        public override string ToString()
        {
            return ToString(false);
        }

        /// <summary>
        /// Get a string representation of the given board.
        /// </summary>
        /// <remarks>
        /// The resulting string complies with the Forsyth–Edwards Notation standard,
        /// unless <paramref name="appendCheckStatus"/> is <see langword="true"/>
        /// </remarks>
        // TODO: Create new format to represent game as string
        public string ToString(bool appendCheckStatus)
        {
            StringBuilder result = new(90);  // TODO: Calculate max length of new format

            for (int y = Board.GetLength(1) - 1; y >= 0; y--)
            {
                int consecutiveNull = 0;
                for (int x = 0; x < Board.GetLength(0); x++)
                {
                    bool? piece = Board[x, y];
                    if (piece is null)
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
                        _ = result.Append(piece.Value ? 'b' : 'w');
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
        /// Convert Go Forsyth–Edwards Notation (SFEN) to a go game instance.
        /// </summary>
        // TODO: Create new format to represent game as string
        public static GoGame FromGoForsythEdwards(string forsythEdwards)
        {
            string[] fields = forsythEdwards.Split(' ');
            if (fields.Length != 3)
            {
                throw new FormatException("Go Forsyth–Edwards Notation requires 3 fields separated by spaces");
            }

            string[] ranks = fields[0].Split('/');
            if (ranks.Length is not 9 and not 5)
            {
                throw new FormatException("Board definitions must have 9 or 5 ranks separated by a forward slash");
            }

            bool minigo = ranks.Length == 5;
            int maxIndex = minigo ? 4 : 8;

            bool?[,] board = minigo ? new bool?[5, 5] : new bool?[9, 9];
            for (int r = 0; r < ranks.Length; r++)
            {
                int fileIndex = 0;
                foreach (char pieceChar in ranks[r])
                {
                    switch (pieceChar)
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
                            if (pieceChar is > '0' and <= '9')
                            {
                                // char - '0' gets numeric value of ASCII number
                                // Leaves the specified number of squares as null
                                fileIndex += pieceChar - '0' - 1; 
                                // Subtract 1 as fileIndex gets incremented by 1 as well later
                            }
                            else
                            {
                                throw new FormatException($"{pieceChar} is not a valid piece character");
                            }
                            break;
                    }
                    fileIndex++;
                }
                if ((fileIndex != 9 && !minigo) || (fileIndex != 5 && minigo))
                {
                    throw new FormatException("Each rank in a board definition must contain definitions for 9 or 5 files");
                }
            }

            bool currentTurnBlack = fields[1] == "b" || (fields[1] == "w" ? false
                : throw new FormatException("Current turn specifier must be either w or b"));

            Dictionary<Type, int> blackPieceDrops = new();
            Dictionary<Type, int> whitePieceDrops = new();

            // Go Forsyth–Edwards doesn't define what the previous moves were, so they moves list starts empty
            return new GoGame(board, currentTurnBlack, false,  // TODO: Format should contain field for whether game is over
                new(), new(), new(), blackPieceDrops, whitePieceDrops, new(), null, null);
        }
    }
}
