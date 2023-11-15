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

            // TODO: Customizable board size (with UI)
            Board = new bool?[19, 19];
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

            for (int x = 0; x < Board.GetLength(0); x++)
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

            for (int y = 0; y < Board.GetLength(1); y++)
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

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            BlackIsComputer = computerSelectBlack.IsChecked ?? false;
            WhiteIsComputer = computerSelectWhite.IsChecked ?? false;
            bool currentTurnBlack = turnSelectBlack.IsChecked ?? false;
            GeneratedGame = new GoGame(Board, currentTurnBlack, false,
                new List<System.Drawing.Point>(), new List<string>(),
                0, 0, new HashSet<string>(), null, null);
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
    }
}
