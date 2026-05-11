namespace FlashcardsApp.Models
{
    /// <summary>
    /// Represents a single flashcard used for learning, containing a term and its definition.
    /// </summary>
    public class Card
    {
        public const int MaxTextLength = 200;
        private string _term;
        private string _definition;

        /// <summary>
        /// Gets or sets the term or question on the front side of the card.
        /// </summary>
        public string Term
        {
            get => _term;
            set
            {
                if (value?.Length > MaxTextLength)
                    throw new System.ArgumentException($"Term cannot exceed {MaxTextLength} characters.");
                _term = value ?? "";
            }
        }

        /// <summary>
        /// Gets or sets the definition or answer on the back side of the card.
        /// </summary>
        public string Definition
        {
            get => _definition;
            set
            {
                if (value?.Length > MaxTextLength)
                    throw new System.ArgumentException($"Definition cannot exceed {MaxTextLength} characters.");
                _definition = value ?? "";
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the card has been mastered by the user.
        /// </summary>
        public bool IsLearned { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Card"/> class with a specified term and definition.
        /// </summary>
        /// <param name="term">The word or concept to be learned.</param>
        /// <param name="definition">The explanation or translation of the term.</param>
        public Card(string term, string definition)
        {
            Term = term;
            Definition = definition;
            IsLearned = false; // defautl value
        }
    }
}
