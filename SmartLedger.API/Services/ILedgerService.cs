using SmartLedger.API.DTOs;
using SmartLedger.API.Models;

namespace SmartLedger.API.Services
{
    public interface ILedgerService
    {
        // Account Management
        Task<Account?> GetAccountByIdAsync(int id, string userId);
        Task<IEnumerable<Account>> GetAllAccountsAsync(string userId);
        Task<Account> CreateAccountAsync(Account account, string userId);
        Task<Account?> UpdateAccountAsync(int id, Account account, string userId);
        Task<bool> DeleteAccountAsync(int id, string userId);

        // Journal Entries
        Task<JournalEntry> CreateJournalEntryAsync(JournalEntry entry, List<JournalEntryLine> lines, string userId);
        Task<JournalEntryResponseDto?> GetJournalEntryByIdAsync(int id, string userId);
        Task<IEnumerable<JournalEntryResponseDto>> GetAllJournalEntriesAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<bool> ApproveJournalEntryAsync(int id, string userId);
        Task<bool> DeleteJournalEntryAsync(int id, string userId);

        // Validation
        bool ValidateDoubleEntry(List<JournalEntryLine> lines);
        decimal GetTotalDebits(List<JournalEntryLine> lines);
        decimal GetTotalCredits(List<JournalEntryLine> lines);

        // Reports
        Task<decimal> GetAccountBalanceAsync(int accountId, string userId, DateTime? asOfDate = null);
        Task<Dictionary<string, decimal>> GetTrialBalanceAsync(string userId, DateTime? asOfDate = null);
        Task<(decimal Income, decimal Expenses, decimal NetProfit)> GetProfitLossAsync(DateTime startDate, DateTime endDate, string userId);
    }
}
