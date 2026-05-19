using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using FlashcardsApp.Models;
using FlashcardsApp.Services;

namespace FlashcardsApp
{
    public partial class ImportExportWindow : Window
    {
        private Deck _deck = null!;
        private IStorageManager _storage = null!;

        public ImportExportWindow()
        {
            InitializeComponent();
        }

        public ImportExportWindow(Deck deck, IStorageManager storage)
        {
            InitializeComponent();
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));

            // Set initial path
            string fileName = $"{_deck.Name.Replace(" ", "_")}.json";
            FilePathInput.Text = Path.Combine(".", "Data", fileName);
        }

        // --- 1. TEXT IMPORT/EXPORT ---

        private string ResolveSeparator(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return input.Replace("\\n", "\n").Replace("\\t", "\t");
        }

        private void ExportToText_Click(object sender, RoutedEventArgs e)
        {
            ShowStatus("Exporting...", Brushes.Gray);
            try
            {
                string termSep = ResolveSeparator(TermSeparatorInput.Text ?? "-");
                string cardSep = ResolveSeparator(CardSeparatorInput.Text ?? ";");
                
                DataBuffer.Text = _storage.FormatDeckToString(_deck, termSep, cardSep);
                ShowStatus("Success! Exported to text buffer.", Brushes.Green);
            }
            catch (Exception ex)
            {
                ShowStatus($"Export failed: {ex.Message}", Brushes.Red);
            }
        }

        private void ImportFromText_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DataBuffer.Text))
            {
                ShowStatus("Buffer is empty. Nothing to import.", Brushes.Red);
                return;
            }

            ShowStatus("Importing...", Brushes.Gray);
            try
            {
                string termSep = ResolveSeparator(TermSeparatorInput.Text ?? "-");
                string cardSep = ResolveSeparator(CardSeparatorInput.Text ?? ";");

                var importedDeck = _storage.ParseDeckFromString(DataBuffer.Text, termSep, cardSep);
                
                // Append cards to current deck
                foreach (var card in importedDeck.Cards)
                {
                    _deck.AddCard(card);
                }

                ShowStatus($"Success! Appended {importedDeck.TotalCards} cards.", Brushes.Green);
            }
            catch (Exception ex)
            {
                ShowStatus($"Import failed: {ex.Message}", Brushes.Red);
            }
        }

        // --- 2. JSON IMPORT/EXPORT ---

        private void ExportToJson_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = FilePathInput.Text ?? "";
                if (string.IsNullOrWhiteSpace(path))
                {
                    ShowStatus("Please enter a valid file path.", Brushes.Red);
                    return;
                }

                ShowStatus("Saving JSON...", Brushes.Gray);

                // Extension safety: if not .json, append it
                if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    path += ".json";
                    FilePathInput.Text = path;
                }

                // Ensure directory exists
                string fullPath = Path.GetFullPath(path);
                string? directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) 
                {
                    Directory.CreateDirectory(directory);
                }

                _storage.ExportDeck(_deck, fullPath);
                ShowStatus($"Success! Saved to: {path}", Brushes.Green);
            }
            catch (Exception ex)
            {
                ShowStatus($"JSON Export failed: {ex.Message}", Brushes.Red);
            }
        }

        private void ImportFromJson_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = FilePathInput.Text ?? "";
                if (string.IsNullOrWhiteSpace(path))
                {
                    ShowStatus("Please enter a valid file path.", Brushes.Red);
                    return;
                }

                ShowStatus("Loading JSON...", Brushes.Gray);

                string fullPath = Path.GetFullPath(path);
                if (!File.Exists(fullPath))
                {
                    ShowStatus($"Error: File not found at '{path}'", Brushes.Red);
                    return;
                }

                var imported = _storage.ImportDeck(fullPath);
                
                // Append cards to current deck
                foreach (var card in imported.Cards)
                {
                    _deck.AddCard(card);
                }

                ShowStatus($"Success! Appended {imported.TotalCards} cards.", Brushes.Green);
            }
            catch (Exception ex)
            {
                ShowStatus($"JSON Import failed: {ex.Message}", Brushes.Red);
            }
        }

        // --- 3. UI HELPERS ---

        private void ShowStatus(string message, IBrush color)
        {
            StatusMessage.Text = message;
            StatusMessage.Foreground = color;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
