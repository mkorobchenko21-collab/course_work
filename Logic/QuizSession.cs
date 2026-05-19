using System;
using FlashcardsApp.Models;

namespace FlashcardsApp.Logic
{
    /// <summary>
    /// Manages a quiz session where the user's knowledge is tested through text input or multiple choice.
    /// </summary>
    public class QuizSession
    {
        private readonly Deck _currentDeck;
        private int _correctAnswers;
        private int _totalQuestions;

        /// <summary>
        /// Initializes a new quiz session for the specified deck.
        /// </summary>
        /// <param name="deck">The deck to test.</param>
        /// <exception cref="ArgumentNullException">Thrown when the deck is null.</exception>
        public QuizSession(Deck deck)
        {
            ArgumentNullException.ThrowIfNull(deck);
            _currentDeck = deck;
        }

        /// <summary>
        /// Prepares the quiz by resetting scores.
        /// </summary>
        public void GenerateQuiz()
        {
            _correctAnswers = 0;
            _totalQuestions = _currentDeck.TotalCards;
        }

        /// <summary>
        /// Checks if the provided answer matches the card's definition.
        /// </summary>
        /// <param name="card">The card being tested.</param>
        /// <param name="answer">The user's input string.</param>
        /// <returns>True if the answer is correct; otherwise, false.</returns>
        public bool CheckAnswer(Card card, string answer)
        {
            ArgumentNullException.ThrowIfNull(card);

            bool isCorrect = string.Equals(card.Definition.Trim(), answer.Trim(), StringComparison.OrdinalIgnoreCase);

            if (isCorrect)
            {
                _correctAnswers++;
            }

            return isCorrect;
        }

        /// <summary>
        /// Calculates and returns the final score as a percentage.
        /// </summary>
        /// <returns>A formatted string representing the percentage of correct answers.</returns>
        public string CalculateResults()
        {
            if (_totalQuestions == 0) return "0%"; // edge case

            double score = (double)_correctAnswers / _totalQuestions * 100;
            return $"{Math.Round(score, 2)}%";
        }
    }
}
