using Newtonsoft.Json;
using System;
using System.Windows.Media;

namespace Go
{
    [Serializable]
    public class Settings
    {
        public bool FlipBoard { get; set; }
        public bool HighlightIllegalMoves { get; set; }
        public bool UpdateEvalAfterBot { get; set; }

        public Color BoardColor { get; set; }
        public Color LastMoveDestinationColor { get; set; }
        public Color BestMoveDestinationColor { get; set; }
        public Color BlackPieceColor { get; set; }
        public Color WhitePieceColor { get; set; }
        public Color IllegalMoveColor { get; set; }

        public Settings()
        {
            FlipBoard = false;
            HighlightIllegalMoves = false;
            UpdateEvalAfterBot = true;

            BoardColor = Color.FromRgb(249, 184, 83);
            LastMoveDestinationColor = Colors.Cyan;
            BestMoveDestinationColor = Colors.LightGreen;
            BlackPieceColor = Colors.Black;
            WhitePieceColor = Colors.White;
            IllegalMoveColor = Colors.Red;
        }

        [JsonConstructor]
        public Settings(bool flipBoard, bool highlightIllegalMoves, bool updateEvalAfterBot,
            Color boardColor, Color lastMoveDestinationColor, Color bestMoveDestinationColor,
            Color blackPieceColor, Color whitePieceColor, Color illegalMoveColor)
        {
            FlipBoard = flipBoard;
            HighlightIllegalMoves = highlightIllegalMoves;
            UpdateEvalAfterBot = updateEvalAfterBot;
            BoardColor = boardColor;
            LastMoveDestinationColor = lastMoveDestinationColor;
            BestMoveDestinationColor = bestMoveDestinationColor;
            BlackPieceColor = blackPieceColor;
            WhitePieceColor = whitePieceColor;
            IllegalMoveColor = illegalMoveColor;
        }
    }
}
