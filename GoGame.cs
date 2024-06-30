using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Go
{
    public enum ScoringSystem
    {
        Area,
        Territory,
        Stone
    }

    public sealed class GoGame
    {
        /// <summary>
        /// <see langword="null"/> = no stone,
        /// <see langword="int"/> = player index (e.g. 0 = black, 1 = white)
        /// </summary>
        public int?[,] Board { get; internal set; }
        public string InitialState { get; }
        public int PlayerCount { get; }

        public int CurrentTurn { get; private set; }
        public bool GameOver { get; set; }
        public bool AwaitingDeadStoneRemoval { get; set; }

        public ScoringSystem CurrentScoring { get; }

        /// <summary>
        /// Passes are represented by a move at (-1, -1)
        /// </summary>
        public List<Point> Moves { get; }
        public List<string> MoveText { get; }
        public GoGame? PreviousGameState { get; private set; }
        /// <summary>
        /// The number of captures each player has made (i.e. the number of other stones removed)
        /// </summary>
        public int[] Captures { get; set; }

        // Used to detect repetition
        public HashSet<string> PreviousBoards { get; }

        /// <summary>
        /// Create a new standard go game with all values at their defaults
        /// </summary>
        public GoGame(int boardWidth, int boardHeight, ScoringSystem scoring, int playerCount = 2)
        {
            if (playerCount is < 2 or > 10)
            {
                throw new ArgumentException("There must be between 2 and 10 players in a game.");
            }

            PlayerCount = playerCount;

            CurrentTurn = 0;
            GameOver = false;
            AwaitingDeadStoneRemoval = false;
            CurrentScoring = scoring;

            Moves = new List<Point>();
            MoveText = new List<string>();

            PreviousBoards = new HashSet<string>();

            Board = new int?[boardWidth, boardHeight];

            Captures = new int[playerCount];

            InitialState = ToString();
        }

        /// <summary>
        /// Create a new instance of a go game, setting each game parameter to a non-default value
        /// </summary>
        internal GoGame(int?[,] board, int currentTurn, bool gameOver, bool awaitingDeadStoneRemoval,
            ScoringSystem scoring, List<Point> moves, List<string> moveText, int[] captures, HashSet<string> previousBoards,
            string? initialState, GoGame? previousGameState, int playerCount)
        {
            PlayerCount = playerCount;

            Board = board;

            CurrentTurn = currentTurn;
            GameOver = gameOver;
            AwaitingDeadStoneRemoval = awaitingDeadStoneRemoval;
            CurrentScoring = scoring;
            Moves = moves;
            MoveText = moveText;
            Captures = captures;
            PreviousBoards = previousBoards;
            _ = PreviousBoards.Add(GetBoardString().ToString());

            InitialState = initialState ?? ToString();
            PreviousGameState = previousGameState;
        }

        /// <summary>
        /// Create a deep copy of all parameters to this go game
        /// </summary>
        public GoGame Clone(bool clonePreviousState)
        {
            return new GoGame(Board.TwoDimensionalClone(), CurrentTurn, GameOver, AwaitingDeadStoneRemoval, CurrentScoring,
                new List<Point>(Moves), new List<string>(MoveText), Captures.ToArray(),
                new HashSet<string>(PreviousBoards), InitialState,
                clonePreviousState ? PreviousGameState?.Clone(true) : PreviousGameState, PlayerCount);
        }

        /// <summary>
        /// Remove fully surrounded stones from the board.
        /// </summary>
        /// <returns>The number of stones removed.</returns>
        public int RemoveSurroundedStones()
        {
            int removed = 0;
            foreach (Point coord in BoardAnalysis.GetSurroundedStones(Board, CurrentTurn))
            {
                int? stone = Board[coord.X, coord.Y];
                if (stone is not null)
                {
                    removed++;
                }
                Board[coord.X, coord.Y] = null;
            }
            return removed;
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
            bool suicide = clone.Board.OfType<int>().Count(p => p == CurrentTurn) < Board.OfType<int>().Count(p => p == CurrentTurn);
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
                PreviousGameState = Clone(true);
                string newMove = $"{CurrentTurn + 1}:P";
                MoveText.Add(newMove);
            }

            Moves.Add(new Point(-1, -1));
            CurrentTurn = ++CurrentTurn % PlayerCount;

            if (Moves.Count >= PlayerCount && Moves.TakeLast(PlayerCount).All(m => m.X == -1))
            {
                GameOver = true;
                AwaitingDeadStoneRemoval = true;
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
                PreviousGameState = Clone(true);
                MoveText.Add($"{CurrentTurn + 1}:{destination.X + 1}-{Board.GetLength(1) - destination.Y}");
            }
            Moves.Add(destination);

            Board[destination.X, destination.Y] = CurrentTurn;

            Captures[CurrentTurn] += RemoveSurroundedStones();

            CurrentTurn = ++CurrentTurn % PlayerCount;
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
                    int? stone = Board[x, y];
                    _ = stone is null ? result.Append('n') : result.Append(stone.Value);
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

            char scoring = CurrentScoring switch
            {
                ScoringSystem.Area => 'a',
                ScoringSystem.Territory => 't',
                ScoringSystem.Stone => 's',
                _ => 'a'
            };

            _ = result.Append(' ').Append(string.Join('/', Captures))
                .Append(' ').Append(CurrentTurn)
                .Append(' ').Append(scoring);

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
            if (fields.Length != 4)
            {
                throw new FormatException("A valid board text state requires 4 fields separated by spaces");
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

            int?[,] board = new int?[ranks[0].Length, ranks.Length];
            for (int r = 0; r < ranks.Length; r++)
            {
                int fileIndex = 0;
                foreach (char stoneChar in ranks[r])
                {
                    if (stoneChar == 'n')
                    {
                        board[fileIndex, maxRankIndex - r] = null;
                    }
                    else
                    {
                        if (stoneChar is < '0' or > '9')
                        {
                            throw new FormatException("Each character in the board must be a digit between 0 and 9 or the character 'n'");
                        }
                        // Convert ASCII digit character to number
                        board[fileIndex, maxRankIndex - r] = stoneChar - '0';
                    }
                    fileIndex++;
                }
                if (fileIndex != ranks[0].Length)
                {
                    throw new FormatException("Each row in the board must be the same length.");
                }
            }

            string[] captureStrings = fields[1].Split('/');
            int[] captures = new int[captureStrings.Length];
            for (int i = 0; i < captureStrings.Length; i++)
            {
                if (!int.TryParse(captureStrings[i], out int removed))
                {
                    throw new FormatException("Captures field must contain numbers separated by a slashes");
                }
                captures[i] = removed;
            }

            if (fields[2].Length != 1 && (fields[2].Length != 2 || fields[2][1] != '!'))
            {
                throw new FormatException("Current turn specifier must be a digit between 0 and 9, optionally ending with an exclamation mark");
            }
            if (fields[2][0] is < '0' or > '9')
            {
                throw new FormatException("Current turn specifier must be a digit between 0 and 9, optionally ending with an exclamation mark");
            }
            // Convert ASCII digit character to number
            int currentTurn = fields[2][0] - '0';

            ScoringSystem scoring = fields[3] switch
            {
                "a" => ScoringSystem.Area,
                "t" => ScoringSystem.Territory,
                "s" => ScoringSystem.Stone,
                _ => throw new FormatException("Scoring system specifier must be 'a', 't', or 's'")
            };

            // Board string doesn't define what the previous moves were, so the moves list starts empty
            return new GoGame(board, currentTurn, fields[2].Length == 2, false, scoring,
                new List<Point>(), new List<string>(), captures,
                new HashSet<string>(), null, null, captures.Length);
        }
    }
}
