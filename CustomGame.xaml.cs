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
        public int?[,] Board { get; private set; }

        public GoGame? GeneratedGame { get; private set; }

        private double tileWidth;
        private double tileHeight;

        private int[] captures;

        public CustomGame()
        {
            GeneratedGame = null;

            InitializeComponent();

            Board = new int?[19, 19];
            captures = new int[2];
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
                    int? stone = Board[x, y];
                    if (stone is not null)
                    {
                        Ellipse newStone = new()
                        {
                            Width = tileWidth,
                            Height = tileHeight,
                            Fill = MainWindow.PieceColours[stone.Value]
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

            GeneratedGame = new GoGame(Board, (int)turnSelect.Value, false, false, scoring,
                new List<System.Drawing.Point>(), new List<string>(),
                captures, new HashSet<string>(), null, null, (int)playerCountSelect.Value);
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

            if (e.ChangedButton == MouseButton.Left)
            {
                int playerCount = (int)playerCountSelect.Value;
                int? currentColour = Board[coord.X, coord.Y];
                Board[coord.X, coord.Y] = currentColour is null ? 0 : (currentColour.Value + 1) % playerCount;

                foreach (System.Drawing.Point stone in
                    BoardAnalysis.GetSurroundedStones(Board, (Board[coord.X, coord.Y]!.Value + 1) % playerCount))
                {
                    // Remove stones that would be captured as soon as game starts
                    Board[stone.X, stone.Y] = null;
                }
            }
            else
            {
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

            int?[,] newBoard = new int?[newWidth, newHeight];
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

        private void playerCountSelect_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsInitialized)
            {
                return;
            }
            int newPlayerCount = (int)playerCountSelect.Value;

            playerCapturesCombo.Items.Clear();
            for (int i = 0; i < newPlayerCount;)
            {
                _ = playerCapturesCombo.Items.Add(new ComboBoxItem()
                {
                    Content = $"Player {++i}"
                });
            }
            playerCapturesCombo.SelectedIndex = 0;

            turnSelect.Maximum = newPlayerCount - 1;

            playerCountHeader.Content = $"Player Count ({newPlayerCount})";

            captures = new int[newPlayerCount];

            for (int x = 0; x < Board.GetLength(0); x++)
            {
                for (int y = 0; y < Board.GetLength(1); y++)
                {
                    // Remove pieces for players than no longer exist
                    if (Board[x, y] is { } value && value >= newPlayerCount)
                    {
                        Board[x, y] = null;
                    }
                }
            }

            UpdateBoard();
        }

        private void playerCapturesCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsInitialized || playerCapturesCombo.SelectedIndex == -1 || playerCapturesCombo.SelectedIndex >= captures.Length)
            {
                return;
            }
            capturesSlider.Value = captures[playerCapturesCombo.SelectedIndex];
        }

        private void turnSelect_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsInitialized)
            {
                return;
            }
            turnHeader.Content = $"Current Turn ({(int)turnSelect.Value + 1})";
        }

        private void capturesSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsInitialized)
            {
                return;
            }
            captures[playerCapturesCombo.SelectedIndex] = (int)capturesSlider.Value;
        }
    }
}
