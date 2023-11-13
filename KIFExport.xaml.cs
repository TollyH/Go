using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace Go
{
    /// <summary>
    /// Interaction logic for KIFExport.xaml
    /// </summary>
    public partial class KIFExport : Window
    {
        private readonly GoGame game;
        private readonly bool blackIsComputer;
        private readonly bool whiteIsComputer;

        public KIFExport(GoGame game, bool blackIsComputer, bool whiteIsComputer)
        {
            this.game = game;
            this.blackIsComputer = blackIsComputer;
            this.whiteIsComputer = whiteIsComputer;
            InitializeComponent();

            if (blackIsComputer)
            {
                blackNameBox.Text = "Computer";
                blackNameBox.IsReadOnly = true;
                blackNameBox.IsEnabled = false;
            }
            if (whiteIsComputer)
            {
                whiteNameBox.Text = "Computer";
                whiteNameBox.IsReadOnly = true;
                whiteNameBox.IsEnabled = false;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new()
            {
                AddExtension = true,
                DefaultExt = ".kifu",
                Filter = "Kifu File|*.kifu",
                CheckPathExists = true
            };
            if (!saveDialog.ShowDialog() ?? true)
            {
                return;
            }
            string eventName = eventNameBox.Text.Trim();
            string locationName = locationNameBox.Text.Trim();
            DateOnly? date = dateBox.SelectedDate is null ? null : DateOnly.FromDateTime(dateBox.SelectedDate.Value);
            string blackName = blackNameBox.Text.Trim();
            string whiteName = whiteNameBox.Text.Trim();
            File.WriteAllText(saveDialog.FileName, game.ToKIF(eventName != "" ? eventName : null,
                locationName != "" ? locationName : null, date, blackName != "" ? blackName : "Player",
                whiteName != "" ? whiteName : "Player", blackIsComputer, whiteIsComputer));
            Close();
        }
    }
}
