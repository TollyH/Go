using Avalonia.Controls;

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

            boardPicker.Color = Config.BoardColor;
            lastMoveDestinationPicker.Color = Config.LastMoveDestinationColor;
            bestMoveDestinationPicker.Color = Config.BestMoveDestinationColor;
            blackPiecePicker.Color = Config.BlackPieceColor;
            whitePiecePicker.Color = Config.WhitePieceColor;
            illegalMovePicker.Color = Config.IllegalMoveColor;

            performRefresh = true;
        }

        private void Picker_ColorChanged(object sender, ColorChangedEventArgs e)
        {
            if (!performRefresh)
            {
                return;
            }
            Config.BoardColor = boardPicker.Color;
            Config.LastMoveDestinationColor = lastMoveDestinationPicker.Color;
            Config.BestMoveDestinationColor = bestMoveDestinationPicker.Color;
            Config.BlackPieceColor = blackPiecePicker.Color;
            Config.WhitePieceColor = whitePiecePicker.Color;
            Config.IllegalMoveColor = illegalMovePicker.Color;
        }
    }
}
