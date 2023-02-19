﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Shogi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ShogiGame game = new();
        private readonly Settings config;

        private Pieces.Piece? grabbedPiece = null;
        /// <summary>
        /// <see langword="true"/> if the player has selected a piece but isn't dragging it, <see langword="false"/> otherwise
        /// </summary>
        private bool highlightGrabbedMoves = false;

        private HashSet<System.Drawing.Point> squareHighlights = new();
        private HashSet<(System.Drawing.Point, System.Drawing.Point)> lineHighlights = new();
        private System.Drawing.Point? mouseDownStartPoint = null;

        private bool senteIsComputer = false;
        private bool goteIsComputer = false;

        private BoardAnalysis.PossibleMove? currentBestMove = null;
        private bool manuallyEvaluating = false;

        private readonly Dictionary<Pieces.Piece, Image> pieceViews = new();

        private CancellationTokenSource cancelMoveComputation = new();

        private double tileWidth;
        private double tileHeight;

        public MainWindow()
        {
            string jsonPath = System.IO.Path.Join(AppDomain.CurrentDomain.BaseDirectory, "shogi-settings.json");
            config = File.Exists(jsonPath)
                ? JsonConvert.DeserializeObject<Settings>(File.ReadAllText(jsonPath)) ?? new Settings()
                : new Settings();

            InitializeComponent();

            shogiBoardBackground.Background = new SolidColorBrush(config.BoardColor);
            moveListSymbolsItem.IsChecked = config.UseSymbolsOnMoveList;
            flipBoardItem.IsChecked = config.FlipBoard;
            updateEvalAfterBotItem.IsChecked = config.UpdateEvalAfterBot;
            foreach (MenuItem item in pieceSetItem.Items)
            {
                item.IsChecked = config.PieceSet == (string)item.Tag;
            }
        }

        public void UpdateGameDisplay()
        {
            if (game.AwaitingPromotionResponse)
            {
                return;
            }
            shogiGameCanvas.Children.Clear();
            pieceViews.Clear();

            bool boardFlipped = config.FlipBoard && ((!game.CurrentTurnSente && !goteIsComputer) || (senteIsComputer && !goteIsComputer));

            tileWidth = shogiGameCanvas.ActualWidth / game.Board.GetLength(0);
            tileHeight = shogiGameCanvas.ActualHeight / game.Board.GetLength(1);

            senteCaptures.Content = 0;
            senteCaptures.ToolTip = "";
            foreach (Pieces.Piece capturedPiece in game.CapturedPieces.Where(p => p.IsSente))
            {
                senteCaptures.Content = (int)senteCaptures.Content + 1;
                senteCaptures.ToolTip = (string)senteCaptures.ToolTip + capturedPiece.Name + "\r\n";
            }
            senteCaptures.ToolTip = ((string)senteCaptures.ToolTip).TrimEnd();

            goteCaptures.Content = 0;
            goteCaptures.ToolTip = "";
            foreach (Pieces.Piece capturedPiece in game.CapturedPieces.Where(p => !p.IsSente))
            {
                goteCaptures.Content = (int)goteCaptures.Content + 1;
                goteCaptures.ToolTip = (string)goteCaptures.ToolTip + capturedPiece.Name + "\r\n";
            }
            goteCaptures.ToolTip = ((string)goteCaptures.ToolTip).TrimEnd();

            if (currentBestMove is null && !manuallyEvaluating)
            {
                if (game.CurrentTurnSente && !senteIsComputer)
                {
                    senteEvaluation.Content = "?";
                }
                else if (!goteIsComputer)
                {
                    goteEvaluation.Content = "?";
                }
            }

            if (boardFlipped)
            {
                Grid.SetColumn(senteEvaluationView, 2);
                Grid.SetRow(senteEvaluationView, 0);
                Grid.SetColumn(goteEvaluationView, 0);
                Grid.SetRow(goteEvaluationView, 2);

                Grid.SetColumn(senteCapturesView, 0);
                Grid.SetRow(senteCapturesView, 0);
                Grid.SetColumn(goteCapturesView, 2);
                Grid.SetRow(goteCapturesView, 2);

                foreach (UIElement child in ranksLeft.Children)
                {
                    DockPanel.SetDock(child, Dock.Bottom);
                }
                foreach (UIElement child in ranksRight.Children)
                {
                    DockPanel.SetDock(child, Dock.Bottom);
                }
                foreach (UIElement child in filesTop.Children)
                {
                    DockPanel.SetDock(child, Dock.Right);
                }
                foreach (UIElement child in filesBottom.Children)
                {
                    DockPanel.SetDock(child, Dock.Right);
                }
            }
            else
            {
                Grid.SetColumn(senteEvaluationView, 0);
                Grid.SetRow(senteEvaluationView, 2);
                Grid.SetColumn(goteEvaluationView, 2);
                Grid.SetRow(goteEvaluationView, 0);

                Grid.SetColumn(senteCapturesView, 2);
                Grid.SetRow(senteCapturesView, 2);
                Grid.SetColumn(goteCapturesView, 0);
                Grid.SetRow(goteCapturesView, 0);

                foreach (UIElement child in ranksLeft.Children)
                {
                    DockPanel.SetDock(child, Dock.Top);
                }
                foreach (UIElement child in ranksRight.Children)
                {
                    DockPanel.SetDock(child, Dock.Top);
                }
                foreach (UIElement child in filesTop.Children)
                {
                    DockPanel.SetDock(child, Dock.Left);
                }
                foreach (UIElement child in filesBottom.Children)
                {
                    DockPanel.SetDock(child, Dock.Left);
                }
            }

            movesPanel.Children.Clear();
            for (int i = 0; i < game.MoveText.Count; i += 2)
            {
                string text = $"{(i / 2) + 1}. {game.MoveText[i]}";
                if (config.UseSymbolsOnMoveList)
                {
                    text = text.Replace('K', '♔').Replace('Q', '♕').Replace('R', '♖')
                        .Replace('B', '♗').Replace('N', '♘');
                }
                if (i + 1 < game.MoveText.Count)
                {
                    text += $" {game.MoveText[i + 1]}";
                    if (config.UseSymbolsOnMoveList)
                    {
                        text = text.Replace('K', '♚').Replace('Q', '♛').Replace('R', '♜')
                            .Replace('B', '♝').Replace('N', '♞');
                    }
                }
                _ = movesPanel.Children.Add(new Label()
                {
                    Content = text,
                    FontSize = 18
                });
            }

            GameState state = game.DetermineGameState();

            if (state is GameState.CheckMateSente or GameState.CheckMateGote)
            {
                System.Drawing.Point kingPosition = state == GameState.CheckMateSente ? game.SenteKing.Position : game.GoteKing.Position;
                Rectangle mateHighlight = new()
                {
                    Width = tileWidth,
                    Height = tileHeight,
                    Fill = new SolidColorBrush(config.CheckMateHighlightColor)
                };
                _ = shogiGameCanvas.Children.Add(mateHighlight);
                Canvas.SetBottom(mateHighlight, (boardFlipped ? 8 - kingPosition.Y : kingPosition.Y) * tileHeight);
                Canvas.SetLeft(mateHighlight, (boardFlipped ? 8 - kingPosition.X : kingPosition.X) * tileWidth);
            }

            if (game.Moves.Count > 0)
            {
                (System.Drawing.Point lastMoveSource, System.Drawing.Point lastMoveDestination) = game.Moves[^1];

                Rectangle sourceMoveHighlight = new()
                {
                    Width = tileWidth,
                    Height = tileHeight,
                    Fill = new SolidColorBrush(config.LastMoveSourceColor)
                };
                _ = shogiGameCanvas.Children.Add(sourceMoveHighlight);
                Canvas.SetBottom(sourceMoveHighlight, (boardFlipped ? 8 - lastMoveSource.Y : lastMoveSource.Y) * tileHeight);
                Canvas.SetLeft(sourceMoveHighlight, (boardFlipped ? 8 - lastMoveSource.X : lastMoveSource.X) * tileWidth);

                Rectangle destinationMoveHighlight = new()
                {
                    Width = tileWidth,
                    Height = tileHeight,
                    Fill = new SolidColorBrush(config.LastMoveDestinationColor)
                };
                _ = shogiGameCanvas.Children.Add(destinationMoveHighlight);
                Canvas.SetBottom(destinationMoveHighlight, (boardFlipped ? 8 - lastMoveDestination.Y : lastMoveDestination.Y) * tileHeight);
                Canvas.SetLeft(destinationMoveHighlight, (boardFlipped ? 8 - lastMoveDestination.X : lastMoveDestination.X) * tileWidth);

            }

            if (currentBestMove is not null
                // Prevent cases where there are no valid moves highlighting (0, 0)
                && currentBestMove.Value.Source != currentBestMove.Value.Destination)
            {
                Rectangle bestMoveSrcHighlight = new()
                {
                    Width = tileWidth,
                    Height = tileHeight,
                    Fill = new SolidColorBrush(config.BestMoveSourceColor)
                };
                _ = shogiGameCanvas.Children.Add(bestMoveSrcHighlight);
                Canvas.SetBottom(bestMoveSrcHighlight,
                    (boardFlipped ? 8 - currentBestMove.Value.Source.Y : currentBestMove.Value.Source.Y) * tileHeight);
                Canvas.SetLeft(bestMoveSrcHighlight,
                    (boardFlipped ? 8 - currentBestMove.Value.Source.X : currentBestMove.Value.Source.X) * tileWidth);

                Rectangle bestMoveDstHighlight = new()
                {
                    Width = tileWidth,
                    Height = tileHeight,
                    Fill = new SolidColorBrush(config.BestMoveDestinationColor)
                };
                _ = shogiGameCanvas.Children.Add(bestMoveDstHighlight);
                Canvas.SetBottom(bestMoveDstHighlight,
                    (boardFlipped ? 8 - currentBestMove.Value.Destination.Y : currentBestMove.Value.Destination.Y) * tileHeight);
                Canvas.SetLeft(bestMoveDstHighlight,
                    (boardFlipped ? 8 - currentBestMove.Value.Destination.X : currentBestMove.Value.Destination.X) * tileWidth);
            }

            if (grabbedPiece is not null && highlightGrabbedMoves)
            {
                foreach (System.Drawing.Point validMove in grabbedPiece.GetValidMoves(game.Board, true))
                {
                    Brush fillBrush;
                    if (game.Board[validMove.X, validMove.Y] is not null)
                    {
                        fillBrush = new SolidColorBrush(config.AvailableCaptureColor);
                    }
                    else
                    {
                        fillBrush = new SolidColorBrush(config.AvailableMoveColor);
                    }

                    Rectangle newRect = new()
                    {
                        Width = tileWidth,
                        Height = tileHeight,
                        Fill = fillBrush
                    };
                    _ = shogiGameCanvas.Children.Add(newRect);
                    Canvas.SetBottom(newRect, (boardFlipped ? 8 - validMove.Y : validMove.Y) * tileHeight);
                    Canvas.SetLeft(newRect, (boardFlipped ? 8 - validMove.X : validMove.X) * tileWidth);
                }
            }

            foreach (System.Drawing.Point square in squareHighlights)
            {
                Ellipse ellipse = new()
                {
                    Fill = new SolidColorBrush(config.SelectedPieceColor),
                    Opacity = 0.5,
                    Width = tileWidth * 0.8,
                    Height = tileHeight * 0.8
                };
                _ = shogiGameCanvas.Children.Add(ellipse);
                Canvas.SetBottom(ellipse, ((boardFlipped ? 8 - square.Y : square.Y) * tileHeight) + (tileHeight * 0.1));
                Canvas.SetLeft(ellipse, (boardFlipped ? 8 - square.X : square.X) * tileWidth + (tileWidth * 0.1));
            }

            foreach ((System.Drawing.Point lineStart, System.Drawing.Point lineEnd) in lineHighlights)
            {
                double arrowLength = Math.Min(tileWidth, tileHeight) / 4;
                Petzold.Media2D.ArrowLine line = new()
                {
                    Stroke = new SolidColorBrush(config.SelectedPieceColor),
                    Fill = new SolidColorBrush(config.SelectedPieceColor),
                    Opacity = 0.5,
                    StrokeThickness = 10,
                    ArrowLength = arrowLength,
                    ArrowAngle = 45,
                    IsArrowClosed = true,
                    X1 = (boardFlipped ? 8 - lineStart.X : lineStart.X) * tileWidth + (tileWidth / 2),
                    X2 = (boardFlipped ? 8 - lineEnd.X : lineEnd.X) * tileWidth + (tileWidth / 2),
                    Y1 = (boardFlipped ? lineStart.Y : 8 - lineStart.Y) * tileHeight + (tileHeight / 2),
                    Y2 = (boardFlipped ? lineEnd.Y : 8 - lineEnd.Y) * tileHeight + (tileHeight / 2)
                };
                _ = shogiGameCanvas.Children.Add(line);
            }

            for (int x = 0; x < game.Board.GetLength(0); x++)
            {
                for (int y = 0; y < game.Board.GetLength(1); y++)
                {
                    Pieces.Piece? piece = game.Board[x, y];
                    if (piece is not null)
                    {
                        Brush foregroundBrush;
                        if (piece is Pieces.King && ((piece.IsSente && state == GameState.CheckSente) || (!piece.IsSente && state == GameState.CheckGote)))
                        {
                            foregroundBrush = new SolidColorBrush(config.CheckedKingColor);
                        }
                        else if (highlightGrabbedMoves && piece == grabbedPiece)
                        {
                            foregroundBrush = new SolidColorBrush(config.SelectedPieceColor);
                        }
                        else
                        {
                            foregroundBrush = new SolidColorBrush(config.DefaultPieceColor);
                        }

                        Image newPiece = new()
                        {
                            Source = new BitmapImage(
                                new Uri($"pack://application:,,,/Pieces/{config.PieceSet}/{(piece.IsSente ? "Sente" : "Gote")}/{piece.Name}.png")),
                            Width = tileWidth,
                            Height = tileHeight,
                            RenderTransformOrigin = new Point(0.5, 0.5),
                            RenderTransform = new RotateTransform()
                            {
                                Angle = boardFlipped ? 180 : 0
                            }
                        };
                        RenderOptions.SetBitmapScalingMode(newPiece, BitmapScalingMode.HighQuality);
                        pieceViews[piece] = newPiece;
                        _ = shogiGameCanvas.Children.Add(newPiece);
                        Canvas.SetBottom(newPiece, (boardFlipped ? 8 - y : y) * tileHeight);
                        Canvas.SetLeft(newPiece, (boardFlipped ? 8 - x : x) * tileWidth);
                    }
                }
            }
        }

        private void UpdateCursor()
        {
            if (game.GameOver)
            {
                Mouse.OverrideCursor = Cursors.Arrow;
                return;
            }
            if (grabbedPiece is not null && !highlightGrabbedMoves)
            {
                Mouse.OverrideCursor = Cursors.ScrollAll;
                return;
            }
            Pieces.Piece? checkPiece = GetPieceAtCanvasPoint(Mouse.GetPosition(shogiGameCanvas));
            if (checkPiece is not null && ((checkPiece.IsSente && game.CurrentTurnSente && !senteIsComputer)
                || (!checkPiece.IsSente && !game.CurrentTurnSente && !goteIsComputer)))
            {
                Mouse.OverrideCursor = Cursors.Hand;
                return;
            }
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        /// <summary>
        /// If the game has ended, alert the user how it ended, otherwise do nothing
        /// </summary>
        private void PushEndgameMessage()
        {
            if (game.GameOver)
            {
                _ = MessageBox.Show(game.DetermineGameState() switch
                {
                    GameState.CheckMateSente => "Gote wins by checkmate!",
                    GameState.CheckMateGote => "Sente wins by checkmate!",
                    GameState.DrawStalemate => "Game drawn due to stalemate",
                    GameState.DrawInsufficientMaterial => "Game drawn as neither side has sufficient material to mate",
                    GameState.DrawThreeFold => "Game drawn as the same position has occured three times",
                    GameState.DrawFiftyMove => "Game drawn as fifty moves have occured without a capture or a pawn movement",
                    _ => "Game over"
                }, "Game Over", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void UpdateEvaluationMeter(BoardAnalysis.PossibleMove? bestMove, bool sente)
        {
            Label toUpdate = sente ? senteEvaluation : goteEvaluation;
            if (bestMove is null)
            {
                toUpdate.Content = "...";
                toUpdate.ToolTip = null;
                return;
            }

            if ((bestMove.Value.SenteMateLocated && !bestMove.Value.GoteMateLocated)
                || bestMove.Value.EvaluatedFutureValue == double.NegativeInfinity)
            {
                toUpdate.Content = $"-M{(int)Math.Ceiling(bestMove.Value.DepthToSenteMate / 2d)}";
            }
            else if ((bestMove.Value.GoteMateLocated && !bestMove.Value.SenteMateLocated)
                || bestMove.Value.EvaluatedFutureValue == double.PositiveInfinity)
            {
                toUpdate.Content = $"+M{(int)Math.Ceiling(bestMove.Value.DepthToGoteMate / 2d)}";
            }
            else
            {
                toUpdate.Content = bestMove.Value.EvaluatedFutureValue.ToString("+0.00;-0.00;0.00");
            }

            string convertedBestLine = "";
            ShogiGame moveStringGenerator = game.Clone();
            foreach ((System.Drawing.Point source, System.Drawing.Point destination, bool doPromotion) in bestMove.Value.BestLine)
            {
                _ = moveStringGenerator.MovePiece(source, destination, true, doPromotion);
                convertedBestLine += " " + moveStringGenerator.MoveText[^1];
            }
            toUpdate.ToolTip = convertedBestLine.Trim();
        }

        /// <summary>
        /// Get the best move according to either the built-in or external engine, depending on configuration
        /// </summary>
        private async Task<BoardAnalysis.PossibleMove> GetEngineMove(CancellationToken cancellationToken)
        {
            BoardAnalysis.PossibleMove? bestMove = null;
            bestMove ??= await BoardAnalysis.EstimateBestPossibleMove(game, 4, cancellationToken);
            return bestMove.Value;
        }

        /// <summary>
        /// Perform a computer move if necessary
        /// </summary>
        private async Task CheckComputerMove()
        {
            while (!game.GameOver && ((game.CurrentTurnSente && senteIsComputer) || (!game.CurrentTurnSente && goteIsComputer)))
            {
                CancellationToken cancellationToken = cancelMoveComputation.Token;
                if (config.UpdateEvalAfterBot)
                {
                    UpdateEvaluationMeter(null, game.CurrentTurnSente);
                }
                BoardAnalysis.PossibleMove bestMove = await GetEngineMove(cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                _ = game.MovePiece(bestMove.Source, bestMove.Destination, true, doPromotion: bestMove.DoPromotion);
                UpdateGameDisplay();
                movesScroll.ScrollToBottom();
                if (config.UpdateEvalAfterBot)
                {
                    // Turn has been inverted already but we have value for the now old turn
                    UpdateEvaluationMeter(bestMove, !game.CurrentTurnSente);
                }
                PushEndgameMessage();
            }
        }

        private System.Drawing.Point GetCoordFromCanvasPoint(Point position)
        {
            bool boardFlipped = config.FlipBoard && ((!game.CurrentTurnSente && !goteIsComputer) || (senteIsComputer && !goteIsComputer));
            // Canvas coordinates are relative to top-left, whereas shogi's are from bottom-left, so y is inverted
            return new System.Drawing.Point((int)((boardFlipped ? shogiGameCanvas.ActualWidth - position.X : position.X) / tileWidth),
                (int)((!boardFlipped ? shogiGameCanvas.ActualHeight - position.Y : position.Y) / tileHeight));
        }

        private Pieces.Piece? GetPieceAtCanvasPoint(Point position)
        {
            if (position.X < 0 || position.Y < 0
                || position.X > shogiGameCanvas.ActualWidth || position.Y > shogiGameCanvas.ActualHeight)
            {
                return null;
            }
            System.Drawing.Point coord = GetCoordFromCanvasPoint(position);
            return coord.X < 0 || coord.Y < 0 || coord.X >= game.Board.GetLength(0) || coord.Y >= game.Board.GetLength(1)
                ? null
                    : game.Board[coord.X, coord.Y];
        }

        private async Task NewGame()
        {
            cancelMoveComputation.Cancel();
            cancelMoveComputation = new CancellationTokenSource();
            game = new ShogiGame();
            currentBestMove = null;
            manuallyEvaluating = false;
            grabbedPiece = null;
            highlightGrabbedMoves = false;
            senteEvaluation.Content = "?";
            goteEvaluation.Content = "?";
            UpdateGameDisplay();
            UpdateCursor();
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
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (grabbedPiece is not null && !highlightGrabbedMoves)
            {
                Canvas.SetBottom(pieceViews[grabbedPiece], shogiGameCanvas.ActualHeight - Mouse.GetPosition(shogiGameCanvas).Y - (tileHeight / 2));
                Canvas.SetLeft(pieceViews[grabbedPiece], Mouse.GetPosition(shogiGameCanvas).X - (tileWidth / 2));
            }
            UpdateCursor();
        }

        private async void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = Mouse.GetPosition(shogiGameCanvas);
            if (e.ChangedButton == MouseButton.Left)
            {
                squareHighlights.Clear();
                lineHighlights.Clear();
                if (game.GameOver)
                {
                    return;
                }

                // If a piece is selected, try to move it
                if (grabbedPiece is not null && highlightGrabbedMoves)
                {
                    System.Drawing.Point destination = GetCoordFromCanvasPoint(mousePos);
                    bool success = game.MovePiece(grabbedPiece.Position, destination, doPromotion: null);
                    if (success)
                    {
                        highlightGrabbedMoves = false;
                        grabbedPiece = null;
                        currentBestMove = null;
                        UpdateCursor();
                        UpdateGameDisplay();
                        movesScroll.ScrollToBottom();
                        PushEndgameMessage();
                        await CheckComputerMove();
                        return;
                    }
                }

                highlightGrabbedMoves = false;
                Pieces.Piece? toCheck = GetPieceAtCanvasPoint(mousePos);
                if (toCheck is not null)
                {
                    if ((toCheck.IsSente && game.CurrentTurnSente && !senteIsComputer)
                        || (!toCheck.IsSente && !game.CurrentTurnSente && !goteIsComputer))
                    {
                        grabbedPiece = toCheck;
                        manuallyEvaluating = false;
                        cancelMoveComputation.Cancel();
                        cancelMoveComputation = new CancellationTokenSource();
                    }
                    else
                    {
                        grabbedPiece = null;
                    }
                }
                else
                {
                    grabbedPiece = null;
                }
            }
            else
            {
                if (mousePos.X < 0 || mousePos.Y < 0
                || mousePos.X > shogiGameCanvas.ActualWidth || mousePos.Y > shogiGameCanvas.ActualHeight)
                {
                    return;
                }
                mouseDownStartPoint = GetCoordFromCanvasPoint(mousePos);
            }
            UpdateGameDisplay();
            UpdateCursor();
        }

        private async void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (game.GameOver)
                {
                    return;
                }
                if (grabbedPiece is not null)
                {
                    System.Drawing.Point destination = GetCoordFromCanvasPoint(Mouse.GetPosition(shogiGameCanvas));
                    if (destination == grabbedPiece.Position)
                    {
                        highlightGrabbedMoves = true;
                        UpdateCursor();
                        UpdateGameDisplay();
                        return;
                    }
                    bool success = game.MovePiece(grabbedPiece.Position, destination, doPromotion: null);
                    if (success)
                    {
                        grabbedPiece = null;
                        highlightGrabbedMoves = false;
                        currentBestMove = null;
                        UpdateCursor();
                        UpdateGameDisplay();
                        movesScroll.ScrollToBottom();
                        PushEndgameMessage();
                        await CheckComputerMove();
                        return;
                    }
                    else
                    {
                        highlightGrabbedMoves = true;
                    }
                }
            }
            else
            {
                Point mousePos = Mouse.GetPosition(shogiGameCanvas);
                if (mousePos.X < 0 || mousePos.Y < 0
                || mousePos.X > shogiGameCanvas.ActualWidth || mousePos.Y > shogiGameCanvas.ActualHeight)
                {
                    return;
                }
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
            UpdateCursor();
            UpdateGameDisplay();
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            if (grabbedPiece is not null)
            {
                highlightGrabbedMoves = true;
            }
            UpdateGameDisplay();
        }

        private async void evaluation_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (currentBestMove is not null || (game.CurrentTurnSente && senteIsComputer)
                || (!game.CurrentTurnSente && goteIsComputer))
            {
                return;
            }
            manuallyEvaluating = true;
            grabbedPiece = null;
            highlightGrabbedMoves = false;
            UpdateEvaluationMeter(null, game.CurrentTurnSente);
            UpdateGameDisplay();
            UpdateCursor();

            CancellationToken cancellationToken = cancelMoveComputation.Token;
            BoardAnalysis.PossibleMove bestMove = await GetEngineMove(cancellationToken);
            
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            UpdateEvaluationMeter(bestMove, game.CurrentTurnSente);
            currentBestMove = bestMove;
            UpdateGameDisplay();
            manuallyEvaluating = false;
        }

        private async void NewGame_Click(object sender, RoutedEventArgs e)
        {
            senteIsComputer = false;
            goteIsComputer = false;
            await NewGame();
        }

        private async void NewGameCpuSente_Click(object sender, RoutedEventArgs e)
        {
            senteIsComputer = false;
            goteIsComputer = true;
            await NewGame();
        }

        private async void NewGameCpuGote_Click(object sender, RoutedEventArgs e)
        {
            senteIsComputer = true;
            goteIsComputer = false;
            await NewGame();
        }

        private async void NewGameCpuOnly_Click(object sender, RoutedEventArgs e)
        {
            senteIsComputer = true;
            goteIsComputer = true;
            await NewGame();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            cancelMoveComputation.Cancel();
            string jsonPath = System.IO.Path.Join(AppDomain.CurrentDomain.BaseDirectory, "shogi-settings.json");
            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(config));
        }

        private async void PGNExport_Click(object sender, RoutedEventArgs e)
        {
            manuallyEvaluating = false;
            cancelMoveComputation.Cancel();
            cancelMoveComputation = new CancellationTokenSource();
            _ = new PGNExport(game, senteIsComputer, goteIsComputer).ShowDialog();
            await CheckComputerMove();
        }

        private async void CustomGame_Click(object sender, RoutedEventArgs e)
        {
            manuallyEvaluating = false;
            cancelMoveComputation.Cancel();
            cancelMoveComputation = new CancellationTokenSource();
            CustomGame customDialog = new(config);
            _ = customDialog.ShowDialog();
            if (customDialog.GeneratedGame is not null)
            {
                game = customDialog.GeneratedGame;
                senteIsComputer = customDialog.SenteIsComputer;
                goteIsComputer = customDialog.GoteIsComputer;
                grabbedPiece = null;
                highlightGrabbedMoves = false;
                currentBestMove = null;
                senteEvaluation.Content = "?";
                goteEvaluation.Content = "?";
                UpdateGameDisplay();
                PushEndgameMessage();
            }
            await CheckComputerMove();
        }

        private void SettingsCheckItem_Click(object sender, RoutedEventArgs e)
        {
            config.UseSymbolsOnMoveList = moveListSymbolsItem.IsChecked;
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
            shogiBoardBackground.Background = new SolidColorBrush(config.BoardColor);
            UpdateGameDisplay();
        }

        private void PieceSetItem_Click(object sender, RoutedEventArgs e)
        {
            string chosenSet = (string)((MenuItem)sender).Tag;
            config.PieceSet = chosenSet;
            foreach (MenuItem item in pieceSetItem.Items)
            {
                item.IsChecked = chosenSet == (string)item.Tag;
            }
            UpdateGameDisplay();
        }
    }
}
