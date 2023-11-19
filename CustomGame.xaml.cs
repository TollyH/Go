using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Shapes;

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

        private double tileWidth;
        private double tileHeight;

        public CustomGame()
        {
            GeneratedGame = null;

            InitializeComponent();

            Board = new bool?[19, 19];
        }

        public void UpdateBoard()
        {
            int boardWidth = Board.GetLength(0);
            int boardHeight = Board.GetLength(1);

            tileWidth = goGameCanvas.ActualWidth / boardWidth;
            tileHeight = goGameCanvas.ActualHeight / boardHeight;

            goGameCanvas.Children.Clear();

            for (int x = 0; x < boardWidth; x++)
            {
                for (int y = 0; y < boardHeight; y++)
                {
                    bool? stone = Board[x, y];
                    if (stone is not null)
                    {
                        Ellipse newStone = new()
                        {
                            Width = tileWidth,
                            Height = tileHeight,
                            Fill = stone.Value ? Brushes.Black : Brushes.White
                        };
                        _ = goGameCanvas.Children.Add(newStone);
                        Canvas.SetBottom(newStone, y * tileHeight);
                        Canvas.SetLeft(newStone, x * tileWidth);
                    }
                }
            }

            goBoardBackground.Children.Clear();

            for (int x = 0; x < boardWidth; x++)
            {
                _ = goBoardBackground.Children.Add(new Rectangle()
                {
                    Margin = new Thickness((tileWidth * x) + (tileWidth / 2), 0, 0, -1),
                    Fill = new SolidColorBrush(Color.FromArgb(191, 0, 0, 0)),
                    StrokeThickness = 0,
                    Height = goGameCanvas.ActualHeight - tileHeight,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = 2
                });
            }

            for (int y = 0; y < boardHeight; y++)
            {
                _ = goBoardBackground.Children.Add(new Rectangle()
                {
                    Margin = new Thickness(0, (tileHeight * y) + (tileHeight / 2), -1, 0),
                    Fill = new SolidColorBrush(Color.FromArgb(191, 0, 0, 0)),
                    StrokeThickness = 0,
                    Width = goGameCanvas.ActualWidth - tileWidth,
                    VerticalAlignment = VerticalAlignment.Top,
                    Height = 2
                });
            }

            if (boardWidth % 2 == 1 && boardHeight % 2 == 1)
            {
                boardCenterDot.Visibility = Visibility.Visible;
                boardCenterDot.Margin = new Thickness(0, 0, (goGameCanvas.ActualWidth / 2f) - 7f, (goGameCanvas.ActualHeight / 2f) - 7f);
                boardDot5.Visibility = Visibility.Visible;
                boardDot6.Visibility = Visibility.Visible;
                boardDot7.Visibility = Visibility.Visible;
                boardDot8.Visibility = Visibility.Visible;
            }
            else
            {
                boardCenterDot.Visibility = Visibility.Collapsed;
                boardDot5.Visibility = Visibility.Collapsed;
                boardDot6.Visibility = Visibility.Collapsed;
                boardDot7.Visibility = Visibility.Collapsed;
                boardDot8.Visibility = Visibility.Collapsed;
            }

            if (boardWidth >= 13 && boardHeight >= 13)
            {
                boardDot1.Visibility = Visibility.Visible;
                boardDot2.Visibility = Visibility.Visible;
                boardDot3.Visibility = Visibility.Visible;
                boardDot4.Visibility = Visibility.Visible;

                boardDot1.Margin = new Thickness(0, 0, (tileWidth * (boardWidth - 3)) - (tileWidth / 2) - 7f,
                    (tileHeight * (boardHeight - 3)) - (tileHeight / 2) - 7f);
                boardDot2.Margin = new Thickness(0, 0, (tileWidth * (boardWidth - 3)) - (tileWidth / 2) - 7f,
                    (tileHeight * 3) + (tileHeight / 2) - 7f);
                boardDot3.Margin = new Thickness(0, 0, (tileWidth * 3) + (tileWidth / 2) - 7f,
                    (tileHeight * (boardHeight - 3)) - (tileHeight / 2) - 7f);
                boardDot4.Margin = new Thickness(0, 0, (tileWidth * 3) + (tileWidth / 2) - 7f,
                    (tileHeight * 3) + (tileHeight / 2) - 7f);

                boardDot5.Margin = new Thickness(0, 0, (goGameCanvas.ActualWidth / 2f) - 7f,
                    (tileHeight * (boardHeight - 3)) - (tileHeight / 2) - 7f);
                boardDot6.Margin = new Thickness(0, 0, (tileWidth * (boardWidth - 3)) - (tileWidth / 2) - 7f,
                    (goGameCanvas.ActualHeight / 2f) - 7f);
                boardDot7.Margin = new Thickness(0, 0, (tileWidth * 3) + (tileWidth / 2) - 7f,
                    (goGameCanvas.ActualHeight / 2f) - 7f);
                boardDot8.Margin = new Thickness(0, 0, (goGameCanvas.ActualWidth / 2f) - 7f,
                    (tileHeight * 3) + (tileHeight / 2) - 7f);
            }
            else if (boardWidth >= 8 && boardHeight >= 8)
            {
                boardDot1.Visibility = Visibility.Visible;
                boardDot2.Visibility = Visibility.Visible;
                boardDot3.Visibility = Visibility.Visible;
                boardDot4.Visibility = Visibility.Visible;
                boardDot5.Visibility = Visibility.Visible;
                boardDot6.Visibility = Visibility.Visible;
                boardDot7.Visibility = Visibility.Visible;
                boardDot8.Visibility = Visibility.Visible;

                boardDot1.Margin = new Thickness(0, 0, (tileWidth * (boardWidth - 2)) - (tileWidth / 2) - 7f,
                    (tileHeight * (boardHeight - 2)) - (tileHeight / 2) - 7f);
                boardDot2.Margin = new Thickness(0, 0, (tileWidth * (boardWidth - 2)) - (tileWidth / 2) - 7f,
                    (tileHeight * 2) + (tileHeight / 2) - 7f);
                boardDot3.Margin = new Thickness(0, 0, (tileWidth * 2) + (tileWidth / 2) - 7f,
                    (tileHeight * (boardHeight - 2)) - (tileHeight / 2) - 7f);
                boardDot4.Margin = new Thickness(0, 0, (tileWidth * 2) + (tileWidth / 2) - 7f,
                    (tileHeight * 2) + (tileHeight / 2) - 7f);

                boardDot5.Margin = new Thickness(0, 0, (goGameCanvas.ActualWidth / 2f) - 7f,
                    (tileHeight * (boardHeight - 2)) - (tileHeight / 2) - 7f);
                boardDot6.Margin = new Thickness(0, 0, (tileWidth * (boardWidth - 2)) - (tileWidth / 2) - 7f,
                    (goGameCanvas.ActualHeight / 2f) - 7f);
                boardDot7.Margin = new Thickness(0, 0, (tileWidth * 2) + (tileWidth / 2) - 7f,
                    (goGameCanvas.ActualHeight / 2f) - 7f);
                boardDot8.Margin = new Thickness(0, 0, (goGameCanvas.ActualWidth / 2f) - 7f,
                    (tileHeight * 2) + (tileHeight / 2) - 7f);
            }
            else
            {
                boardDot1.Visibility = Visibility.Collapsed;
                boardDot2.Visibility = Visibility.Collapsed;
                boardDot3.Visibility = Visibility.Collapsed;
                boardDot4.Visibility = Visibility.Collapsed;
                boardDot5.Visibility = Visibility.Collapsed;
                boardDot6.Visibility = Visibility.Collapsed;
                boardDot7.Visibility = Visibility.Collapsed;
                boardDot8.Visibility = Visibility.Collapsed;
            }
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            BlackIsComputer = computerSelectBlack.IsChecked ?? false;
            WhiteIsComputer = computerSelectWhite.IsChecked ?? false;
            bool currentTurnBlack = turnSelectBlack.IsChecked ?? false;

            double komiCompensation = (int)komiSlider.Value;
            if (komiHalfPoint.IsChecked ?? false)
            {
                komiCompensation += 0.5;
            }

            ScoringSystem scoring;
            if (scoringSelectArea.IsChecked ?? false)
            {
                scoring = ScoringSystem.Area;
            }
            else if (scoringSelectTerritory.IsChecked ?? false)
            {
                scoring = ScoringSystem.Territory;
            }
            else
            {
                scoring = ScoringSystem.Stone;
            }

            GeneratedGame = new GoGame(Board, currentTurnBlack, false, scoring,
                new List<System.Drawing.Point>(), new List<string>(),
                (int)blackCapturesSlider.Value, (int)whiteCapturesSlider.Value,
                new HashSet<string>(), null, null, komiCompensation);
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

            foreach (System.Drawing.Point stone in
                BoardAnalysis.GetSurroundedStones(Board, e.ChangedButton == MouseButton.Left))
            {
                // Remove stones that would be captured as soon as game starts
                Board[stone.X, stone.Y] = null;
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
                GeneratedGame = GoGame.FromBoardString(boardTextInput.Text);
                Close();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message, "Board Notation Error",
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

        private void BoardSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsInitialized)
            {
                return;
            }
            int newWidth = (int)boardWidthSlider.Value;
            int newHeight = (int)boardHeightSlider.Value;
            boardWidthHeader.Content = $"Width ({newWidth})";
            boardHeightHeader.Content = $"Height ({newHeight})";

            blackCapturesSlider.Maximum = Math.Floor((double)newWidth * newHeight / 2);
            whiteCapturesSlider.Maximum = Math.Ceiling((double)newWidth * newHeight / 2);

            bool?[,] newBoard = new bool?[newWidth, newHeight];
            for (int x = 0; x < newWidth && x < Board.GetLength(0); x++)
            {
                for (int y = 0; y < newHeight && y < Board.GetLength(1); y++)
                {
                    newBoard[x, y] = Board[x, y];
                }
            }
            Board = newBoard;
            UpdateBoard();
        }

        private void CapturesSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsInitialized)
            {
                return;
            }
            blackCapturesHeader.Content = $"Black ({(int)blackCapturesSlider.Value})";
            whiteCapturesHeader.Content = $"White ({(int)whiteCapturesSlider.Value})";
        }

        private void KomiSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsInitialized)
            {
                return;
            }
            komiHeader.Content = (int)komiSlider.Value;
        }
    }
}
