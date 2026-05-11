using System;
using System.Collections.Generic;
using System.Linq;

namespace FlashcardsApp.Models
{
    /// <summary>
    /// Represents a collection of flashcards (a deck).
    /// </summary>
    public class Deck
    {
        public const int MaxNameLength = 30;
        private string _name = null!; // null! says lsp that _name will never be a null
        /// <summary>
        /// Gets or sets the name of the deck.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the name is null or whitespace, or exceeds MaxNameLength.</exception>
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
        /// Gets or sets the list of cards in this deck.
        /// </summary>
        public List<Card> Cards { get; set; } = new();

        /// <summary>
        /// Gets the total number of cards in the deck.
        /// </summary>
        public int TotalCards => Cards.Count;

        /// <summary>
        /// Gets the number of successfully learned cards.
        /// </summary>
        public int LearnedCards => Cards.Count(c => c.IsLearned);

        /// <summary>
        /// Initializes a new instance of the <see cref="Deck"/> class.
        /// </summary>
        /// <param name="name">The name of the deck.</param>
        public Deck(string name)
        {
            Name = name;
            Cards = []; // same as Cards = new() and Cards = new List<Card>();
        }

        /// <summary>
        /// Adds a new card to the deck.
        /// </summary>
        /// <param name="card">The card to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when card is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when card already exists.</exception>
        public void AddCard(Card card)
        {
            ArgumentNullException.ThrowIfNull(card);

            if (Cards.Contains(card))
            {
                throw new InvalidOperationException("Card already exists.");
            }

            Cards.Add(card);
        }

        /// <summary>
        /// Removes a specified card from the deck.
        /// </summary>
        /// <param name="card">The card to remove.</param>
        /// <exception cref="ArgumentNullException">Thrown when card is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when card is not found in the deck.</exception>
        public void RemoveCard(Card card)
        {
            ArgumentNullException.ThrowIfNull(card);

            // List<T>.Remove returns bool
            if (!Cards.Remove(card))
            {
                throw new InvalidOperationException("Card not found.");
            }
        }

        /// <summary>
        /// Replaces an existing card with a new one.
        /// </summary>
        /// <param name="oldCard">The card to be replaced.</param>
        /// <param name="newCard">The new card to insert.</param>
        /// <exception cref="ArgumentNullException">Thrown when either card is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the old card is not found.</exception>
        public void EditCard(Card oldCard, Card newCard)
        {
            ArgumentNullException.ThrowIfNull(oldCard);
            ArgumentNullException.ThrowIfNull(newCard);

            int index = Cards.IndexOf(oldCard);
            if (index == -1)
            {
                throw new InvalidOperationException("Card for editing not found.");
            }

            Cards[index] = newCard;
        }

        /// <summary>
        /// Randomly shuffles the cards in the deck using the Fisher-Yates algorithm.
        /// </summary>
        public void Shuffle()
        {
            if (Cards.Count < 2)
            {
                return;
            }

            Random rnd = new(); // Random rng = new Random();

            int n = Cards.Count;

            while (n > 1)
            {
                n--;
                int k = rnd.Next(n + 1);
                (Cards[k], Cards[n]) = (Cards[n], Cards[k]);
            }
        }
    }
}
