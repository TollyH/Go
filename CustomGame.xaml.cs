using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;

namespace Go
{
    /// <summary>
    /// Interaction logic for CustomGame.xaml
    /// </summary>
    public partial class CustomGame : Window
    {
        public bool?[,] Board { get; private set; }

        public GoGame? GeneratedGame { get; private set; }
        public bool BlackIsComputer { get; private set; }
        public bool WhiteIsComputer { get; private set; }

        private readonly Dictionary<Type, int> blackPieceDrops = new();
        private readonly Dictionary<Type, int> whitePieceDrops = new();

        private double tileWidth;
        private double tileHeight;

        public CustomGame(bool minigo)
        {
            GeneratedGame = null;

            InitializeComponent();

            if (minigo)
            {
                Board = new bool?[5, 5];
            }
            else
            {
                Board = new bool?[9, 9];
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
                    bool? piece = Board[x, y];
                    if (piece is not null)
                    {
                        // TODO: Replace with character based piece display
                        Image newPiece = new()
                        {
                            Source = null,
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
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            BlackIsComputer = computerSelectBlack.IsChecked ?? false;
            WhiteIsComputer = computerSelectWhite.IsChecked ?? false;
            bool currentTurnBlack = turnSelectBlack.IsChecked ?? false;
            GeneratedGame = new GoGame(Board, currentTurnBlack, false,
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

            Board[coord.X, coord.Y] = Board[coord.X, coord.Y] is null ? e.ChangedButton == MouseButton.Left : null;

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
    }
}
