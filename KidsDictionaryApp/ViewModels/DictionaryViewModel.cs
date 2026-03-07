using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsDictionaryApp.Models;
using KidsDictionaryApp.Services.Interfaces;

namespace KidsDictionaryApp.ViewModels
{
    public partial class DictionaryViewModel : ObservableObject
    {
        private readonly IDictionaryService _dictionaryService;
        private readonly ITextToSpeechService _textToSpeechService;
        private readonly ISpeechService _speechService;
        private readonly IWordHistoryService _historyService;
        private readonly IFavoritesService _favoritesService;
        private readonly IProfileService _profileService;
        private readonly IProgressService _progressService;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private Word? _currentWord;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isFavorite;

        [ObservableProperty]
        private bool _isListening;

        public DictionaryViewModel(
            IDictionaryService dictionaryService,
            ITextToSpeechService textToSpeechService,
            ISpeechService speechService,
            IWordHistoryService historyService,
            IFavoritesService favoritesService,
            IProfileService profileService,
            IProgressService progressService)
        {
            _dictionaryService = dictionaryService;
            _textToSpeechService = textToSpeechService;
            _speechService = speechService;
            _historyService = historyService;
            _favoritesService = favoritesService;
            _profileService = profileService;
            _progressService = progressService;
        }

        [RelayCommand]
        public async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return;

            IsLoading = true;
            StatusMessage = string.Empty;
            CurrentWord = null;

            try
            {
                CurrentWord = await _dictionaryService.GetWordAsync(SearchText.Trim());
                if (CurrentWord == null)
                {
                    StatusMessage = $"Oops! '{SearchText}' was not found. Try another word! 🔍";
                }
                else
                {
                    await _historyService.AddAsync(CurrentWord.WordText);
                    IsFavorite = await _favoritesService.IsFavoriteAsync(CurrentWord.WordText);

                    // Track progress for the active profile
                    if (_profileService.ActiveProfile != null)
                    {
                        await _progressService.RecordWordLookupAsync(
                            _profileService.ActiveProfile.Id,
                            CurrentWord.WordText);
                    }
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task ListenAsync()
        {
            IsListening = true;
            StatusMessage = "Listening... 🎤";

            try
            {
                var spokenWord = await _speechService.ListenAsync();
                if (!string.IsNullOrWhiteSpace(spokenWord))
                {
                    SearchText = spokenWord;
                    await SearchAsync();
                }
                else
                {
                    StatusMessage = "Could not hear anything. Please try again! 🎤";
                }
            }
            finally
            {
                IsListening = false;
            }
        }

        [RelayCommand]
        public async Task SpeakAsync()
        {
            if (CurrentWord == null)
                return;

            var text = $"{CurrentWord.WordText}. {CurrentWord.Meaning}";
            if (!string.IsNullOrWhiteSpace(CurrentWord.Example))
                text += $" For example: {CurrentWord.Example}";

            await _textToSpeechService.SpeakAsync(text);
        }

        [RelayCommand]
        public async Task ToggleFavoriteAsync()
        {
            if (CurrentWord == null)
                return;

            if (IsFavorite)
            {
                await _favoritesService.RemoveAsync(CurrentWord.WordText);
                IsFavorite = false;
            }
            else
            {
                await _favoritesService.AddAsync(CurrentWord.WordText);
                IsFavorite = true;
            }
        }
    }
}
