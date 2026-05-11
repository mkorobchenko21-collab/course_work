using System;
using System.Collections.Generic;

namespace FlashcardsApp.Models
{
    /// <summary>
    /// Represents a folder that groups multiple flashcard decks.
    /// </summary>
    public class Folder
    {
        public const int MaxNameLength = 30;
        private string _name = null!; // _name != null
        /// <summary>
        /// Gets or sets the name of the folder.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the value is null or whitespace, or exceeds MaxNameLength.</exception>
        public string Name
        {
            get => _name;
            set
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
                if (value.Length > MaxNameLength)
                    throw new ArgumentException($"Name cannot exceed {MaxNameLength} characters.", nameof(value));
                _name = value;
            }
        }

        /// <summary>
        /// Gets or sets the list of decks contained within this folder.
        /// </summary>
        public List<Deck> Decks { get; set; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="Folder"/> class.
        /// </summary>
        /// <param name="name">The name of the folder.</param>
        public Folder(string name)
        {
            Name = name;
            Decks = []; // same as Decks = new() and Decks = new List<Deck>();
        }

        /// <summary>
        /// Adds a new deck to the folder.
        /// </summary>
        /// <param name="deck">The deck to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when the deck is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the deck already exists in this folder.</exception>
        public void AddDeck(Deck deck)
        {
            ArgumentNullException.ThrowIfNull(deck);

            if (Decks.Contains(deck))
            {
                throw new InvalidOperationException("Deck already exists in this folder.");
            }

            Decks.Add(deck);
        }

        /// <summary>
        /// Removes a specified deck from the folder.
        /// </summary>
        /// <param name="deck">The deck to remove.</param>
        /// <exception cref="ArgumentNullException">Thrown when the deck is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the deck is not found in this folder.</exception>
        public void RemoveDeck(Deck deck)
        {
            ArgumentNullException.ThrowIfNull(deck);

            if (!Decks.Remove(deck))
            {
                throw new InvalidOperationException("Deck not found in this folder.");
            }
        }
    }
}
