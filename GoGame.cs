﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Go
{
    /// <remarks>
    /// CheckBlack and CheckMateBlack mean that the check is against black,
    /// or that black has lost respectively, and vice versa.
    /// </remarks>
    public enum GameState
    {
        StandardPlay,
        DrawRepetition,
        CheckBlack,
        CheckWhite,
        PerpetualCheckBlack,
        PerpetualCheckWhite,
        StalemateBlack,
        StalemateWhite,
        CheckMateBlack,
        CheckMateWhite
    }

    public class GoGame
    {
        public static readonly ImmutableHashSet<GameState> EndingStates = new HashSet<GameState>()
        {
            GameState.DrawRepetition,
            GameState.PerpetualCheckBlack,
            GameState.PerpetualCheckWhite,
            GameState.StalemateBlack,
            GameState.StalemateWhite,
            GameState.CheckMateBlack,
            GameState.CheckMateWhite
        }.ToImmutableHashSet();

        /// <summary>
        /// Used to give to <see cref="MovePiece"/> as the source position to declare that a piece drop should occur
        /// </summary>
        public static readonly Dictionary<Type, Point> PieceDropSources = new()
        {
            { typeof(Pieces.GoldGeneral), new Point(-1, 0) },
            { typeof(Pieces.SilverGeneral), new Point(-1, 1) },
            { typeof(Pieces.Rook), new Point(-1, 2) },
            { typeof(Pieces.Bishop), new Point(-1, 3) },
            { typeof(Pieces.Knight), new Point(-1, 4) },
            { typeof(Pieces.Lance), new Point(-1, 5) },
            { typeof(Pieces.Pawn), new Point(-1, 6) },
        };
        public static readonly Type[] DropTypeOrder = new Type[7]
        {
            typeof(Pieces.GoldGeneral), typeof(Pieces.SilverGeneral), typeof(Pieces.Rook),
            typeof(Pieces.Bishop), typeof(Pieces.Knight), typeof(Pieces.Lance), typeof(Pieces.Pawn)
        };

        public Pieces.Piece?[,] Board { get; }
        public string InitialState { get; }

        public int PromotionZoneBlackStart { get; }
        public int PromotionZoneWhiteStart { get; }

        public Pieces.King BlackKing { get; }
        public Pieces.King WhiteKing { get; }

        public bool CurrentTurnBlack { get; private set; }
        public bool GameOver { get; private set; }
        public bool AwaitingPromotionResponse { get; private set; }

        /// <summary>
        /// A list of the moves made this game as
        /// (pieceLetter, sourcePosition, destinationPosition, promotionHappened, dropHappened)
        /// </summary>
        public List<(string, Point, Point, bool, bool)> Moves { get; }
        public List<string> JapaneseMoveText { get; }
        public List<string> WesternMoveText { get; }
        public GoGame? PreviousGameState { get; private set; }
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
            AwaitingPromotionResponse = false;

            BlackKing = new Pieces.King(new Point(minigo ? 0 : 4, 0), true);
            WhiteKing = new Pieces.King(new Point(4, minigo ? 4 : 8), false);

            Moves = new List<(string, Point, Point, bool, bool)>();
            JapaneseMoveText = new List<string>();
            WesternMoveText = new List<string>();
            BlackPieceDrops = new Dictionary<Type, int>()
            {
                { typeof(Pieces.GoldGeneral), 0 },
                { typeof(Pieces.SilverGeneral), 0 },
                { typeof(Pieces.Rook), 0 },
                { typeof(Pieces.Bishop), 0 },
                { typeof(Pieces.Knight), 0 },
                { typeof(Pieces.Lance), 0 },
                { typeof(Pieces.Pawn), 0 },
            };
            WhitePieceDrops = new Dictionary<Type, int>()
            {
                { typeof(Pieces.GoldGeneral), 0 },
                { typeof(Pieces.SilverGeneral), 0 },
                { typeof(Pieces.Rook), 0 },
                { typeof(Pieces.Bishop), 0 },
                { typeof(Pieces.Knight), 0 },
                { typeof(Pieces.Lance), 0 },
                { typeof(Pieces.Pawn), 0 },
            };

            BoardCounts = new Dictionary<string, int>();

            Board = minigo
            ? new Pieces.Piece?[5, 5]
                {
                    { BlackKing, new Pieces.Pawn(new Point(0, 1), true), null, null, new Pieces.Rook(new Point(0, 4), false) },
                    { new Pieces.GoldGeneral(new Point(1, 0), true), null, null, null, new Pieces.Bishop(new Point(1, 4), false) },
                    { new Pieces.SilverGeneral(new Point(2, 0), true), null, null, null, new Pieces.SilverGeneral(new Point(2, 4), false) },
                    { new Pieces.Bishop(new Point(3, 0), true), null, null, null, new Pieces.GoldGeneral(new Point(3, 4), false) },
                    { new Pieces.Rook(new Point(4, 0), true), null, null, new Pieces.Pawn(new Point(4, 3), false), WhiteKing }
                }
            : new Pieces.Piece?[9, 9]
                {
                    { new Pieces.Lance(new Point(0, 0), true), null, new Pieces.Pawn(new Point(0, 2), true), null, null, null, new Pieces.Pawn(new Point(0, 6), false), null, new Pieces.Lance(new Point(0, 8), false) },
                    { new Pieces.Knight(new Point(1, 0), true), new Pieces.Bishop(new Point(1, 1), true), new Pieces.Pawn(new Point(1, 2), true), null, null, null, new Pieces.Pawn(new Point(1, 6), false), new Pieces.Rook(new Point(1, 7), false), new Pieces.Knight(new Point(1, 8), false) },
                    { new Pieces.SilverGeneral(new Point(2, 0), true), null, new Pieces.Pawn(new Point(2, 2), true), null, null, null, new Pieces.Pawn(new Point(2, 6), false), null, new Pieces.SilverGeneral(new Point(2, 8), false) },
                    { new Pieces.GoldGeneral(new Point(3, 0), true), null, new Pieces.Pawn(new Point(3, 2), true), null, null, null, new Pieces.Pawn(new Point(3, 6), false), null, new Pieces.GoldGeneral(new Point(3, 8), false) },
                    { BlackKing, null, new Pieces.Pawn(new Point(4, 2), true), null, null, null, new Pieces.Pawn(new Point(4, 6), false), null, WhiteKing },
                    { new Pieces.GoldGeneral(new Point(5, 0), true), null, new Pieces.Pawn(new Point(5, 2), true), null, null, null, new Pieces.Pawn(new Point(5, 6), false), null, new Pieces.GoldGeneral(new Point(5, 8), false) },
                    { new Pieces.SilverGeneral(new Point(6, 0), true), null, new Pieces.Pawn(new Point(6, 2), true), null, null, null, new Pieces.Pawn(new Point(6, 6), false), null, new Pieces.SilverGeneral(new Point(6, 8), false) },
                    { new Pieces.Knight(new Point(7, 0), true), new Pieces.Rook(new Point(7, 1), true), new Pieces.Pawn(new Point(7, 2), true), null, null, null, new Pieces.Pawn(new Point(7, 6), false), new Pieces.Bishop(new Point(7, 7), false), new Pieces.Knight(new Point(7, 8), false) },
                    { new Pieces.Lance(new Point(8, 0), true), null, new Pieces.Pawn(new Point(8, 2), true), null, null, null, new Pieces.Pawn(new Point(8, 6), false), null, new Pieces.Lance(new Point(8, 8), false) }
                };
            PromotionZoneBlackStart = minigo ? 4 : 6;
            PromotionZoneWhiteStart = minigo ? 0 : 2;

            InitialState = ToString();
        }

        /// <summary>
        /// Create a new instance of a go game, setting each game parameter to a non-default value
        /// </summary>
        public GoGame(Pieces.Piece?[,] board, bool currentTurnBlack, bool gameOver,
            List<(string, Point, Point, bool, bool)> moves, List<string> japaneseMoveText,
            List<string> westernMoveText, Dictionary<Type, int>? blackPieceDrops,
            Dictionary<Type, int>? whitePieceDrops, Dictionary<string, int> boardCounts,
            string? initialState, GoGame? previousGameState)
        {
            if (board.GetLength(0) is not 9 and not 5 || board.GetLength(1) is not 9 and not 5)
            {
                throw new ArgumentException("Boards must be 9x9 or 5x5 in size");
            }

            bool minigo = board.GetLength(0) == 5;

            Board = board;
            PromotionZoneBlackStart = minigo ? 4 : 6;
            PromotionZoneWhiteStart = minigo ? 0 : 2;
            BlackKing = Board.OfType<Pieces.King>().Where(k => k.IsBlack).First();
            WhiteKing = Board.OfType<Pieces.King>().Where(k => !k.IsBlack).First();

            CurrentTurnBlack = currentTurnBlack;
            GameOver = gameOver;
            Moves = moves;
            JapaneseMoveText = japaneseMoveText;
            WesternMoveText = westernMoveText;
            BlackPieceDrops = blackPieceDrops ?? new Dictionary<Type, int>()
            {
                { typeof(Pieces.GoldGeneral), 0 },
                { typeof(Pieces.SilverGeneral), 0 },
                { typeof(Pieces.Rook), 0 },
                { typeof(Pieces.Bishop), 0 },
                { typeof(Pieces.Knight), 0 },
                { typeof(Pieces.Lance), 0 },
                { typeof(Pieces.Pawn), 0 },
            };
            WhitePieceDrops = whitePieceDrops ?? new Dictionary<Type, int>()
            {
                { typeof(Pieces.GoldGeneral), 0 },
                { typeof(Pieces.SilverGeneral), 0 },
                { typeof(Pieces.Rook), 0 },
                { typeof(Pieces.Bishop), 0 },
                { typeof(Pieces.Knight), 0 },
                { typeof(Pieces.Lance), 0 },
                { typeof(Pieces.Pawn), 0 },
            };
            BoardCounts = boardCounts;

            InitialState = initialState ?? ToString();
            PreviousGameState = previousGameState;
        }

        /// <summary>
        /// Create a deep copy of all parameters to this go game
        /// </summary>
        public GoGame Clone()
        {
            Pieces.Piece?[,] boardClone = new Pieces.Piece?[Board.GetLength(0), Board.GetLength(1)];
            for (int x = 0; x < boardClone.GetLength(0); x++)
            {
                for (int y = 0; y < boardClone.GetLength(1); y++)
                {
                    boardClone[x, y] = Board[x, y]?.Clone();
                }
            }

            return new GoGame(boardClone, CurrentTurnBlack, GameOver, new(Moves), new(JapaneseMoveText),
                new(WesternMoveText), new Dictionary<Type, int>(BlackPieceDrops),
                new Dictionary<Type, int>(WhitePieceDrops), new(BoardCounts), InitialState, PreviousGameState?.Clone());
        }

        /// <summary>
        /// Determine the current state of the game.
        /// </summary>
        /// <remarks>
        /// This method is similar to <see cref="BoardAnalysis.DetermineGameState"/>, however it can also detect repetition.
        /// </remarks>
        public GameState DetermineGameState(bool includeRepetition = true)
        {
            GameState staticState = BoardAnalysis.DetermineGameState(Board, CurrentTurnBlack,
                BlackKing.Position, WhiteKing.Position);
            if (EndingStates.Contains(staticState))
            {
                bool endAvoidableWithDrop = false;
                foreach ((Type dropType, int count) in CurrentTurnBlack ? BlackPieceDrops : WhitePieceDrops)
                {
                    if (count > 0)
                    {
                        for (int x = 0; x < Board.GetLength(0); x++)
                        {
                            for (int y = 0; y < Board.GetLength(1); y++)
                            {
                                Point pt = new(x, y);
                                if (IsDropPossible(dropType, pt))
                                {
                                    endAvoidableWithDrop = true;
                                    break;
                                }
                            }
                            if (endAvoidableWithDrop)
                            {
                                break;
                            }
                        }
                    }
                    if (endAvoidableWithDrop)
                    {
                        break;
                    }
                }
                if (!endAvoidableWithDrop)
                {
                    return staticState;
                }
                else
                {
                    staticState = staticState is GameState.CheckMateBlack or GameState.StalemateBlack
                        ? GameState.CheckBlack : GameState.CheckWhite;
                }
            }
            if (includeRepetition && BoardCounts.GetValueOrDefault(ToString(true)) >= 4)
            {
                if (ToString(true)[^1] == '!')
                {
                    return CurrentTurnBlack ? GameState.PerpetualCheckBlack : GameState.PerpetualCheckWhite;
                }
                return GameState.DrawRepetition;
            }
            return staticState;
        }

        /// <summary>
        /// Determine whether a drop of the given piece type to the given destination is valid or not.
        /// </summary>
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
            if (((dropType == typeof(Pieces.Pawn) || dropType == typeof(Pieces.Lance))
                    && (destination.Y == (CurrentTurnBlack ? Board.GetLength(1) - 1 : 0)))
                || (dropType == typeof(Pieces.Knight)
                    && (CurrentTurnBlack ? destination.Y >= Board.GetLength(1) - 2 : destination.Y <= 1)))
            {
                return false;
            }

            GoGame checkmateTest = Clone();
            _ = checkmateTest.MovePiece(new Point(-1, Array.IndexOf(DropTypeOrder, dropType)),
                destination, forceMove: true, updateMoveText: false, determineGameState: false);
            GameState resultingGameState = BoardAnalysis.DetermineGameState(checkmateTest.Board, checkmateTest.CurrentTurnBlack,
                checkmateTest.BlackKing.Position, checkmateTest.WhiteKing.Position);

            if ((CurrentTurnBlack && BoardAnalysis.IsKingReachable(checkmateTest.Board,
                    true, checkmateTest.BlackKing.Position))
                || (!CurrentTurnBlack && BoardAnalysis.IsKingReachable(checkmateTest.Board,
                    false, checkmateTest.WhiteKing.Position)))
            {
                return false;
            }

            bool pawnPresentOnFile = false;
            for (int y = 0; y < Board.GetLength(1); y++)
            {
                if (Board[destination.X, y] is Pieces.Pawn
                    && Board[destination.X, y]!.IsBlack == CurrentTurnBlack)
                {
                    pawnPresentOnFile = true;
                    break;
                }
            }

            if (dropType == typeof(Pieces.Pawn) && (pawnPresentOnFile
                || resultingGameState is GameState.CheckMateBlack or GameState.CheckMateWhite))
            {
                return false;
            }

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
        public bool MovePiece(Point source, Point destination, bool forceMove = false, bool? doPromotion = null, bool updateMoveText = true,
            bool determineGameState = true)
        {
            if (!forceMove && GameOver)
            {
                return false;
            }

            Pieces.Piece? piece;
            if (source.X == -1)
            {
                // Piece drop
                Type dropType = DropTypeOrder[source.Y];
                piece = (Pieces.Piece)Activator.CreateInstance(dropType, destination, CurrentTurnBlack)!;
                if (!forceMove && !IsDropPossible(dropType, destination))
                {
                    return false;
                }
                if (CurrentTurnBlack && BlackPieceDrops[dropType] > 0)
                {
                    BlackPieceDrops[dropType]--;
                }
                else if (WhitePieceDrops[dropType] > 0)
                {
                    WhitePieceDrops[dropType]--;
                }
            }
            else
            {
                piece = Board[source.X, source.Y];
                if (piece is null)
                {
                    return false;
                }
                if (!forceMove && piece.IsBlack != CurrentTurnBlack)
                {
                    return false;
                }
            }

            // Used for generating new move text and move undoing
            GoGame? oldGame = null;
            if (updateMoveText)
            {
                oldGame = Clone();
                PreviousGameState = oldGame;
            }

            bool pieceMoved = piece.Move(Board, destination, forceMove || source.X == -1);

            if (pieceMoved)
            {
                Moves.Add((piece.SymbolLetter, source, destination, false, source.X == -1));
                if (Board[destination.X, destination.Y] is not null)
                {
                    Type targetPiece = Board[destination.X, destination.Y]!.GetType();
                    if (Pieces.Piece.DemotionMap.ContainsKey(targetPiece))
                    {
                        targetPiece = Pieces.Piece.DemotionMap[targetPiece];
                    }
                    if (BlackPieceDrops.ContainsKey(targetPiece))
                    {
                        if (CurrentTurnBlack)
                        {
                            BlackPieceDrops[targetPiece]++;
                        }
                        else
                        {
                            WhitePieceDrops[targetPiece]++;
                        }
                    }
                }

                bool promotionPossible = false;
                bool promotionHappened = false;
                Pieces.Piece beforePromotion = piece;
                Type pieceType = piece.GetType();
                if (source.X != -1 && Pieces.Piece.PromotionMap.ContainsKey(pieceType))
                {
                    if ((piece.IsBlack ? destination.Y >= PromotionZoneBlackStart : destination.Y <= PromotionZoneWhiteStart)
                        || (piece.IsBlack ? source.Y >= PromotionZoneBlackStart : source.Y <= PromotionZoneWhiteStart))
                    {
                        promotionPossible = true;
                        if ((piece is Pieces.Pawn or Pieces.Lance && (destination.Y == (piece.IsBlack ? Board.GetLength(1) - 1 : 0)))
                            || (piece is Pieces.Knight && (piece.IsBlack ? destination.Y >= Board.GetLength(1) - 2 : destination.Y <= 1)))
                        {
                            // Always promote pawns and lances upon reaching the last rank
                            // Always promote knights upon reaching the last two ranks
                            doPromotion = true;
                        }
                        AwaitingPromotionResponse = true;
                        doPromotion ??= System.Windows.MessageBox.Show(
                            $"Do you want to promote the {piece.Name} you just moved?", "Promotion",
                            System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question
                        ) == System.Windows.MessageBoxResult.Yes;
                        AwaitingPromotionResponse = false;
                        if (doPromotion.Value)
                        {
                            promotionHappened = true;
                            Moves[^1] = (Moves[^1].Item1, source, destination, true, false);
                            piece = (Pieces.Piece)Activator.CreateInstance(Pieces.Piece.PromotionMap[pieceType], piece.Position, piece.IsBlack)!;
                        }
                        Board[source.X, source.Y] = piece;
                    }
                }

                Board[destination.X, destination.Y] = piece;
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
                if (determineGameState)
                {
                    GameOver = EndingStates.Contains(DetermineGameState());
                }

                if (updateMoveText)
                {
                    string newJapaneseMove = (CurrentTurnBlack ? "☖" : "☗")
                        + (Moves.Count > 1 && destination == Moves[^2].Item3 ? "同　" : destination.ToGoCoordinate(Board.GetLength(0) == 5))
                        + beforePromotion.SymbolLetter;
                    string newWesternMove = beforePromotion.SFENLetter;

                    // Disambiguate moving piece if two pieces of the same type can reach destination
                    IEnumerable<Pieces.Piece> canReachDest = oldGame!.Board.OfType<Pieces.Piece>().Where(
                        p => beforePromotion.GetType() == p.GetType() && p.Position != source && p.IsBlack == beforePromotion.IsBlack
                            && p.GetValidMoves(oldGame.Board, true).Contains(destination));
                    if (canReachDest.Any())
                    {
                        newWesternMove += $"{Board.GetLength(0) - source.X}{Board.GetLength(1) - source.Y}";
                        if (source.X == -1)
                        {
                            newJapaneseMove += '打';
                        }
                        else if (destination.Y > source.Y && !canReachDest.Where(p => destination.Y > p.Position.Y).Any())
                        {
                            newJapaneseMove += CurrentTurnBlack ? '引' : '上';
                        }
                        else if (destination.Y < source.Y && !canReachDest.Where(p => destination.Y < p.Position.Y).Any())
                        {
                            newJapaneseMove += CurrentTurnBlack ? '上' : '引';
                        }
                        else if (destination.Y == source.Y && !canReachDest.Where(p => destination.Y == p.Position.Y).Any())
                        {
                            newJapaneseMove += '寄';
                        }
                        else if (destination.X > source.X && !canReachDest.Where(p => destination.X > p.Position.X).Any())
                        {
                            newJapaneseMove += CurrentTurnBlack ? "右" : "左";
                        }
                        else if (destination.X < source.X && !canReachDest.Where(p => destination.X < p.Position.X).Any())
                        {
                            newJapaneseMove += CurrentTurnBlack ? "左" : "右";
                        }
                        else
                        {
                            newJapaneseMove += "直";
                        }
                    }

                    newWesternMove += source.X == -1 ? '*'
                        : oldGame.Board[destination.X, destination.Y] is not null ? 'x'
                        : '-';
                    newWesternMove += $"{Board.GetLength(0) - destination.X}{Board.GetLength(1) - destination.Y}";

                    if (promotionPossible)
                    {
                        newJapaneseMove += promotionHappened ? "成" : "不成";
                        newWesternMove += promotionHappened ? '+' : '=';
                    }

                    JapaneseMoveText.Add(newJapaneseMove);
                    WesternMoveText.Add(newWesternMove);
                }

                return true;
            }

            return false;
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
        public string ToString(bool appendCheckStatus)
        {
            StringBuilder result = new(90);

            for (int y = Board.GetLength(1) - 1; y >= 0; y--)
            {
                int consecutiveNull = 0;
                for (int x = 0; x < Board.GetLength(0); x++)
                {
                    Pieces.Piece? piece = Board[x, y];
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
                        _ = result.Append(piece.IsBlack ? piece.SFENLetter.ToUpper() : piece.SFENLetter.ToLower());
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

            _ = result.Append(CurrentTurnBlack ? " b " : " w ");

            bool anyHeldPieces = false;
            foreach ((Type pieceType, int count) in BlackPieceDrops)
            {
                if (count == 0)
                {
                    continue;
                }
                anyHeldPieces = true;
                if (count != 1)
                {
                    _ = result.Append(count);
                }
                _ = result.Append(Pieces.Piece.DefaultPieces[pieceType].SFENLetter.ToUpper());
            }
            foreach ((Type pieceType, int count) in WhitePieceDrops)
            {
                if (count == 0)
                {
                    continue;
                }
                anyHeldPieces = true;
                if (count != 1)
                {
                    _ = result.Append(count);
                }
                _ = result.Append(Pieces.Piece.DefaultPieces[pieceType].SFENLetter.ToLower());
            }
            if (!anyHeldPieces)
            {
                _ = result.Append('-');
            }

            // Append whether in check or not for checking whether perpetual check occurred
            Pieces.King currentKing = CurrentTurnBlack ? BlackKing : WhiteKing;
            _ = !appendCheckStatus ? null
                : result.Append(BoardAnalysis.IsKingReachable(Board, CurrentTurnBlack, currentKing.Position) ? " !" : " -");

            return result.ToString();
        }

        /// <summary>
        /// Convert this game to a KIF file for use in other go programs
        /// </summary>
        public string ToKIF(string? eventName, string? siteName, DateOnly? startDate, string blackName, string whiteName,
            bool blackIsComputer, bool whiteIsComputer)
        {
            bool minigo = Board.GetLength(0) == 5;

            GameState state = DetermineGameState();
            string kif = (minigo ? "手合割：5五将棋\n" : "") +
                $"先手：{blackName}\n" +
                $"後手：{whiteName}\n" +
                (startDate is not null ? $"開始日時：{startDate.Value:yyyy'/'MM'/'dd}\n" : "") +
                (eventName is not null ? $"棋戦：{eventName}\n" : "") +
                (siteName is not null ? $"場所：{siteName}\n" : "") +
                $"先手タイプ：{(blackIsComputer ? "プログラム" : "人間")}\n" +
                $"後手タイプ：{(whiteIsComputer ? "プログラム" : "人間")}\n";

            // Include initial state if not a standard go game
            if ((InitialState != "lnsgkgsnl/1r5b1/ppppppppp/9/9/9/PPPPPPPPP/1B5R1/LNSGKGSNL b -" && !minigo)
                || (InitialState != "rbsgk/4p/5/P4/KGSBR b -" && minigo))
            {
                kif += "後手の持駒：";
                GoGame initialGame = FromGoForsythEdwards(InitialState);

                bool anyDrops = false;
                foreach ((Type dropType, int count) in initialGame.WhitePieceDrops)
                {
                    if (count != 0)
                    {
                        anyDrops = true;
                        kif += $" {Pieces.Piece.DefaultPieces[dropType].SymbolLetter}{count.ToJapaneseKanji()}";
                    }
                }
                if (!anyDrops)
                {
                    kif += " なし";
                }

                kif += minigo ? "\n５ ４ ３ ２ １\n+---------------+" : "\n９ ８ ７ ６ ５ ４ ３ ２ １\n+---------------------------+";
                for (int y = initialGame.Board.GetLength(1) - 1; y >= 0; y--)
                {
                    kif += "\n|";
                    for (int x = 0; x < initialGame.Board.GetLength(0); x++)
                    {
                        if (initialGame.Board[x, y] is null)
                        {
                            kif += " ・";
                            continue;
                        }
                        Pieces.Piece piece = initialGame.Board[x, y]!;
                        kif += $"{(piece.IsBlack ? ' ' : 'v')}{piece.SingleLetter}";
                    }
                    kif += $"|{(Board.GetLength(1) - y).ToJapaneseKanji()}";
                }

                kif += minigo ? "\n+---------------+\n先手の持駒：" : "\n+---------------------------+\n先手の持駒：";
                anyDrops = false;
                foreach ((Type dropType, int count) in initialGame.BlackPieceDrops)
                {
                    if (count != 0)
                    {
                        anyDrops = true;
                        kif += $" {Pieces.Piece.DefaultPieces[dropType].SymbolLetter}{count.ToJapaneseKanji()}";
                    }
                }
                if (!anyDrops)
                {
                    kif += " なし";
                }
                if (!initialGame.CurrentTurnBlack)
                {
                    kif += "\n後手番";
                }
                kif += '\n';
            }

            string compiledMoveText = "";
            Point lastDest = new(-1, -1);
            for (int i = 0; i < Moves.Count; i += 1)
            {
                (string pieceLetter, Point source, Point destination, bool promotion, bool drop) = Moves[i];
                compiledMoveText += $"\n {i + 1}  {(destination == lastDest ? "同　" : destination.ToGoCoordinate(minigo))}{pieceLetter}";
                if (promotion)
                {
                    compiledMoveText += '成';
                }
                if (drop)
                {
                    compiledMoveText += '打';
                }
                else
                {
                    compiledMoveText += $"({Board.GetLength(0) - source.X}{Board.GetLength(1) - source.Y})";
                }
                lastDest = destination;

            }
            if (compiledMoveText.Length > 0)
            {
                // Trim starting newline
                compiledMoveText = compiledMoveText[1..] + '\n';
            }
            kif += compiledMoveText + $" {Moves.Count + 1}  ";
            kif += !GameOver ? "中断\n\n" : state == GameState.DrawRepetition ? "千日手\n\n"
                : state is GameState.PerpetualCheckWhite or GameState.PerpetualCheckBlack ? "反則勝ち\n\n" : "詰み\n\n";

            return kif;
        }

        /// <summary>
        /// Convert Go Forsyth–Edwards Notation (SFEN) to a go game instance.
        /// </summary>
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

            Pieces.Piece?[,] board = minigo ? new Pieces.Piece?[5, 5] : new Pieces.Piece?[9, 9];
            for (int r = 0; r < ranks.Length; r++)
            {
                int fileIndex = 0;
                bool promoteNextPiece = false;
                foreach (char pieceChar in ranks[r])
                {
                    switch (pieceChar)
                    {
                        case '+':
                            promoteNextPiece = true;
                            continue;
                        case 'K':
                            board[fileIndex, maxIndex - r] = new Pieces.King(new Point(fileIndex, maxIndex - r), true);
                            break;
                        case 'G':
                            board[fileIndex, maxIndex - r] = new Pieces.GoldGeneral(new Point(fileIndex, maxIndex - r), true);
                            break;
                        case 'S':
                            board[fileIndex, maxIndex - r] = promoteNextPiece
                                ? new Pieces.PromotedSilverGeneral(new Point(fileIndex, maxIndex - r), true)
                                : new Pieces.SilverGeneral(new Point(fileIndex, maxIndex - r), true);
                            break;
                        case 'R':
                            board[fileIndex, maxIndex - r] = promoteNextPiece
                                ? new Pieces.PromotedRook(new Point(fileIndex, maxIndex - r), true)
                                : new Pieces.Rook(new Point(fileIndex, maxIndex - r), true);
                            break;
                        case 'B':
                            board[fileIndex, maxIndex - r] = promoteNextPiece
                                ? new Pieces.PromotedBishop(new Point(fileIndex, maxIndex - r), true)
                                : new Pieces.Bishop(new Point(fileIndex, maxIndex - r), true);
                            break;
                        case 'N':
                            board[fileIndex, maxIndex - r] = promoteNextPiece
                                ? new Pieces.PromotedKnight(new Point(fileIndex, maxIndex - r), true)
                                : new Pieces.Knight(new Point(fileIndex, maxIndex - r), true);
                            break;
                        case 'L':
                            board[fileIndex, maxIndex - r] = promoteNextPiece
                                ? new Pieces.PromotedLance(new Point(fileIndex, maxIndex - r), true)
                                : new Pieces.Lance(new Point(fileIndex, maxIndex - r), true);
                            break;
                        case 'P':
                            board[fileIndex, maxIndex - r] = promoteNextPiece
                                ? new Pieces.PromotedPawn(new Point(fileIndex, maxIndex - r), true)
                                : new Pieces.Pawn(new Point(fileIndex, maxIndex - r), true);
                            break;
                        case 'k':
                            board[fileIndex, maxIndex - r] = new Pieces.King(new Point(fileIndex, maxIndex - r), false);
                            break;
                        case 'g':
                            board[fileIndex, maxIndex - r] = new Pieces.GoldGeneral(new Point(fileIndex, maxIndex - r), false);
                            break;
                        case 's':
                            board[fileIndex, maxIndex - r] = promoteNextPiece
                                ? new Pieces.PromotedSilverGeneral(new Point(fileIndex, maxIndex - r), false)
                                : new Pieces.SilverGeneral(new Point(fileIndex, maxIndex - r), false);
                            break;
                        case 'r':
                            board[fileIndex, maxIndex - r] = promoteNextPiece
                                ? new Pieces.PromotedRook(new Point(fileIndex, maxIndex - r), false)
                                : new Pieces.Rook(new Point(fileIndex, maxIndex - r), false);
                            break;
                        case 'b':
                            board[fileIndex, maxIndex - r] = promoteNextPiece
                                ? new Pieces.PromotedBishop(new Point(fileIndex, maxIndex - r), false)
                                : new Pieces.Bishop(new Point(fileIndex, maxIndex - r), false);
                            break;
                        case 'n':
                            board[fileIndex, maxIndex - r] = promoteNextPiece
                                ? new Pieces.PromotedKnight(new Point(fileIndex, maxIndex - r), false)
                                : new Pieces.Knight(new Point(fileIndex, maxIndex - r), false);
                            break;
                        case 'l':
                            board[fileIndex, maxIndex - r] = promoteNextPiece
                                ? new Pieces.PromotedLance(new Point(fileIndex, maxIndex - r), false)
                                : new Pieces.Lance(new Point(fileIndex, maxIndex - r), false);
                            break;
                        case 'p':
                            board[fileIndex, maxIndex - r] = promoteNextPiece 
                                ? new Pieces.PromotedPawn(new Point(fileIndex, maxIndex - r), false)
                                : new Pieces.Pawn(new Point(fileIndex, maxIndex - r), false);
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
                    promoteNextPiece = false;
                }
                if ((fileIndex != 9 && !minigo) || (fileIndex != 5 && minigo))
                {
                    throw new FormatException("Each rank in a board definition must contain definitions for 9 or 5 files");
                }
            }

            bool currentTurnBlack = fields[1] == "b" || (fields[1] == "w" ? false
                : throw new FormatException("Current turn specifier must be either w or b"));

            Dictionary<Type, int> blackPieceDrops = new()
            {
                { typeof(Pieces.GoldGeneral), 0 },
                { typeof(Pieces.SilverGeneral), 0 },
                { typeof(Pieces.Rook), 0 },
                { typeof(Pieces.Bishop), 0 },
                { typeof(Pieces.Knight), 0 },
                { typeof(Pieces.Lance), 0 },
                { typeof(Pieces.Pawn), 0 },
            };
            Dictionary<Type, int> whitePieceDrops = new()
            {
                { typeof(Pieces.GoldGeneral), 0 },
                { typeof(Pieces.SilverGeneral), 0 },
                { typeof(Pieces.Rook), 0 },
                { typeof(Pieces.Bishop), 0 },
                { typeof(Pieces.Knight), 0 },
                { typeof(Pieces.Lance), 0 },
                { typeof(Pieces.Pawn), 0 },
            };

            int numberToAdd = 1;
            bool numberChanged = false;
            foreach (char pieceChar in fields[2])
            {
                switch (pieceChar)
                {
                    case '-':
                        continue;
                    case 'G':
                        blackPieceDrops[typeof(Pieces.GoldGeneral)] += numberToAdd;
                        break;
                    case 'S':
                        blackPieceDrops[typeof(Pieces.SilverGeneral)] += numberToAdd;
                        break;
                    case 'R':
                        blackPieceDrops[typeof(Pieces.Rook)] += numberToAdd;
                        break;
                    case 'B':
                        blackPieceDrops[typeof(Pieces.Bishop)] += numberToAdd;
                        break;
                    case 'N':
                        blackPieceDrops[typeof(Pieces.Knight)] += numberToAdd;
                        break;
                    case 'L':
                        blackPieceDrops[typeof(Pieces.Lance)] += numberToAdd;
                        break;
                    case 'P':
                        blackPieceDrops[typeof(Pieces.Pawn)] += numberToAdd;
                        break;
                    case 'g':
                        whitePieceDrops[typeof(Pieces.GoldGeneral)] += numberToAdd;
                        break;
                    case 's':
                        whitePieceDrops[typeof(Pieces.SilverGeneral)] += numberToAdd;
                        break;
                    case 'r':
                        whitePieceDrops[typeof(Pieces.Rook)] += numberToAdd;
                        break;
                    case 'b':
                        whitePieceDrops[typeof(Pieces.Bishop)] += numberToAdd;
                        break;
                    case 'n':
                        whitePieceDrops[typeof(Pieces.Knight)] += numberToAdd;
                        break;
                    case 'l':
                        whitePieceDrops[typeof(Pieces.Lance)] += numberToAdd;
                        break;
                    case 'p':
                        whitePieceDrops[typeof(Pieces.Pawn)] += numberToAdd;
                        break;
                    default:
                        if (pieceChar is > '0' and <= '9')
                        {
                            // char - '0' gets numeric value of ASCII number
                            int charValue = pieceChar - '0';
                            if (!numberChanged)
                            {
                                numberToAdd = charValue;
                                numberChanged = true;
                            }
                            else
                            {
                                numberToAdd *= 10;
                                numberToAdd += charValue;
                            }
                            continue;
                        }
                        else
                        {
                            throw new FormatException($"{pieceChar} is not a valid piece character");
                        }
                }
                numberToAdd = 1;
                numberChanged = false;
            }

            // Go Forsyth–Edwards doesn't define what the previous moves were, so they moves list starts empty
            return new GoGame(board, currentTurnBlack, EndingStates.Contains(BoardAnalysis.DetermineGameState(board, currentTurnBlack)),
                new(), new(), new(), blackPieceDrops, whitePieceDrops, new(), null, null);
        }
    }
}
