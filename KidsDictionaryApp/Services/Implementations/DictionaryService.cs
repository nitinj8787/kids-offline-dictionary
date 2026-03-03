using KidsDictionaryApp.Models;
using KidsDictionaryApp.Services.Interfaces;

namespace KidsDictionaryApp.Services.Implementations
{
    public class DictionaryService : IDictionaryService
    {
        private readonly IWordRepository _repository;

        public DictionaryService(IWordRepository repository)
        {
            _repository = repository;
        }

        public async Task<Word?> GetWordAsync(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return null;

            return await _repository.GetWordAsync(word.Trim());
        }
    }
}