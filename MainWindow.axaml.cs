using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using FlashcardsApp.Models;
using FlashcardsApp.Logic;
using FlashcardsApp.Services;

namespace FlashcardsApp
{
    public partial class MainWindow : Window
    {
        private List<Folder> _folders = [];
        private List<Deck> _standaloneDecks = [];
        private List<Deck> _recentDecks = [];
        private readonly StorageManager _storage = new();

        private Deck? _selectedDeck;
        private StudySession? _studySession;
        private QuizSession? _quizSession;
        private Card? _currentCard;

        private int _carouselIndex = 0;
        private bool _carouselFlipped = false;
        private bool _studyFlipped = false;
        private int _quizIndex = 0;
        private bool _isUpdatingUI;

        private ObservableCollection<Card> _editCards = [];

        public MainWindow()
        {
            InitializeComponent();
            LoadApplicationData();

            this.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape) ShowPanel(WelcomePanel);
            };
        }

        private void LoadApplicationData()
        {
            var (folders, standalone) = _storage.LoadLibrary("library.json");
            _folders = folders;
            _standaloneDecks = standalone;

            SortLibrary();

            _recentDecks = _folders.SelectMany(f => f.Decks).Concat(_standaloneDecks).Take(5).ToList();
            RecentDecksListBox.ItemsSource = _recentDecks;

            RefreshSidebar();
        }

        private void SortLibrary()
        {
            _folders.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            _standaloneDecks.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            foreach (var folder in _folders)
            {
                folder.Decks.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            }
        }

        private void ShowPanel(Control active)
        {
            WelcomePanel.IsVisible = active == WelcomePanel;
            DeckPanel.IsVisible = active == DeckPanel;
            StudyPanel.IsVisible = active == StudyPanel;
            StudyResultPanel.IsVisible = active == StudyResultPanel;
            QuizPanel.IsVisible = active == QuizPanel;
            EditPanel.IsVisible = active == EditPanel;
            ResultPanel.IsVisible = active == ResultPanel;
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            SaveLibraryToFile();
            this.Close();
        }

        private void BackToDeck_Click(object sender, RoutedEventArgs e) => UpdateDeckInfo();

        // --- SIDEBAR ---
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshSidebar();
        }

        private void RefreshSidebar()
        {
            string searchText = SearchTextBox?.Text ?? "";
            var rootItems = new List<object>();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                rootItems.AddRange(_standaloneDecks);
                rootItems.AddRange(_folders);
            }
            else
            {
                // Filter standalone decks using linear search
                rootItems.AddRange(_standaloneDecks.Where(d => d.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)));

                // Filter folders and their decks
                foreach (var folder in _folders)
                {
                    var matchingDecks = folder.Decks.Where(d => d.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (matchingDecks.Count > 0)
                    {
                        // Create a temporary folder for display to avoid mutating the original
                        var filteredFolder = new Folder(folder.Name)
                        {
                            Decks = matchingDecks
                        };
                        rootItems.Add(filteredFolder);
                    }
                }
            }

            LibraryTreeView.ItemsSource = rootItems;
        }

        private void LibraryTreeView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LibraryTreeView.SelectedItem is Deck deck)
            {
                _selectedDeck = deck;
                _carouselIndex = 0;
                _carouselFlipped = false;
                UpdateDeckInfo();
            }
        }

        private void UpdateDeckInfo()
        {
            if (_selectedDeck == null || _isUpdatingUI) return;

            _isUpdatingUI = true;
            try
            {
                // Move deck to top of recent list or add it if new
                _recentDecks.Remove(_selectedDeck);
                _recentDecks.Insert(0, _selectedDeck);

                if (_recentDecks.Count > 5) _recentDecks.RemoveAt(5);

                // Use .ToList() to provide a fresh reference to the UI
                RecentDecksListBox.ItemsSource = null;
                RecentDecksListBox.ItemsSource = _recentDecks.ToList();
            }
            finally
            {
                _isUpdatingUI = false;
            }

            UpdateCarouselUI();

            // Safer list refresh with fresh reference
            QuickEditList.ItemsSource = null;
            QuickEditList.ItemsSource = _selectedDeck.Cards.ToList();

            ShowPanel(DeckPanel);
        }

        // --- CAROUSEL LOGIC ---
        private void UpdateCarouselUI()
        {
            if (_selectedDeck == null || _selectedDeck.TotalCards == 0)
            {
                CarouselCardText.Text = "No cards in deck. Add some below!";
                CarouselCounterText.Text = "0 / 0";
                return;
            }

            if (_carouselIndex < 0) _carouselIndex = 0;
            if (_carouselIndex >= _selectedDeck.TotalCards) _carouselIndex = 0;

            var card = _selectedDeck.Cards[_carouselIndex];
            CarouselCardText.Text = _carouselFlipped ? card.Definition : card.Term;
            CarouselCounterText.Text = $"{_carouselIndex + 1} / {_selectedDeck.TotalCards}";
        }

        private void CarouselFlip_Click(object sender, RoutedEventArgs e)
        {
            _carouselFlipped = !_carouselFlipped;
            UpdateCarouselUI();
        }

        private void CarouselPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDeck == null || _selectedDeck.TotalCards == 0) return;
            _carouselIndex = (_carouselIndex - 1 + _selectedDeck.TotalCards) % _selectedDeck.TotalCards;
            _carouselFlipped = false;
            UpdateCarouselUI();
        }

        private void CarouselNext_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDeck == null || _selectedDeck.TotalCards == 0) return;
            _carouselIndex = (_carouselIndex + 1) % _selectedDeck.TotalCards;
            _carouselFlipped = false;
            UpdateCarouselUI();
        }

        // --- QUICK EDIT LOGIC ---
        private void ToggleQuickEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Parent?.Parent is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child is TextBlock tb && tb.Classes.Contains("DisplayOnly")) tb.IsVisible = !tb.IsVisible;
                    if (child is TextBox tbox && tbox.Classes.Contains("EditOnly")) tbox.IsVisible = !tbox.IsVisible;
                    if (child is StackPanel sp)
                    {
                        foreach (var spChild in sp.Children)
                        {
                            if (spChild is Button b)
                            {
                                if (b.Classes.Contains("DisplayOnly")) b.IsVisible = !b.IsVisible;
                                if (b.Classes.Contains("EditOnly")) b.IsVisible = !b.IsVisible;
                            }
                        }
                    }
                }
            }
        }

        private void SaveQuickEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Card card)
            {
                // Since Card doesn't implement INotifyPropertyChanged, 
                // we refresh the entire deck info to update all UI bindings (List and Carousel).
                UpdateDeckInfo();
            }
        }

        private void RecentDecksListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isUpdatingUI && RecentDecksListBox.SelectedItem is Deck d)
            {
                _selectedDeck = d;
                _carouselIndex = 0;
                _carouselFlipped = false;
                UpdateDeckInfo();
                RecentDecksListBox.SelectedItem = null;
            }
        }

        // --- MANAGEMENT ---
        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewItemNameInput.Text)) return;
            _folders.Add(new Folder(NewItemNameInput.Text));
            SortLibrary();
            NewItemNameInput.Text = "";
            RefreshSidebar();
            SaveLibraryToFile();
        }

        private void AddDeck_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewItemNameInput.Text)) return;
            var newDeck = new Deck(NewItemNameInput.Text);
            if (LibraryTreeView.SelectedItem is Folder folder) folder.AddDeck(newDeck);
            else _standaloneDecks.Add(newDeck);
            SortLibrary();
            NewItemNameInput.Text = "";
            RefreshSidebar();
            SaveLibraryToFile();
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            bool activeDeckDeleted = false;

            if (LibraryTreeView.SelectedItem is Folder f)
            {
                foreach (var deck in f.Decks)
                {
                    _recentDecks.Remove(deck);
                    if (deck == _selectedDeck) activeDeckDeleted = true;
                }
                _folders.Remove(f);
            }
            else if (LibraryTreeView.SelectedItem is Deck d)
            {
                _recentDecks.Remove(d);
                if (d == _selectedDeck) activeDeckDeleted = true;

                if (!_standaloneDecks.Remove(d))
                {
                    _folders.ForEach(fold => fold.Decks.Remove(d));
                }
            }

            // Update Recent Decks UI with a fresh list reference
            RecentDecksListBox.ItemsSource = _recentDecks.ToList();

            RefreshSidebar();
            SaveLibraryToFile();

            if (activeDeckDeleted)
            {
                _selectedDeck = null;
                ShowPanel(WelcomePanel);
            }
        }

        private void SaveLibrary_Click(object sender, RoutedEventArgs e) => SaveLibraryToFile();

        private void SaveLibraryToFile()
        {
            _storage.SaveLibrary(_folders, _standaloneDecks, "library.json");
        }

        // --- STUDY MODE ---
        private void StartStudy_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDeck?.TotalCards > 0)
            {
                _studySession = new StudySession(_selectedDeck);
                ShowPanel(StudyPanel);
                NextStudy();
            }
        }

        private void NextStudy()
        {
            _currentCard = _studySession?.GetNextCard();
            if (_currentCard == null)
            {
                ShowPanel(StudyResultPanel);
                return;
            }
            _studyFlipped = false;
            UpdateStudyUI();
        }

        private void UpdateStudyUI()
        {
            if (_currentCard == null) return;
            StudyCounterText.Text = $"{_selectedDeck?.LearnedCards} / {_selectedDeck?.TotalCards} learned";
            StudyCardText.Text = _studyFlipped ? _currentCard.Definition : _currentCard.Term;
            SwipeButtons.IsVisible = true;
        }

        private void StudyFlip_Click(object sender, RoutedEventArgs e)
        {
            _studyFlipped = !_studyFlipped;
            UpdateStudyUI();
        }

        private void SwipeLeft_Click(object sender, RoutedEventArgs e)
        {
            if (_currentCard != null) _studySession?.SwipeLeft(_currentCard);
            NextStudy();
        }

        private void SwipeRight_Click(object sender, RoutedEventArgs e)
        {
            if (_currentCard != null) _studySession?.SwipeRight(_currentCard);
            NextStudy();
        }

        // --- QUIZ MODE ---
        private void StartQuiz_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDeck?.TotalCards > 0)
            {
                _quizSession = new QuizSession(_selectedDeck);
                _quizSession.GenerateQuiz(true);
                _quizIndex = 0;
                ShowPanel(QuizPanel);
                NextQuiz();
            }
        }

        private void NextQuiz()
        {
            if (_selectedDeck == null || _quizIndex >= _selectedDeck.TotalCards)
            {
                ResultScoreText.Text = _quizSession?.CalculateResults();
                ShowPanel(ResultPanel);
                return;
            }
            _currentCard = _selectedDeck.Cards[_quizIndex];
            QuizQuestionText.Text = _currentCard.Term;
            QuizAnswerInput.Text = "";
            QuizAnswerInput.IsEnabled = true;
            SubmitAnswerButton.IsVisible = true;
            QuizFeedbackContainer.IsVisible = false;
        }

        private void SubmitQuiz_Click(object sender, RoutedEventArgs e)
        {
            if (_quizSession != null && _currentCard != null)
            {
                bool ok = _quizSession.CheckAnswer(_currentCard, QuizAnswerInput.Text ?? "");
                
                QuizAnswerInput.IsEnabled = false;
                SubmitAnswerButton.IsVisible = false;
                QuizFeedbackContainer.IsVisible = true;

                if (ok)
                {
                    QuizFeedbackText.Text = "Correct!";
                    QuizFeedbackText.Foreground = Brushes.Green;
                    CorrectControls.IsVisible = true;
                    WrongControls.IsVisible = false;
                }
                else
                {
                    QuizFeedbackText.Text = "Wrong: the correct " + _currentCard.Definition;
                    QuizFeedbackText.Foreground = Brushes.Red;
                    CorrectControls.IsVisible = false;
                    WrongControls.IsVisible = true;
                }
            }
        }

        private void QuizNext_Click(object sender, RoutedEventArgs e)
        {
            _quizIndex++;
            NextQuiz();
        }

        private void QuizRewrite_Click(object sender, RoutedEventArgs e)
        {
            QuizAnswerInput.Text = "";
            QuizAnswerInput.IsEnabled = true;
            SubmitAnswerButton.IsVisible = true;
            QuizFeedbackContainer.IsVisible = false;
            QuizAnswerInput.Focus();
        }

        // --- EDIT MODE ---
        private void StartEdit_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDeck == null) return;
            _editCards = new ObservableCollection<Card>(_selectedDeck.Cards.Select(c => new Card(c.Term, c.Definition)));
            CardsEditList.ItemsSource = _editCards;
            ShowPanel(EditPanel);
        }

        private void AddCardRow_Click(object sender, RoutedEventArgs e) => _editCards.Add(new Card("New Term", "New Definition"));

        private void DeleteCardRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Card card) _editCards.Remove(card);
        }

        private void SaveEditChanges_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDeck == null) return;

            // Sync changes back to the original deck
            _selectedDeck.Cards.Clear();
            foreach (var c in _editCards) _selectedDeck.AddCard(c);

            SaveLibraryToFile();

            // Use Dispatcher to avoid layout conflicts when switching panels
            Dispatcher.UIThread.Post(() =>
            {
                UpdateDeckInfo();
            });
        }

        // --- DATA EXCHANGE ---
        private async void ToggleExchangePanel_Click(object sender, RoutedEventArgs e)
        {
            var deck = _selectedDeck;
            if (deck == null) return;

            var dialog = new ImportExportWindow(deck, _storage);
            await dialog.ShowDialog(this);
            
            // Refresh logic: don't call UpdateDeckInfo if it kicks us out of the editor
            if (EditPanel.IsVisible)
            {
                // Refresh the editor's copy of cards
                _editCards = new ObservableCollection<Card>(deck.Cards.Select(c => new Card(c.Term, c.Definition)));
                CardsEditList.ItemsSource = _editCards;
            }
            else
            {
                UpdateDeckInfo();
            }
        }
    }
}
