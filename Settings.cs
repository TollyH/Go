using Avalonia.Media;

namespace Go
{
    public class Settings
    {
        public bool FlipBoard { get; set; } = false;
        public bool HighlightIllegalMoves { get; set; } = false;
        public bool UpdateEvalAfterBot { get; set; } = true;

        public Color BoardColor { get; set; } = Color.FromRgb(249, 184, 83);
        public Color LastMoveDestinationColor { get; set; } = Colors.Cyan;
        public Color BestMoveDestinationColor { get; set; } = Colors.LightGreen;
        public Color BlackPieceColor { get; set; } = Colors.Black;
        public Color WhitePieceColor { get; set; } = Colors.White;
        public Color IllegalMoveColor { get; set; } = Colors.Red;
    }
}
