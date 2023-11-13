using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Collections.Generic;

namespace Go
{
    /// <summary>
    /// Interaction logic for CustomGame.xaml
    /// </summary>
    public partial class CustomGame : Window
    {
        public Pieces.Piece?[,] Board { get; private set; }

        public GoGame? GeneratedGame { get; private set; }
        public bool BlackIsComputer { get; private set; }
        public bool WhiteIsComputer { get; private set; }

        private readonly Settings config;

        private readonly Dictionary<Type, int> blackPieceDrops = new()
        {
            { typeof(Pieces.GoldGeneral), 0 },
            { typeof(Pieces.SilverGeneral), 0 },
            { typeof(Pieces.Rook), 0 },
            { typeof(Pieces.Bishop), 0 },
            { typeof(Pieces.Knight), 0 },
            { typeof(Pieces.Lance), 0 },
            { typeof(Pieces.Pawn), 0 },
        };
        private readonly Dictionary<Type, int> whitePieceDrops = new()
        {
            { typeof(Pieces.GoldGeneral), 0 },
            { typeof(Pieces.SilverGeneral), 0 },
            { typeof(Pieces.Rook), 0 },
            { typeof(Pieces.Bishop), 0 },
            { typeof(Pieces.Knight), 0 },
            { typeof(Pieces.Lance), 0 },
            { typeof(Pieces.Pawn), 0 },
        };

        private Pieces.King? blackKing = null;
        private Pieces.King? whiteKing = null;

        private double tileWidth;
        private double tileHeight;

        public CustomGame(Settings config, bool minigo)
        {
            GeneratedGame = null;
            this.config = config;

            InitializeComponent();

            if (minigo)
            {
                Board = new Pieces.Piece?[5, 5];
                goBoardBackground.Visibility = Visibility.Collapsed;
            }
            else
            {
                Board = new Pieces.Piece?[9, 9];
                miniGoBoardBackground.Visibility = Visibility.Collapsed;
            }
        }

        public void UpdateBoard()
        {
            tileWidth = goGameCanvas.ActualWidth / Board.GetLength(0);
            tileHeight = goGameCanvas.ActualHeight / Board.GetLength(1);

            goGameCanvas.Children.Clear();

            for (int x = 0; x < Board.GetLength(0); x++)
            {
                for (int y = 0; y < Board.GetLength(1); y++)
                {
                    Pieces.Piece? piece = Board[x, y];
                    if (piece is not null)
                    {
                        Image newPiece = new()
                        {
                            Source = new BitmapImage(
                                new Uri($"pack://application:,,,/Pieces/{config.PieceSet}/{(piece.IsBlack ? "Black" : "White")}/{piece.Name}.png")),
                            Width = tileWidth,
                            Height = tileHeight
                        };
                        RenderOptions.SetBitmapScalingMode(newPiece, BitmapScalingMode.HighQuality);
                        _ = goGameCanvas.Children.Add(newPiece);
                        Canvas.SetBottom(newPiece, y * tileHeight);
                        Canvas.SetLeft(newPiece, x * tileWidth);
                    }
                }
            }

            foreach (Grid dropItem in blackDropsPanel.Children)
            {
                Type pieceType = (Type)dropItem.Tag;
                int heldCount = blackPieceDrops[pieceType];
                dropItem.Opacity = heldCount == 0 ? 0.55 : 1;
                ((Label)dropItem.Children[1]).Content = heldCount;
                ((Image)dropItem.Children[0]).Source = new BitmapImage(new Uri(
                    $"pack://application:,,,/Pieces/{config.PieceSet}/Black/{((Image)dropItem.Children[0]).Tag}.png"));
            }
            foreach (Grid dropItem in whiteDropsPanel.Children)
            {
                Type pieceType = (Type)dropItem.Tag;
                int heldCount = whitePieceDrops[pieceType];
                dropItem.Opacity = heldCount == 0 ? 0.55 : 1;
                ((Label)dropItem.Children[1]).Content = heldCount;
                ((Image)dropItem.Children[0]).Source = new BitmapImage(new Uri(
                    $"pack://application:,,,/Pieces/{config.PieceSet}/White/{((Image)dropItem.Children[0]).Tag}.png"));
            }

            startButton.IsEnabled = blackKing is not null && whiteKing is not null;
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            BlackIsComputer = computerSelectBlack.IsChecked ?? false;
            WhiteIsComputer = computerSelectWhite.IsChecked ?? false;
            bool currentTurnBlack = turnSelectBlack.IsChecked ?? false;
            GeneratedGame = new GoGame(Board, currentTurnBlack,
                GoGame.EndingStates.Contains(BoardAnalysis.DetermineGameState(Board, currentTurnBlack)),
                new(), new(), new(), blackPieceDrops, whitePieceDrops, new(), null, null);
            Close();
        }

