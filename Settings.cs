﻿using Newtonsoft.Json;
using System;
using System.Windows.Media;

namespace Shogi
{
    [Serializable]
    public class Settings
    {
        public bool UseSymbolsOnMoveList { get; set; }
        public bool FlipBoard { get; set; }
        public bool UpdateEvalAfterBot { get; set; }

        public Color BoardColor { get; set; }
        public Color DefaultPieceColor { get; set; }
        public Color CheckedKingColor { get; set; }
        public Color SelectedPieceColor { get; set; }
        public Color CheckMateHighlightColor { get; set; }
        public Color LastMoveSourceColor { get; set; }
        public Color LastMoveDestinationColor { get; set; }
        public Color BestMoveSourceColor { get; set; }
        public Color BestMoveDestinationColor { get; set; }
        public Color AvailableMoveColor { get; set; }
        public Color AvailableCaptureColor { get; set; }
        public Color AvailableEnPassantColor { get; set; }
        public Color AvailableCastleColor { get; set; }

        public Settings()
        {
            UseSymbolsOnMoveList = false;
            FlipBoard = false;
            UpdateEvalAfterBot = true;

            BoardColor = Color.FromRgb(249, 184, 83);
            DefaultPieceColor = Brushes.Black.Color;
            CheckedKingColor = Brushes.Red.Color;
            SelectedPieceColor = Brushes.Blue.Color;
            CheckMateHighlightColor = Brushes.IndianRed.Color;
            LastMoveSourceColor = Brushes.CadetBlue.Color;
            LastMoveDestinationColor = Brushes.Cyan.Color;
            BestMoveSourceColor = Brushes.LightGreen.Color;
            BestMoveDestinationColor = Brushes.Green.Color;
            AvailableMoveColor = Brushes.Yellow.Color;
            AvailableCaptureColor = Brushes.Red.Color;
            AvailableEnPassantColor = Brushes.OrangeRed.Color;
            AvailableCastleColor = Brushes.MediumPurple.Color;
        }

        [JsonConstructor]
        public Settings(bool useSymbolsOnMoveList, bool flipBoard, bool updateEvalAfterBot,
            Color boardColor, Color defaultPieceColor,
            Color checkedKingColor, Color selectedPieceColor, Color checkMateHighlightColor, Color lastMoveSourceColor, Color lastMoveDestinationColor,
            Color bestMoveSourceColor, Color bestMoveDestinationColor, Color availableMoveColor, Color availableCaptureColor, Color availableEnPassantColor,
            Color availableCastleColor)
        {
            UseSymbolsOnMoveList = useSymbolsOnMoveList;
            FlipBoard = flipBoard;
            UpdateEvalAfterBot = updateEvalAfterBot;
            BoardColor = boardColor;
            DefaultPieceColor = defaultPieceColor;
            CheckedKingColor = checkedKingColor;
            SelectedPieceColor = selectedPieceColor;
            CheckMateHighlightColor = checkMateHighlightColor;
            LastMoveSourceColor = lastMoveSourceColor;
            LastMoveDestinationColor = lastMoveDestinationColor;
            BestMoveSourceColor = bestMoveSourceColor;
            BestMoveDestinationColor = bestMoveDestinationColor;
            AvailableMoveColor = availableMoveColor;
            AvailableCaptureColor = availableCaptureColor;
            AvailableEnPassantColor = availableEnPassantColor;
            AvailableCastleColor = availableCastleColor;
        }
    }
}
