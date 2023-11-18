using System.Windows;
using System.Windows.Media;

namespace Go
{
    /// <summary>
    /// Interaction logic for Customisation.xaml
    /// </summary>
    public partial class Customisation : Window
    {
        public Settings Config { get; }

        private bool performRefresh = false;

        public Customisation(Settings config)
        {
            Config = config;
            InitializeComponent();

            boardPicker.SelectedColor = Config.BoardColor;
            lastMoveDestinationPicker.SelectedColor = Config.LastMoveDestinationColor;
            bestMoveDestinationPicker.SelectedColor = Config.BestMoveDestinationColor;
            blackPiecePicker.SelectedColor = Config.BlackPieceColor;
            whitePiecePicker.SelectedColor = Config.WhitePieceColor;
            illegalMovePicker.SelectedColor = Config.IllegalMoveColor;

            performRefresh = true;
        }

        private void Picker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (!performRefresh)
            {
                return;
            }
            Config.BoardColor = boardPicker.SelectedColor ?? default;
            Config.LastMoveDestinationColor = lastMoveDestinationPicker.SelectedColor ?? default;
            Config.BestMoveDestinationColor = bestMoveDestinationPicker.SelectedColor ?? default;
            Config.BlackPieceColor = blackPiecePicker.SelectedColor ?? default;
            Config.WhitePieceColor = whitePiecePicker.SelectedColor ?? default;
            Config.IllegalMoveColor = illegalMovePicker.SelectedColor ?? default;
        }
    }
}
