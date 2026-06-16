using Microsoft.EntityFrameworkCore;
using SmartLedger.API.Data;
using SmartLedger.API.Models;

namespace SmartLedger.API.Services
{
    public class LedgerService : ILedgerService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LedgerService> _logger;

        public LedgerService(ApplicationDbContext context, ILogger<LedgerService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ============================================================
        // ACCOUNT MANAGEMENT
        // ============================================================

        public async Task<Account?> GetAccountByIdAsync(int id)
        {
            return await _context.Accounts.FindAsync(id);
        }

        public async Task<IEnumerable<Account>> GetAllAccountsAsync()
        {
            return await _context.Accounts
                .Where(a => a.IsActive)
                .OrderBy(a => a.AccountCode)
                .ToListAsync();
        }

        public async Task<Account> CreateAccountAsync(Account account)
        {
            account.CreatedAt = DateTime.UtcNow;
            account.IsActive = true;

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Account created: {account.AccountCode} - {account.Name}");
            return account;
        }

        public async Task<Account?> UpdateAccountAsync(int id, Account account)
        {
            var existingAccount = await _context.Accounts.FindAsync(id);
            if (existingAccount == null) return null;

            existingAccount.Name = account.Name;
            existingAccount.Type = account.Type;
            existingAccount.NormalSide = account.NormalSide;
            existingAccount.ParentAccountId = account.ParentAccountId;
            existingAccount.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existingAccount;
        }

        public async Task<bool> DeleteAccountAsync(int id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null) return false;

            // Check if account has transactions
            var hasTransactions = await _context.JournalEntryLines
                .AnyAsync(l => l.AccountId == id);

            if (hasTransactions)
            {
                throw new InvalidOperationException("Cannot delete account with existing transactions");
            }

            account.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        // ============================================================
        // JOURNAL ENTRIES
        // ============================================================

        public async Task<JournalEntry> CreateJournalEntryAsync(JournalEntry entry, List<JournalEntryLine> lines)
        {
            // Validate double-entry accounting
            if (!ValidateDoubleEntry(lines))
            {
                throw new InvalidOperationException("Total debits must equal total credits");
            }

            // Generate unique entry number
            entry.EntryNumber = await GenerateEntryNumberAsync();
            entry.CreatedAt = DateTime.UtcNow;
            entry.IsActive = true;

            _context.JournalEntries.Add(entry);
            await _context.SaveChangesAsync();

            // Add lines with reference to entry
            foreach (var line in lines)
            {
                line.JournalEntryId = entry.Id;
                line.CreatedAt = DateTime.UtcNow;
                line.IsActive = true;
                _context.JournalEntryLines.Add(line);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Journal entry created: {entry.EntryNumber} (ID: {entry.Id})");
            return entry;
        }

        public async Task<JournalEntry?> GetJournalEntryByIdAsync(int id)
        {
            return await _context.JournalEntries
                .Include(j => j.Lines)
                    .ThenInclude(l => l.Account)
                .FirstOrDefaultAsync(j => j.Id == id);
        }

        public async Task<IEnumerable<JournalEntry>> GetAllJournalEntriesAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.JournalEntries
                .Include(j => j.Lines)
                    .ThenInclude(l => l.Account)
                .Where(j => j.IsActive);

            if (fromDate.HasValue)
                query = query.Where(j => j.EntryDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(j => j.EntryDate <= toDate.Value);

            var results = await query.OrderByDescending(j => j.EntryDate).ToListAsync();

            // 🔧 FIX: Ensure Lines is never null and Account is never null
            foreach (var entry in results)
            {
                if (entry.Lines == null)
                {
                    entry.Lines = new List<JournalEntryLine>();
                }
                else
                {
                    foreach (var line in entry.Lines)
                    {
                        if (line.Account == null)
                        {
                            line.Account = new Account
                            {
                                Id = line.AccountId,
                                Name = "Unknown Account",
                                AccountCode = "N/A",
                                Type = "Unknown",
                                NormalSide = "DEBIT"
                            };
                        }
                    }
                }
            }

            return results;
        }

        public async Task<bool> ApproveJournalEntryAsync(int id)
        {
            var entry = await _context.JournalEntries.FindAsync(id);
            if (entry == null) return false;

            entry.IsApproved = true;
            entry.ApprovedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Journal entry approved: {entry.EntryNumber}");
            return true;
        }

        public async Task<bool> DeleteJournalEntryAsync(int id)
        {
            var entry = await _context.JournalEntries.FindAsync(id);
            if (entry == null) return false;

            entry.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        // ============================================================
        // VALIDATION
        // ============================================================

        public bool ValidateDoubleEntry(List<JournalEntryLine> lines)
        {
            if (lines == null || lines.Count < 2) return false;

            var totalDebits = GetTotalDebits(lines);
            var totalCredits = GetTotalCredits(lines);

            return totalDebits == totalCredits;
        }

        public decimal GetTotalDebits(List<JournalEntryLine> lines)
        {
            return lines.Sum(l => l.Debit);
        }

        public decimal GetTotalCredits(List<JournalEntryLine> lines)
        {
            return lines.Sum(l => l.Credit);
        }

        // ============================================================
        // REPORTS
        // ============================================================

        public async Task<decimal> GetAccountBalanceAsync(int accountId)
        {
            var lines = await _context.JournalEntryLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == accountId && l.JournalEntry!.IsApproved)
                .ToListAsync();

            var account = await _context.Accounts.FindAsync(accountId);
            if (account == null) return 0;

            var totalDebits = lines.Sum(l => l.Debit);
            var totalCredits = lines.Sum(l => l.Credit);

            return account.NormalSide == "DEBIT"
                ? totalDebits - totalCredits
                : totalCredits - totalDebits;
        }

        public async Task<Dictionary<string, decimal>> GetTrialBalanceAsync()
        {
            var accounts = await _context.Accounts
                .Where(a => a.IsActive)
                .ToListAsync();

            var trialBalance = new Dictionary<string, decimal>();

            foreach (var account in accounts)
            {
                var balance = await GetAccountBalanceAsync(account.Id);
                if (balance != 0)
                {
                    trialBalance.Add($"{account.AccountCode} - {account.Name}", balance);
                }
            }

            return trialBalance;
        }

        public async Task<(decimal Income, decimal Expenses, decimal NetProfit)> GetProfitLossAsync(DateTime startDate, DateTime endDate)
        {
            // Get all journal entries in date range
            var entries = await _context.JournalEntries
                .Include(j => j.Lines!)
                .ThenInclude(l => l.Account)
                .Where(j => j.EntryDate >= startDate && j.EntryDate <= endDate && j.IsApproved)
                .ToListAsync();

            decimal income = 0;
            decimal expenses = 0;

            foreach (var entry in entries)
            {
                foreach (var line in entry.Lines!)
                {
                    if (line.Account!.Type == "Income")
                    {
                        income += line.Credit;
                    }
                    else if (line.Account.Type == "Expense")
                    {
                        expenses += line.Debit;
                    }
                }
            }

            var netProfit = income - expenses;
            return (income, expenses, netProfit);
        }

        // ============================================================
        // PRIVATE HELPERS
        // ============================================================

        private async Task<string> GenerateEntryNumberAsync()
        {
            var lastEntry = await _context.JournalEntries
                .OrderByDescending(j => j.Id)
                .FirstOrDefaultAsync();

            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month;

            if (lastEntry == null)
            {
                return $"JE-{year}{month:D2}-0001";
            }

            var lastNumber = lastEntry.EntryNumber.Split('-').Last();
            var nextNumber = int.Parse(lastNumber) + 1;

            return $"JE-{year}{month:D2}-{nextNumber:D4}";
        }
    }
}