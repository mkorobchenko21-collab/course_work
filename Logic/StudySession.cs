using System;
using System.Collections.Generic;
using System.Linq;
using FlashcardsApp.Models;

namespace FlashcardsApp.Logic
{
    /// <summary>
    /// Manages a study session where the user swipes through cards to mark them as learned or unlearned.
    /// </summary>
    public class StudySession
    {
        private readonly Deck _currentDeck;
        private readonly Queue<Card> _sessionQueueCards;

        /// <summary>
        /// Initializes a new study session with a copy of cards from the specified deck.
        /// </summary>
        /// <param name="deck">The deck to study.</param>
        /// <exception cref="ArgumentNullException">Thrown when the deck is null.</exception>
        public StudySession(Deck deck)
        {
            ArgumentNullException.ThrowIfNull(deck);
            _currentDeck = deck;

            // Reset all cards to unlearned at the start of each session
            foreach (var card in _currentDeck.Cards)
            {
                card.IsLearned = false;
            }

            // sessionQueue now contains all cards
            _sessionQueueCards = new Queue<Card>(_currentDeck.Cards);
        }

        /// <summary>
        /// Retrieves the next card in the session queue.
        /// </summary>
        /// <returns>The next <see cref="Card"/> or null if no cards are left.</returns>
        public Card? GetNextCard()
        {
            return _sessionQueueCards.Count > 0 ? _sessionQueueCards.Peek() : null;
        }

        /// <summary>
        /// Handles a "Don't Know" action. Moves the card to the end of the queue.
        /// </summary>
        /// <param name="card">The card being swiped.</param>
        public void SwipeLeft(Card card)
        {
            Card current = _sessionQueueCards.Dequeue();

            _sessionQueueCards.Enqueue(current);
        }

        /// <summary>
        /// Handles a "Know" action. Marks the card as learned and removes it from the queue.
        /// </summary>
        /// <param name="card">The card being swiped.</param>
        public void SwipeRight(Card card)
        {
            Card current = _sessionQueueCards.Dequeue();
            current.IsLearned = true;
        }
    }
}
