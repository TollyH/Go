using Newtonsoft.Json;
using System;
using System.Windows.Media;

namespace Go
{
    [Serializable]
    public class Settings
    {
        public bool FlipBoard { get; set; }
        public bool UpdateEvalAfterBot { get; set; }

        public Color BoardColor { get; set; }
        public Color LastMoveDestinationColor { get; set; }
        public Color BestMoveDestinationColor { get; set; }

        public Settings()
        {
            FlipBoard = false;
            UpdateEvalAfterBot = true;

            BoardColor = Color.FromRgb(249, 184, 83);
            LastMoveDestinationColor = Brushes.Cyan.Color;
            BestMoveDestinationColor = Brushes.Green.Color;
        }

        [JsonConstructor]
        public Settings(bool flipBoard, bool updateEvalAfterBot,
            Color boardColor, Color lastMoveDestinationColor, Color bestMoveDestinationColor)
        {
            FlipBoard = flipBoard;
            UpdateEvalAfterBot = updateEvalAfterBot;
            BoardColor = boardColor;
            LastMoveDestinationColor = lastMoveDestinationColor;
            BestMoveDestinationColor = bestMoveDestinationColor;
        }
    }
}
