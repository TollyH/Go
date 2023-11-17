using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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

        /// <summary>
        /// Passes are represented by a move at (-1, -1)
        /// </summary>
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
            _ = PreviousBoards.Add(GetBoardString().ToString());

            InitialState = initialState ?? ToString();
            PreviousGameState = previousGameState;
        }

        /// <summary>
        /// Create a deep copy of all parameters to this go game
        /// </summary>
        public GoGame Clone(bool clonePreviousState = true)
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
                new HashSet<string>(PreviousBoards), InitialState,
                clonePreviousState ? PreviousGameState?.Clone() : PreviousGameState);
        }

        /// <summary>
        /// Remove fully surrounded stones from the board.
        /// </summary>
        /// <returns>The number of black and white stones removed respectively.</returns>
        public (int RemovedBlack, int RemovedWhite) RemoveSurroundedStones()
        {
            int removedBlack = 0;
            int removedWhite = 0;
            foreach (Point coord in BoardAnalysis.GetSurroundedStones(Board, CurrentTurnBlack))
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

            // Try making move to see if it results in suicide or repetition
            GoGame clone = Clone(clonePreviousState: false);
            _ = clone.PlaceStone(destination, forceMove: true, updateMoveText: false);
            bool suicide = BoardAnalysis.GetSurroundedStones(clone.Board, CurrentTurnBlack).Any(s => Board[s.X, s.Y] == CurrentTurnBlack);
            bool repetition = PreviousBoards.Contains(clone.GetBoardString().ToString());
            return !suicide && !repetition;
        }

        /// <summary>
        /// Pass the current turn to the next player without placing a stone.
        /// If two passes occur in a row, the game ends.
        /// </summary>
        /// <param name="updateMoveText">
        /// Whether the move should update the game move text and update <see cref="PreviousGameState"/>. This should usually be <see langword="true"/>,
        /// but may be set to <see langword="false"/> for performance optimisations in clone games for analysis.
        /// </param>
        /// <returns>Whether or not the pass caused the game to end.</returns>
        public bool PassTurn(bool updateMoveText = true)
        {
            if (updateMoveText)
            {
                // Used for move undoing
                PreviousGameState = Clone();
                string newMove = CurrentTurnBlack ? "BP" : "WP";
                MoveText.Add(newMove);
            }

            Moves.Add(new Point(-1, -1));
            CurrentTurnBlack = !CurrentTurnBlack;

            if (Moves.Count >= 2 && Moves[^2].X == -1)
            {
                GameOver = true;
                return true;
            }
            return false;
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
                string newMove = CurrentTurnBlack ? "B" : "W";
                newMove += $"{destination.X + 1}-{Board.GetLength(1) - destination.Y}";
                MoveText.Add(newMove);
            }
            Moves.Add(destination);

            Board[destination.X, destination.Y] = CurrentTurnBlack;

            (int removedBlack, int removedWhite) = RemoveSurroundedStones();
            BlackCaptures += removedWhite;
            WhiteCaptures += removedBlack;

            CurrentTurnBlack = !CurrentTurnBlack;
            _ = PreviousBoards.Add(GetBoardString().ToString());

            return true;
        }

        private StringBuilder GetBoardString()
        {
            StringBuilder result = new();

            for (int y = Board.GetLength(1) - 1; y >= 0; y--)
            {
                for (int x = 0; x < Board.GetLength(0); x++)
                {
                    bool? stone = Board[x, y];
                    _ = stone is null ? result.Append('n') : result.Append(stone.Value ? 'b' : 'w');
                }
                if (y > 0)
                {
                    _ = result.Append('/');
                }
            }

            return result;
        }

        /// <summary>
        /// Get a string representation of the given board.
        /// </summary>
        public override string ToString()
        {
            StringBuilder result = GetBoardString();

            _ = result.Append(' ').Append(BlackCaptures).Append('/').Append(WhiteCaptures)
                .Append(CurrentTurnBlack ? " b" : " w");

            if (GameOver)
            {
                _ = result.Append('!');
            }

            return result.ToString();
        }

        /// <summary>
        /// Convert a string representing a Go board to a go game instance.
        /// </summary>
        public static GoGame FromBoardString(string boardString)
        {
            string[] fields = boardString.Split(' ');
            if (fields.Length != 3)
            {
                throw new FormatException("A valid board text state requires 3 fields separated by spaces");
            }

            string[] ranks = fields[0].Split('/');
            int maxRankIndex = ranks.Length - 1;

            if (ranks.Length < 2)
            {
                throw new FormatException("A valid board must have at least 2 horizontal rows");
            }
            if (ranks[0].Length < 2)
            {
                throw new FormatException("A valid board must have at least 2 vertical columns");
            }

            bool?[,] board = new bool?[ranks[0].Length, ranks.Length];
            for (int r = 0; r < ranks.Length; r++)
            {
                int fileIndex = 0;
                foreach (char stoneChar in ranks[r])
                {
                    board[fileIndex, maxRankIndex - r] = stoneChar switch
                    {
                        'b' => true,
                        'w' => false,
                        'n' => null,
                        _ => throw new FormatException($"{stoneChar} is not a valid stone character"),
                    };
                    fileIndex++;
                }
                if (fileIndex != ranks[0].Length)
                {
                    throw new FormatException("Each row in the board must be the same length.");
                }
            }

            string[] captures = fields[1].Split('/');
            if (captures.Length != 2)
            {
                throw new FormatException("Captures field must contain two numbers separated by a slash");
            }
            if (!int.TryParse(captures[0], out int blackCaptures) || !int.TryParse(captures[1], out int whiteCaptures))
            {
                throw new FormatException("Captures field must contain two numbers separated by a slash");
            }

            if (fields[2].Length != 1 && (fields[2].Length != 2 || fields[2][1] != '!'))
            {
                throw new FormatException("Current turn specifier must be either w or b, optionally ending with an exclamation mark");
            }
            bool currentTurnBlack = fields[2][0] == 'b' || (fields[2][0] == 'w' ? false
                : throw new FormatException("Current turn specifier must be either w or b, optionally ending with an exclamation mark"));

            // Board string doesn't define what the previous moves were, so the moves list starts empty
            return new GoGame(board, currentTurnBlack, fields[2].Length == 2,
                new List<Point>(), new List<string>(), blackCaptures, whiteCaptures,
                new HashSet<string>(), null, null);
        }
    }
}
