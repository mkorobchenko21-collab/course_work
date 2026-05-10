using System.Collections.Generic;
using FlashcardsApp.Models;

namespace FlashcardsApp.Services
{
    /// <summary>
    /// Defines the contract for storage operations and data formatting.
    /// </summary>
    public interface IStorageManager
    {
        // --- Persistence (Files, Databases, Cloud, etc.) ---

        void SaveLibrary(List<Folder> folders, List<Deck> standaloneDecks, string destinationPath);
        (List<Folder> folders, List<Deck> standaloneDecks) LoadLibrary(string sourcePath);

        void ExportDeck(Deck deck, string destinationPath);
        Deck ImportDeck(string sourcePath);

        // --- Data formatting (Raw strings) ---

        Deck ParseDeckFromString(string rawText, string termSeparator, string cardSeparator);
        string FormatDeckToString(Deck deck, string termSeparator, string cardSeparator);
    }
}
