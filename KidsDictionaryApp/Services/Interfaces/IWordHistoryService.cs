using KidsDictionaryApp.Models;

namespace KidsDictionaryApp.Services.Interfaces
{
    public interface IWordHistoryService
    {
        Task AddAsync(string word);
        Task<List<WordHistory>> GetHistoryAsync();
        Task ClearAsync();
        Task<int> GetTodayCountAsync();
        Task<int> GetThisWeekCountAsync();
        Task<int> GetThisMonthCountAsync();
    }
}
