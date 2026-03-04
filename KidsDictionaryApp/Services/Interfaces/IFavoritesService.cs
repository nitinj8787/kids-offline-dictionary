using KidsDictionaryApp.Models;

namespace KidsDictionaryApp.Services.Interfaces
{
    public interface IFavoritesService
    {
        Task AddAsync(string word);
        Task RemoveAsync(string word);
        Task<bool> IsFavoriteAsync(string word);
        Task<List<FavoriteWord>> GetFavoritesAsync();
    }
}
