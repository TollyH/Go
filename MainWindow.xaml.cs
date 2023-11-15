using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
        private GoGame game = new(19, 19);
        private readonly Settings config;

        private HashSet<System.Drawing.Point> squareHighlights = new();
        private HashSet<(System.Drawing.Point, System.Drawing.Point)> lineHighlights = new();
        private System.Drawing.Point? mouseDownStartPoint = null;

        private bool blackIsComputer = false;
        private bool whiteIsComputer = false;

        private BoardAnalysis.PossibleMove? currentBestMove = null;
        private bool manuallyEvaluating = false;

        private CancellationTokenSource cancelMoveComputation = new();

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
            updateEvalAfterBotItem.IsChecked = config.UpdateEvalAfterBot;
        }

        public void UpdateGameDisplay()
        {
            goGameCanvas.Children.Clear();

            bool boardFlipped = config.FlipBoard && ((!game.CurrentTurnBlack && !whiteIsComputer) || (blackIsComputer && !whiteIsComputer));

            tileWidth = goGameCanvas.ActualWidth / game.Board.GetLength(0);
            tileHeight = goGameCanvas.ActualHeight / game.Board.GetLength(1);

            blackCaptures.Content = game.BlackCaptures;
            whiteCaptures.Content = game.WhiteCaptures;

            if (currentBestMove is null && !manuallyEvaluating)
            {
                if (game.CurrentTurnBlack && !blackIsComputer)
                {
                    blackEvaluation.Content = "?";
                }
                else if (!whiteIsComputer)
                {
                    whiteEvaluation.Content = "?";
                }
            }

            if (boardFlipped)
            {
                Grid.SetColumn(blackEvaluationView, 2);
                Grid.SetRow(blackEvaluationView, 0);
                Grid.SetColumn(whiteEvaluationView, 0);
                Grid.SetRow(whiteEvaluationView, 2);

                Grid.SetColumn(blackCapturesView, 0);
                Grid.SetRow(blackCapturesView, 0);
                Grid.SetColumn(whiteCapturesView, 2);
                Grid.SetRow(whiteCapturesView, 2);
            }
            else
            {
                Grid.SetColumn(blackEvaluationView, 0);
                Grid.SetRow(blackEvaluationView, 2);
                Grid.SetColumn(whiteEvaluationView, 2);
                Grid.SetRow(whiteEvaluationView, 0);

                Grid.SetColumn(blackCapturesView, 2);
                Grid.SetRow(blackCapturesView, 2);
                Grid.SetColumn(whiteCapturesView, 0);
                Grid.SetRow(whiteCapturesView, 0);
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

            int boardMaxY = game.Board.GetLength(1) - 1;
            int boardMaxX = game.Board.GetLength(0) - 1;

            // TODO: Upon game over, show who surrounded territory belongs to with smaller dots

            if (game.Moves.Count > 0)
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

            if (currentBestMove is not null)
            {
                Rectangle bestMoveDstHighlight = new()
                {
                    Width = tileWidth,
                    Height = tileHeight,
                    Fill = new SolidColorBrush(config.BestMoveDestinationColor)
                };
                _ = goGameCanvas.Children.Add(bestMoveDstHighlight);
                Canvas.SetBottom(bestMoveDstHighlight,
                    (boardFlipped ? boardMaxY - currentBestMove.Value.Destination.Y : currentBestMove.Value.Destination.Y) * tileHeight);
                Canvas.SetLeft(bestMoveDstHighlight,
                    (boardFlipped ? boardMaxX - currentBestMove.Value.Destination.X : currentBestMove.Value.Destination.X) * tileWidth);
            }

            for (int x = 0; x < game.Board.GetLength(0); x++)
            {
                for (int y = 0; y < game.Board.GetLength(1); y++)
                {
                    bool? stone = game.Board[x, y];
                    if (stone is not null)
                    {
                        Ellipse newStone = new()
                        {
                            Width = tileWidth,
                            Height = tileHeight,
                            Fill = stone.Value ? Brushes.Black : Brushes.White
                        };
                        _ = goGameCanvas.Children.Add(newStone);
                        Canvas.SetBottom(newStone, (boardFlipped ? boardMaxY - y : y) * tileHeight);
                        Canvas.SetLeft(newStone, (boardFlipped ? boardMaxX - x : x) * tileWidth);
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

            for (int x = 0; x < game.Board.GetLength(0); x++)
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

            for (int y = 0; y < game.Board.GetLength(1); y++)
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
        }

        /// <summary>
        /// If the game has ended, alert the user how it ended, otherwise do nothing
        /// </summary>
        private void PushEndgameMessage()
        {
            if (game.GameOver)
            {
                // TODO: Determine winner and show how much they won by
                _ = MessageBox.Show("Game over", "Game Over", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void UpdateEvaluationMeter(BoardAnalysis.PossibleMove? bestMove, bool black)
        {
            Label toUpdate = black ? blackEvaluation : whiteEvaluation;
            if (bestMove is null)
            {
                toUpdate.Content = "...";
                toUpdate.ToolTip = null;
                return;
            }

            toUpdate.Content = bestMove.Value.EvaluatedFutureValue.ToString("+0.00;-0.00;0.00");

            string convertedBestLine = "";
            GoGame moveStringGenerator = game.Clone();
            foreach (System.Drawing.Point destination in bestMove.Value.BestLine)
            {
                _ = moveStringGenerator.PlaceStone(destination, true);
                convertedBestLine += " " + moveStringGenerator.MoveText[^1];
            }
            toUpdate.ToolTip = convertedBestLine.Trim();
        }

        /// <summary>
        /// Get the best move according to either the built-in or external engine, depending on configuration
        /// </summary>
        private async Task<BoardAnalysis.PossibleMove?> GetEngineMove(CancellationToken cancellationToken)
        {
            BoardAnalysis.PossibleMove? bestMove = await BoardAnalysis.EstimateBestPossibleMove(game, 4, cancellationToken);
            return bestMove;
        }

        /// <summary>
        /// Perform a computer move if necessary
        /// </summary>
        private async Task CheckComputerMove()
        {
            while (!game.GameOver && ((game.CurrentTurnBlack && blackIsComputer) || (!game.CurrentTurnBlack && whiteIsComputer)))
            {
                CancellationToken cancellationToken = cancelMoveComputation.Token;
                if (config.UpdateEvalAfterBot)
                {
                    UpdateEvaluationMeter(null, game.CurrentTurnBlack);
                }
                BoardAnalysis.PossibleMove? bestMove = await GetEngineMove(cancellationToken);
                if (bestMove is null || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                _ = game.PlaceStone(bestMove.Value.Destination, true);
                UpdateGameDisplay();
                movesScroll.ScrollToBottom();
                if (config.UpdateEvalAfterBot)
                {
                    // Turn has been inverted already but we have value for the now old turn
                    UpdateEvaluationMeter(bestMove, !game.CurrentTurnBlack);
                }
                PushEndgameMessage();
            }
        }

        private System.Drawing.Point GetCoordFromCanvasPoint(Point position)
        {
            bool boardFlipped = config.FlipBoard && ((!game.CurrentTurnBlack && !whiteIsComputer) || (blackIsComputer && !whiteIsComputer));
            // Canvas coordinates are relative to top-left, whereas go's are from bottom-left, so y is inverted
            return new System.Drawing.Point((int)((boardFlipped ? goGameCanvas.ActualWidth - position.X : position.X) / tileWidth),
                (int)((!boardFlipped ? goGameCanvas.ActualHeight - position.Y : position.Y) / tileHeight));
        }

        private async Task NewGame(int boardWidth, int boardHeight)
        {
            cancelMoveComputation.Cancel();
            cancelMoveComputation = new CancellationTokenSource();
            game = new GoGame(boardWidth, boardHeight);
            currentBestMove = null;
            manuallyEvaluating = false;
            blackEvaluation.Content = "?";
            whiteEvaluation.Content = "?";
            UpdateGameDisplay();
            await CheckComputerMove();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateGameDisplay();
            await CheckComputerMove();
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

        private async void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = Mouse.GetPosition(goGameCanvas);
            if (mousePos.X < 0 || mousePos.Y < 0
                || mousePos.X > goGameCanvas.ActualWidth || mousePos.Y > goGameCanvas.ActualHeight)
            {
                return;
            }
            if (e.ChangedButton == MouseButton.Left)
            {
                if (game.GameOver)
                {
                    return;
                }
                System.Drawing.Point destination = GetCoordFromCanvasPoint(mousePos);
                bool success = game.PlaceStone(destination);
                if (success)
                {
                    currentBestMove = null;
                    UpdateGameDisplay();
                    movesScroll.ScrollToBottom();
                    PushEndgameMessage();
                    await CheckComputerMove();
                    return;
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

        private async void evaluation_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (currentBestMove is not null || (game.CurrentTurnBlack && blackIsComputer)
                || (!game.CurrentTurnBlack && whiteIsComputer))
            {
                return;
            }
            manuallyEvaluating = true;
            UpdateEvaluationMeter(null, game.CurrentTurnBlack);
            UpdateGameDisplay();

            CancellationToken cancellationToken = cancelMoveComputation.Token;
            BoardAnalysis.PossibleMove? bestMove = await GetEngineMove(cancellationToken);
            
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            UpdateEvaluationMeter(bestMove, game.CurrentTurnBlack);
            currentBestMove = bestMove;
            UpdateGameDisplay();
            manuallyEvaluating = false;
        }

        private async void NewGame_Click(object sender, RoutedEventArgs e)
        {
            blackIsComputer = false;
            whiteIsComputer = false;
            await NewGame(19, 19);
        }

        private async void NewGameCpuBlack_Click(object sender, RoutedEventArgs e)
        {
            blackIsComputer = false;
            whiteIsComputer = true;
            await NewGame(19, 19);
        }

        private async void NewGameCpuWhite_Click(object sender, RoutedEventArgs e)
        {
            blackIsComputer = true;
            whiteIsComputer = false;
            await NewGame(19, 19);
        }

        private async void NewGameCpuOnly_Click(object sender, RoutedEventArgs e)
        {
            blackIsComputer = true;
            whiteIsComputer = true;
            await NewGame(19, 19);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            cancelMoveComputation.Cancel();
            string jsonPath = System.IO.Path.Join(AppDomain.CurrentDomain.BaseDirectory, "go-settings.json");
            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(config));
        }

        private async void CustomGame_Click(object sender, RoutedEventArgs e)
        {
            manuallyEvaluating = false;
            cancelMoveComputation.Cancel();
            cancelMoveComputation = new CancellationTokenSource();
            CustomGame customDialog = new();
            _ = customDialog.ShowDialog();
            if (customDialog.GeneratedGame is not null)
            {
                game = customDialog.GeneratedGame;
                blackIsComputer = customDialog.BlackIsComputer;
                whiteIsComputer = customDialog.WhiteIsComputer;
                currentBestMove = null;
                blackEvaluation.Content = "?";
                whiteEvaluation.Content = "?";
                UpdateGameDisplay();
                PushEndgameMessage();
            }
            await CheckComputerMove();
        }

        private void SettingsCheckItem_Click(object sender, RoutedEventArgs e)
        {
            config.FlipBoard = flipBoardItem.IsChecked;
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

        private async void UndoMove_Click(object sender, RoutedEventArgs e)
        {
            if (game.PreviousGameState is not null
                && ((game.CurrentTurnBlack && !blackIsComputer) || (!game.CurrentTurnBlack && !whiteIsComputer)))
            {
                game = game.PreviousGameState;
                if (blackIsComputer || whiteIsComputer)
                {
                    // Reverse two moves if the opponent is computer controlled
                    game = game.PreviousGameState!;
                }
                UpdateGameDisplay();
                await CheckComputerMove();
            }
        }
    }
}