        private void importButton_Click(object sender, RoutedEventArgs e)
        {
            importOverlay.Visibility = Visibility.Visible;
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Point mousePoint = Mouse.GetPosition(goGameCanvas);
            if (mousePoint.X < 0 || mousePoint.Y < 0
                || mousePoint.X > goGameCanvas.ActualWidth || mousePoint.Y > goGameCanvas.ActualHeight)
            {
                return;
            }

            // Canvas coordinates are relative to top-left, whereas go's are from bottom-left, so y is inverted
            System.Drawing.Point coord = new((int)(mousePoint.X / tileWidth),
                (int)((goGameCanvas.ActualHeight - mousePoint.Y) / tileHeight));
            if (coord.X < 0 || coord.Y < 0 || coord.X >= Board.GetLength(0) || coord.Y >= Board.GetLength(1))
            {
                return;
            }

            if (Board[coord.X, coord.Y] is null)
            {
                bool black = e.ChangedButton == MouseButton.Left;
                if (pieceSelectKing.IsChecked ?? false)
                {
                    // Only allow one king of each colour
                    if (black && blackKing is null)
                    {
                        blackKing = new Pieces.King(coord, true);
                        Board[coord.X, coord.Y] = blackKing;
                    }
                    else if (!black && whiteKing is null)
                    {
                        whiteKing = new Pieces.King(coord, false);
                        Board[coord.X, coord.Y] = whiteKing;
                    }
                }
                else if (pieceSelectGoldGeneral.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.GoldGeneral(coord, black);
                }
                else if (pieceSelectSilverGeneral.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.SilverGeneral(coord, black);
                }
                else if (pieceSelectPromotedSilverGeneral.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.PromotedSilverGeneral(coord, black);
                }
                else if (pieceSelectRook.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.Rook(coord, black);
                }
                else if (pieceSelectPromotedRook.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.PromotedRook(coord, black);
                }
                else if (pieceSelectBishop.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.Bishop(coord, black);
                }
                else if (pieceSelectPromotedBishop.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.PromotedBishop(coord, black);
                }
                else if (pieceSelectKnight.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.Knight(coord, black);
                }
                else if (pieceSelectPromotedKnight.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.PromotedKnight(coord, black);
                }
                else if (pieceSelectLance.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.Lance(coord, black);
                }
                else if (pieceSelectPromotedLance.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.PromotedLance(coord, black);
                }
                else if (pieceSelectPawn.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.Pawn(coord, black);
                }
                else if (pieceSelectPromotedPawn.IsChecked ?? false)
                {
                    Board[coord.X, coord.Y] = new Pieces.PromotedPawn(coord, black);
                }
            }
            else
            {
                if (Board[coord.X, coord.Y] is Pieces.King king)
                {
                    if (king.IsBlack)
                    {
                        blackKing = null;
                    }
                    else
                    {
                        whiteKing = null;
                    }
                }
                Board[coord.X, coord.Y] = null;
            }

            UpdateBoard();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateBoard();
        }

        private void submitFenButton_Click(object sender, RoutedEventArgs e)
        {
            BlackIsComputer = computerSelectBlack.IsChecked ?? false;
            WhiteIsComputer = computerSelectWhite.IsChecked ?? false;
            try
            {
                GeneratedGame = GoGame.FromGoForsythEdwards(sfenInput.Text);
                Close();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message, "Go Forsyth–Edwards Notation Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cancelFenButton_Click(object sender, RoutedEventArgs e)
        {
            importOverlay.Visibility = Visibility.Hidden;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateBoard();
        }

        private void WhiteDrop_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Type clickedType = (Type)((Grid)sender).Tag;
            if (e.ChangedButton == MouseButton.Left)
            {
                whitePieceDrops[clickedType]++;
            }
            else if (whitePieceDrops[clickedType] != 0)
            {
                whitePieceDrops[clickedType]--;
            }
            UpdateBoard();
        }

        private void BlackDrop_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Type clickedType = (Type)((Grid)sender).Tag;
            if (e.ChangedButton == MouseButton.Left)
            {
                blackPieceDrops[clickedType]++;
            }
            else if (blackPieceDrops[clickedType] != 0)
            {
                blackPieceDrops[clickedType]--;
            }
            UpdateBoard();
        }
    }
}
