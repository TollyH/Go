﻿using Newtonsoft.Json;
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
        private GoGame game = new(19, 19, ScoringSystem.Area);
        private bool?[,]? inProcessDeadStoneBoard = null;
        private readonly Settings config;

        private readonly HashSet<System.Drawing.Point> squareHighlights = new();
        private readonly HashSet<(System.Drawing.Point, System.Drawing.Point)> lineHighlights = new();
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
            highlightIllegalItem.IsChecked = config.HighlightIllegalMoves;
            updateEvalAfterBotItem.IsChecked = config.UpdateEvalAfterBot;
        }

        public void UpdateGameDisplay()
        {
            goGameCanvas.Children.Clear();
            passMenuItem.IsEnabled = !game.GameOver || (game.AwaitingDeadStoneRemoval && inProcessDeadStoneBoard is not null);

            bool boardFlipped = config.FlipBoard && ((!game.CurrentTurnBlack && !whiteIsComputer) || (blackIsComputer && !whiteIsComputer));

            int boardWidth = game.Board.GetLength(0);
            int boardHeight = game.Board.GetLength(1);

            tileWidth = goGameCanvas.ActualWidth / boardWidth;
            tileHeight = goGameCanvas.ActualHeight / boardHeight;

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

            if (currentBestMove is not null)
            {
                if (currentBestMove.Value.Destination.X >= 0)
                {
                    passMenuItem.Background = null;
                    Rectangle bestMoveDstHighlight = new()
                    {
                        Width = tileWidth,
                        Height = tileHeight,
                        Fill = new SolidColorBrush(config.BestMoveDestinationColor)
                    };
                    _ = goGameCanvas.Children.Add(bestMoveDstHighlight);
                    Canvas.SetBottom(bestMoveDstHighlight,
                        (boardFlipped
                            ? boardMaxY - currentBestMove.Value.Destination.Y
                            : currentBestMove.Value.Destination.Y) * tileHeight);
                    Canvas.SetLeft(bestMoveDstHighlight,
                        (boardFlipped
                            ? boardMaxX - currentBestMove.Value.Destination.X
                            : currentBestMove.Value.Destination.X) * tileWidth);
                }
                else
                {
                    passMenuItem.Background = new SolidColorBrush(config.BestMoveDestinationColor);
                }
            }
            else
            {
                passMenuItem.Background = null;
            }

            // Used to highlight territory when the game is over
            bool?[,] scoredTerritory;
            if (game is { GameOver: true, AwaitingDeadStoneRemoval: false, CurrentScoring: ScoringSystem.Area or ScoringSystem.Territory })
            {
                scoredTerritory =
                    BoardAnalysis.FillSurroundedAreas(game.Board, game.CurrentScoring == ScoringSystem.Area);
            }
            else
            {
                scoredTerritory = new bool?[boardWidth, boardHeight];
            }

            for (int x = 0; x < boardWidth; x++)
            {
                for (int y = 0; y < boardHeight; y++)
                {
                    bool? stone = game.Board[x, y];
                    bool? scoredStone = scoredTerritory[x, y];
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
                            Fill = stone.Value ? new SolidColorBrush(config.BlackPieceColor) : new SolidColorBrush(config.WhitePieceColor),
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
                            Fill = scoredStone.Value ? new SolidColorBrush(config.BlackPieceColor) : new SolidColorBrush(config.WhitePieceColor)
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
                        "Both players have passed their turn." + Environment.NewLine +
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
                    double finalScore = BoardAnalysis.CalculateGameValue(game, game.CurrentScoring);
                    string message = finalScore switch
                    {
                        > 0 => $"Black wins by {finalScore} point{(finalScore != 1 ? "s" : "")}.",
                        < 0 => $"White wins by {-finalScore} point{(finalScore != -1 ? "s" : "")}.",
                        _ => "The game is a draw"
                    };
                    _ = MessageBox.Show(message, "Game over", MessageBoxButton.OK, MessageBoxImage.Information);
                }
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
            GoGame moveStringGenerator = game.Clone(false);
            foreach (System.Drawing.Point destination in bestMove.Value.BestLine)
            {
                if (destination.X == -1 || destination.Y == -1)
                {
                    _ = moveStringGenerator.PassTurn();
                }
                else
                {
                    _ = moveStringGenerator.PlaceStone(destination, true);
                }
                convertedBestLine += " " + moveStringGenerator.MoveText[^1];
            }
            toUpdate.ToolTip = convertedBestLine.Trim();
        }

        /// <summary>
        /// Get the best move according to either the built-in or external engine, depending on configuration
        /// </summary>
        private async Task<BoardAnalysis.PossibleMove?> GetEngineMove(CancellationToken cancellationToken)
        {
            BoardAnalysis.PossibleMove? bestMove = await BoardAnalysis.EstimateBestPossibleMove(game, 2, true, cancellationToken);
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

                if (bestMove.Value.Destination.X == -1 || bestMove.Value.Destination.Y == -1)
                {
                    _ = game.PassTurn();
                }
                else
                {
                    _ = game.PlaceStone(bestMove.Value.Destination, true);
                }
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
            game = new GoGame(boardWidth, boardHeight, ScoringSystem.Area);
            inProcessDeadStoneBoard = null;
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
                        currentBestMove = null;
                        UpdateGameDisplay();
                        movesScroll.ScrollToBottom();
                        PushEndgameMessage();
                        await CheckComputerMove();
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

        private async void evaluation_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (currentBestMove is not null || game.GameOver
                || (game.CurrentTurnBlack && blackIsComputer) || (!game.CurrentTurnBlack && whiteIsComputer))
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
                inProcessDeadStoneBoard = null;
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

                inProcessDeadStoneBoard = null;
                currentBestMove = null;
                blackEvaluation.Content = "?";
                whiteEvaluation.Content = "?";
                manuallyEvaluating = false;
                cancelMoveComputation.Cancel();
                cancelMoveComputation = new CancellationTokenSource();

                UpdateGameDisplay();
                await CheckComputerMove();
            }
        }

        private async void passMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (!game.GameOver && ((game.CurrentTurnBlack && !blackIsComputer) || (!game.CurrentTurnBlack && !whiteIsComputer)))
            {
                _ = game.PassTurn();

                squareHighlights.Clear();
                lineHighlights.Clear();
                currentBestMove = null;
                UpdateGameDisplay();
                movesScroll.ScrollToBottom();
                PushEndgameMessage();
                await CheckComputerMove();
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
