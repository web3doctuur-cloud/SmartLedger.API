using SmartLedger.API.Models;

namespace SmartLedger.API.Services
{
    public interface ILedgerService
    {
        // Account Management
        Task<Account?> GetAccountByIdAsync(int id);
        Task<IEnumerable<Account>> GetAllAccountsAsync();
        Task<Account> CreateAccountAsync(Account account);
        Task<Account?> UpdateAccountAsync(int id, Account account);
        Task<bool> DeleteAccountAsync(int id);

        // Journal Entries
        Task<JournalEntry> CreateJournalEntryAsync(JournalEntry entry, List<JournalEntryLine> lines);
        Task<JournalEntry?> GetJournalEntryByIdAsync(int id);
        Task<IEnumerable<JournalEntry>> GetAllJournalEntriesAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<bool> ApproveJournalEntryAsync(int id);
        Task<bool> DeleteJournalEntryAsync(int id);

        // Validation
        bool ValidateDoubleEntry(List<JournalEntryLine> lines);
        decimal GetTotalDebits(List<JournalEntryLine> lines);
        decimal GetTotalCredits(List<JournalEntryLine> lines);

        // Reports
        Task<decimal> GetAccountBalanceAsync(int accountId);
        Task<Dictionary<string, decimal>> GetTrialBalanceAsync();
        Task<(decimal Income, decimal Expenses, decimal NetProfit)> GetProfitLossAsync(DateTime startDate, DateTime endDate);
    }
}
