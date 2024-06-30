using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Go
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly SolidColorBrush[] PieceColours = new SolidColorBrush[10]
        {
            Brushes.Black, Brushes.White, Brushes.Red, Brushes.Blue, Brushes.Green,
            Brushes.Aquamarine, Brushes.Magenta, Brushes.Yellow, Brushes.BlueViolet, Brushes.Coral
        };

        private GoGame game = new(19, 19, ScoringSystem.Area);
        private int?[,]? inProcessDeadStoneBoard = null;
        private readonly Settings config;

        private readonly HashSet<System.Drawing.Point> squareHighlights = new();
        private readonly HashSet<(System.Drawing.Point, System.Drawing.Point)> lineHighlights = new();
        private System.Drawing.Point? mouseDownStartPoint = null;

        private double tileWidth;
        private double tileHeight;

        public MainWindow()
        {
            string jsonPath = System.IO.Path.Join(AppDomain.CurrentDomain.BaseDirectory, "go-settings.json");
            config = File.Exists(jsonPath)
                ? JsonConvert.DeserializeObject<Settings>(File.ReadAllText(jsonPath)) ?? new Settings()
                : new Settings();

            InitializeComponent();

            goBoardBackground.Background = new SolidColorBrush(config.BoardColor);
            flipBoardItem.IsChecked = config.FlipBoard;
            highlightIllegalItem.IsChecked = config.HighlightIllegalMoves;
            updateEvalAfterBotItem.IsChecked = config.UpdateEvalAfterBot;
        }

        public void UpdateGameDisplay()
        {
            goGameCanvas.Children.Clear();
            passMenuItem.IsEnabled = !game.GameOver || (game.AwaitingDeadStoneRemoval && inProcessDeadStoneBoard is not null);

            bool boardFlipped = config.FlipBoard && game.CurrentTurn == 1;

            int boardWidth = game.Board.GetLength(0);
            int boardHeight = game.Board.GetLength(1);

            tileWidth = goGameCanvas.ActualWidth / boardWidth;
            tileHeight = goGameCanvas.ActualHeight / boardHeight;

            capturesLabel.Content = game.Captures[game.CurrentTurn];

            if (boardFlipped)
            {
                Grid.SetColumn(capturesView, 0);
                Grid.SetRow(capturesView, 0);
            }
            else
            {
                Grid.SetColumn(capturesView, 2);
                Grid.SetRow(capturesView, 2);
            }

            movesPanel.Children.Clear();
            for (int i = 0; i < game.MoveText.Count; i++)
            {
                string text = $"{i + 1}. {game.MoveText[i]}";
                _ = movesPanel.Children.Add(new Label()
                {
                    Content = text,
                    FontSize = 18
                });
            }

            int boardMaxY = boardHeight - 1;
            int boardMaxX = boardWidth - 1;

            if (game.Moves.Count > 0 && game.Moves[^1].X >= 0)
            {
                System.Drawing.Point lastMoveDestination = game.Moves[^1];

                Rectangle destinationMoveHighlight = new()
                {
                    Width = tileWidth,
                    Height = tileHeight,
                    Fill = new SolidColorBrush(config.LastMoveDestinationColor)
                };
                _ = goGameCanvas.Children.Add(destinationMoveHighlight);
                Canvas.SetBottom(destinationMoveHighlight, (boardFlipped ? boardMaxY - lastMoveDestination.Y : lastMoveDestination.Y) * tileHeight);
                Canvas.SetLeft(destinationMoveHighlight, (boardFlipped ? boardMaxX - lastMoveDestination.X : lastMoveDestination.X) * tileWidth);

            }

            // Used to highlight territory when the game is over
            int?[,] scoredTerritory;
            if (game is { GameOver: true, AwaitingDeadStoneRemoval: false, CurrentScoring: ScoringSystem.Area or ScoringSystem.Territory })
            {
                scoredTerritory =
                    BoardAnalysis.FillSurroundedAreas(game.Board, game.CurrentScoring == ScoringSystem.Area);
            }
            else
            {
                scoredTerritory = new int?[boardWidth, boardHeight];
            }

            for (int x = 0; x < boardWidth; x++)
            {
                for (int y = 0; y < boardHeight; y++)
                {
                    int? stone = game.Board[x, y];
                    int? scoredStone = scoredTerritory[x, y];
                    if (stone is not null)
                    {
                        double opacity;
                        if (!game.GameOver)
                        {
                            opacity = 1.0;
                        }
                        else if (game.AwaitingDeadStoneRemoval)
                        {
                            // Show stones that have been marked dead as transparent
                            opacity = inProcessDeadStoneBoard is not null && inProcessDeadStoneBoard[x, y] is null ? 0.4 : 1.0;
                        }
                        else
                        {
                            // Placed stones aren't counted in territory scoring, so make them transparent when game ends
                            opacity = game.CurrentScoring == ScoringSystem.Territory ? 0.4 : 1.0;
                        }
                        Ellipse newStone = new()
                        {
                            Width = tileWidth,
                            Height = tileHeight,
                            Fill = PieceColours[stone.Value],
                            Opacity = opacity
                        };
                        _ = goGameCanvas.Children.Add(newStone);
                        Canvas.SetBottom(newStone, (boardFlipped ? boardMaxY - y : y) * tileHeight);
                        Canvas.SetLeft(newStone, (boardFlipped ? boardMaxX - x : x) * tileWidth);
                    }
                    else if (game.GameOver && scoredStone is not null)
                    {
                        // Show surrounded territory that counted toward the final score once the game ends
                        Ellipse newStone = new()
                        {
                            Width = tileWidth / 2,
                            Height = tileHeight / 2,
                            Fill = PieceColours[scoredStone.Value]
                        };
                        _ = goGameCanvas.Children.Add(newStone);
                        Canvas.SetBottom(newStone, (boardFlipped ? boardMaxY - y : y) * tileHeight + (tileHeight / 4));
                        Canvas.SetLeft(newStone, (boardFlipped ? boardMaxX - x : x) * tileWidth + (tileWidth / 4));
                    }
                    else if (!game.GameOver && config.HighlightIllegalMoves && !game.IsPlacementPossible(new System.Drawing.Point(x, y)))
                    {
                        Rectangle illegalMoveHighlight = new()
                        {
                            Width = tileWidth,
                            Height = tileHeight,
                            Fill = new SolidColorBrush(config.IllegalMoveColor)
                        };
                        _ = goGameCanvas.Children.Add(illegalMoveHighlight);
                        Canvas.SetBottom(illegalMoveHighlight,
                            (boardFlipped ? boardMaxY - y : y) * tileHeight);
                        Canvas.SetLeft(illegalMoveHighlight,
                            (boardFlipped ? boardMaxX - x : x) * tileWidth);
                    }
                }
            }

            foreach (System.Drawing.Point square in squareHighlights)
            {
                Ellipse ellipse = new()
                {
                    Fill = Brushes.Blue,
                    Opacity = 0.5,
                    Width = tileWidth * 0.8,
                    Height = tileHeight * 0.8
                };
                _ = goGameCanvas.Children.Add(ellipse);
                Canvas.SetBottom(ellipse, ((boardFlipped ? boardMaxY - square.Y : square.Y) * tileHeight) + (tileHeight * 0.1));
                Canvas.SetLeft(ellipse, (boardFlipped ? boardMaxX - square.X : square.X) * tileWidth + (tileWidth * 0.1));
            }

            foreach ((System.Drawing.Point lineStart, System.Drawing.Point lineEnd) in lineHighlights)
            {
                double arrowLength = Math.Min(tileWidth, tileHeight) / 4;
                Petzold.Media2D.ArrowLine line = new()
                {
                    Stroke = Brushes.Blue,
                    Fill = Brushes.Blue,
                    Opacity = 0.5,
                    StrokeThickness = 10,
                    ArrowLength = arrowLength,
                    ArrowAngle = 45,
                    IsArrowClosed = true,
                    X1 = (boardFlipped ? boardMaxX - lineStart.X : lineStart.X) * tileWidth + (tileWidth / 2),
                    X2 = (boardFlipped ? boardMaxX - lineEnd.X : lineEnd.X) * tileWidth + (tileWidth / 2),
                    Y1 = (boardFlipped ? lineStart.Y : boardMaxY - lineStart.Y) * tileHeight + (tileHeight / 2),
                    Y2 = (boardFlipped ? lineEnd.Y : boardMaxY - lineEnd.Y) * tileHeight + (tileHeight / 2)
                };
                _ = goGameCanvas.Children.Add(line);
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

        /// <summary>
        /// If the game has ended, alert the user how it ended, otherwise do nothing
        /// </summary>
        private void PushEndgameMessage()
        {
            if (game.GameOver)
            {
                if (game.AwaitingDeadStoneRemoval)
                {
                    MessageBoxResult result = MessageBox.Show(
                        "All players have passed their turn." + Environment.NewLine +
                        "If you agree to end the game now, click 'Yes'. " + Environment.NewLine +
                        "If you wish to continue play instead, click 'No'.",
                        "Two passes", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.No)
                    {
                        game.GameOver = false;
                        game.AwaitingDeadStoneRemoval = false;
                        inProcessDeadStoneBoard = null;
                    }
                    else
                    {
                        inProcessDeadStoneBoard = game.Board.TwoDimensionalClone();
                        _ = MessageBox.Show(
                            "To mark a group of stones as dead, click on them." + Environment.NewLine +
                            "To revert a group to being alive, click it again. " + Environment.NewLine +
                            "Once you've finished, press the Pass Turn button again. ",
                            "Dead stone removal", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    UpdateGameDisplay();
                }
                else
                {
                    int[] finalScores = BoardAnalysis.CalculateGameValue(game, game.CurrentScoring);
                    (int Player, int Score) winner = finalScores.Select((score, p) => (p, score)).MaxBy(w => w.score);

                    string message = $"Player {winner.Player + 1} wins!\nScores:";
                    for (int i = 0; i < finalScores.Length; i++)
                    {
                        message += $"\n    {i + 1}: {finalScores[i]}";
                    }

                    _ = MessageBox.Show(message, "Game over", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private System.Drawing.Point GetCoordFromCanvasPoint(Point position)
        {
            bool boardFlipped = config.FlipBoard && game.CurrentTurn == 1;
            // Canvas coordinates are relative to top-left, whereas go's are from bottom-left, so y is inverted
            return new System.Drawing.Point((int)((boardFlipped ? goGameCanvas.ActualWidth - position.X : position.X) / tileWidth),
                (int)((!boardFlipped ? goGameCanvas.ActualHeight - position.Y : position.Y) / tileHeight));
        }

        private void NewGame(int boardWidth, int boardHeight)
        {
            game = new GoGame(boardWidth, boardHeight, ScoringSystem.Area);
            inProcessDeadStoneBoard = null;
            UpdateGameDisplay();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateGameDisplay();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateGameDisplay();
            moveListColumn.Width = ActualWidth < 900 ? new GridLength(0) : new GridLength(210);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = Mouse.GetPosition(goGameCanvas);
            if (e.ChangedButton == MouseButton.Left)
            {
                squareHighlights.Clear();
                lineHighlights.Clear();
            }
            else
            {
                if (mousePos.X < 0 || mousePos.Y < 0
                    || mousePos.X > goGameCanvas.ActualWidth || mousePos.Y > goGameCanvas.ActualHeight)
                {
                    return;
                }
                mouseDownStartPoint = GetCoordFromCanvasPoint(mousePos);
            }
            UpdateGameDisplay();
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = Mouse.GetPosition(goGameCanvas);
            if (mousePos.X < 0 || mousePos.Y < 0
                || mousePos.X > goGameCanvas.ActualWidth || mousePos.Y > goGameCanvas.ActualHeight)
            {
                return;
            }
            if (e.ChangedButton == MouseButton.Left)
            {
                System.Drawing.Point destination = GetCoordFromCanvasPoint(mousePos);
                if (game.AwaitingDeadStoneRemoval && inProcessDeadStoneBoard is not null)
                {
                    if (inProcessDeadStoneBoard[destination.X, destination.Y] is not null)
                    {
                        BoardAnalysis.FloodFillArea(inProcessDeadStoneBoard, destination, null);
                    }
                    else if (game.Board[destination.X, destination.Y] is not null)
                    {
                        BoardAnalysis.FloodFillArea(inProcessDeadStoneBoard, destination, game.Board[destination.X, destination.Y], game.Board);
                    }
                }
                else
                {
                    if (game.GameOver)
                    {
                        return;
                    }
                    bool success = game.PlaceStone(destination);
                    if (success)
                    {
                        UpdateGameDisplay();
                        movesScroll.ScrollToBottom();
                        PushEndgameMessage();
                        return;
                    }
                }
            }
            else
            {
                System.Drawing.Point onSquare = GetCoordFromCanvasPoint(mousePos);
                if (mouseDownStartPoint is null || mouseDownStartPoint == onSquare)
                {
                    if (!squareHighlights.Add(onSquare))
                    {
                        _ = squareHighlights.Remove(onSquare);
                    }
                }
                else
                {
                    if (!lineHighlights.Add((mouseDownStartPoint.Value, onSquare)))
                    {
                        _ = lineHighlights.Remove((mouseDownStartPoint.Value, onSquare));
                    }
                }
            }
            UpdateGameDisplay();
        }

        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            NewGame(19, 19);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            string jsonPath = System.IO.Path.Join(AppDomain.CurrentDomain.BaseDirectory, "go-settings.json");
            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(config));
        }

        private void CustomGame_Click(object sender, RoutedEventArgs e)
        {
            CustomGame customDialog = new();
            _ = customDialog.ShowDialog();
            if (customDialog.GeneratedGame is not null)
            {
                game = customDialog.GeneratedGame;
                inProcessDeadStoneBoard = null;
                UpdateGameDisplay();
                PushEndgameMessage();
            }
        }

        private void SettingsCheckItem_Click(object sender, RoutedEventArgs e)
        {
            config.FlipBoard = flipBoardItem.IsChecked;
            config.HighlightIllegalMoves = highlightIllegalItem.IsChecked;
            config.UpdateEvalAfterBot = updateEvalAfterBotItem.IsChecked;

            UpdateGameDisplay();
        }

        private void FENCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(game.ToString());
        }

        private void CustomiseItem_Click(object sender, RoutedEventArgs e)
        {
            _ = new Customisation(config).ShowDialog();
            goBoardBackground.Background = new SolidColorBrush(config.BoardColor);
            UpdateGameDisplay();
        }

        private void UndoMove_Click(object sender, RoutedEventArgs e)
        {
            if (game.PreviousGameState is not null)
            {
                game = game.PreviousGameState;

                inProcessDeadStoneBoard = null;

                UpdateGameDisplay();
            }
        }

        private void passMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (!game.GameOver)
            {
                _ = game.PassTurn();

                squareHighlights.Clear();
                lineHighlights.Clear();
                UpdateGameDisplay();
                movesScroll.ScrollToBottom();
                PushEndgameMessage();
            }
            else if (game.AwaitingDeadStoneRemoval && inProcessDeadStoneBoard is not null)
            {
                game.Board = inProcessDeadStoneBoard;
                game.AwaitingDeadStoneRemoval = false;
                inProcessDeadStoneBoard = null;
                UpdateGameDisplay();
                PushEndgameMessage();
            }
        }
    }
}
