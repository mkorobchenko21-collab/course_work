using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FlashcardsApp.Models;

namespace FlashcardsApp.Services
{
    /// <summary>
    /// Provides functionality for saving and loading flashcard data to and from various formats, such as JSON and plain text.
    /// </summary>
    public class StorageManager : IStorageManager
    {
        // Settings for pretty JSON formatting
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true // for readable json file 
        };

        // Wrapper class for serializing both lists
        private class LibraryData
        {
            public List<Folder> Folders { get; set; } = new();
            public List<Deck> StandaloneDecks { get; set; } = new();
        }

        /// <summary>
        /// Saves the entire library (folders and standalone decks) to a specified file in JSON format.
        /// </summary>
        public void SaveLibrary(List<Folder> folders, List<Deck> standaloneDecks, string filePath)
        {
            ArgumentNullException.ThrowIfNull(folders);
            ArgumentNullException.ThrowIfNull(standaloneDecks);
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));

            var data = new LibraryData { Folders = folders, StandaloneDecks = standaloneDecks };
            string jsonString = JsonSerializer.Serialize(data, _jsonOptions);
            File.WriteAllText(filePath, jsonString);
        }

        /// <summary>
        /// Loads the entire library from a specified JSON file.
        /// </returns>
        public (List<Folder> folders, List<Deck> standaloneDecks) LoadLibrary(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));

            if (!File.Exists(filePath))
            {
                return (new List<Folder>(), new List<Deck>());
            }

            try
            {
                string jsonString = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<LibraryData>(jsonString, _jsonOptions);
                return (data?.Folders ?? new List<Folder>(), data?.StandaloneDecks ?? new List<Deck>());
            }
            catch (JsonException)
            {
                // Return empty if format is old or invalid to prevent startup crash
                return (new List<Folder>(), new List<Deck>());
            }
        }

        /// <summary>
        /// Exports a single deck to a specified file in JSON format.
        /// </summary>
        /// <param name="deck">The deck to export.</param>
        /// <param name="filePath">The path to the destination file.</param>
        /// <exception cref="ArgumentNullException">Thrown when the deck is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the file path is null or whitespace.</exception>
        public void ExportDeck(Deck deck, string filePath)
        {
            ArgumentNullException.ThrowIfNull(deck);
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));

            string jsonString = JsonSerializer.Serialize(deck, _jsonOptions);
            File.WriteAllText(filePath, jsonString);
        }

        /// <summary>
        /// Imports a single deck from a specified JSON file.
        /// </summary>
        /// <param name="filePath">The path to the source file.</param>
        /// <returns>The imported deck.</returns>
        /// <exception cref="ArgumentException">Thrown when the file path is null or whitespace.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the specified file is not found.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the JSON is malformed and fails to parse.</exception>
        public Deck ImportDeck(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found.", filePath);
            }

            string jsonString = File.ReadAllText(filePath);
            Deck? importedDeck = JsonSerializer.Deserialize<Deck>(jsonString, _jsonOptions);

            // Throws if JSON was malformed and resulted in null
            return importedDeck ?? throw new InvalidOperationException("Failed to parse Deck from JSON.");
        }

        // --- Way 2: Text (Interface / Quick import) ---

        /// <summary>
        /// Imports a deck from a raw text string using specified separators.
        /// </summary>
        /// <param name="rawText">The raw text containing terms and definitions.</param>
        /// <param name="termSeparator">The string used to separate a term from its definition within a card.</param>
        /// <param name="cardSeparator">The string used to separate distinct cards from each other.</param>
        /// <returns>A new deck containing the parsed cards.</returns>
        /// <exception cref="ArgumentException">Thrown when the raw text is null/whitespace, or separators are null/empty.</exception>
        /// <exception cref="FormatException">Thrown when no valid cards can be parsed from the text.</exception>
        public Deck ParseDeckFromString(string rawText, string termSeparator, string cardSeparator)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rawText, nameof(rawText));
            ArgumentException.ThrowIfNullOrEmpty(termSeparator, nameof(termSeparator));
            ArgumentException.ThrowIfNullOrEmpty(cardSeparator, nameof(cardSeparator));

            Deck newDeck = new("Imported Deck"); // Default name for quick import

            // Splits text by the chosen card separator
            string[] cardBlocks = rawText.Split(cardSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (string block in cardBlocks)
            {
                // Splits each block by the term separator
                string[] parts = block.Split(termSeparator, StringSplitOptions.TrimEntries);

                // Validates if the block has exactly 2 parts (Term and Definition)
                if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[0]) && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    Card newCard = new(parts[0], parts[1]);
                    newDeck.AddCard(newCard);
                }
            }

            if (newDeck.TotalCards == 0)
            {
                throw new FormatException("No valid cards found in the provided text. Check the separators.");
            }

            return newDeck;
        }

        /// <summary>
        /// Exports a deck to a raw text string using specified separators.
        /// </summary>
        /// <param name="deck">The deck to export.</param>
        /// <param name="termSeparator">The string to use to separate a term from its definition.</param>
        /// <param name="cardSeparator">The string to use to separate distinct cards.</param>
        /// <returns>A formatted string containing all cards in the deck.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the deck is null.</exception>
        /// <exception cref="ArgumentException">Thrown when separators are null or empty.</exception>
        public string FormatDeckToString(Deck deck, string termSeparator, string cardSeparator)
        {
            ArgumentNullException.ThrowIfNull(deck);
            ArgumentException.ThrowIfNullOrEmpty(termSeparator, nameof(termSeparator));
            ArgumentException.ThrowIfNullOrEmpty(cardSeparator, nameof(cardSeparator));

            List<string> blocks = [];

            foreach (Card card in deck.Cards)
            {
                // Combines term and definition using the term separator (added spaces for readability)
                blocks.Add($"{card.Term} {termSeparator} {card.Definition}");
            }

            // Joins all blocks with the card separator
            return string.Join(cardSeparator, blocks);
        }
    }
}
