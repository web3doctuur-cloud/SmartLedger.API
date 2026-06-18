using Microsoft.EntityFrameworkCore;
using SmartLedger.API.Data;
using SmartLedger.API.DTOs;
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

        public async Task<Account?> GetAccountByIdAsync(int id, string userId)
        {
            return await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId && a.IsActive);
        }

        public async Task<IEnumerable<Account>> GetAllAccountsAsync(string userId)
        {
            return await _context.Accounts
                .Where(a => a.IsActive && a.UserId == userId)
                .OrderBy(a => a.AccountCode)
                .ToListAsync();
        }

        public async Task<Account> CreateAccountAsync(Account account, string userId)
        {
            account.UserId = userId;
            account.CreatedAt = DateTime.UtcNow;
            account.IsActive = true;

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Account created: {account.AccountCode} - {account.Name}");
            return account;
        }

        public async Task<Account?> UpdateAccountAsync(int id, Account account, string userId)
        {
            var existingAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (existingAccount == null) return null;

            existingAccount.Name = account.Name;
            existingAccount.Type = account.Type;
            existingAccount.NormalSide = account.NormalSide;
            existingAccount.ParentAccountId = account.ParentAccountId;
            existingAccount.IsActive = account.IsActive;
            existingAccount.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existingAccount;
        }

        public async Task<bool> DeleteAccountAsync(int id, string userId)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (account == null) return false;

            account.IsActive = false;
            account.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        // ============================================================
        // JOURNAL ENTRIES
        // ============================================================

        public async Task<JournalEntry> CreateJournalEntryAsync(JournalEntry entry, List<JournalEntryLine> lines, string userId)
        {
            // Validate double-entry accounting
            if (!ValidateDoubleEntry(lines))
            {
                throw new InvalidOperationException("Total debits must equal total credits");
            }

            var accountIds = lines.Select(line => line.AccountId).Distinct().ToList();
            var validAccountCount = await _context.Accounts
                .CountAsync(a => accountIds.Contains(a.Id) && a.UserId == userId && a.IsActive);

            if (validAccountCount != accountIds.Count)
            {
                throw new InvalidOperationException("One or more accounts do not belong to the current user");
            }

            // Generate unique entry number
            entry.EntryNumber = await GenerateEntryNumberAsync(userId);
            entry.UserId = userId;
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

        public async Task<JournalEntryResponseDto?> GetJournalEntryByIdAsync(int id, string userId)
        {
            var entry = await _context.JournalEntries
                .FirstOrDefaultAsync(j => j.Id == id && j.UserId == userId && j.IsActive);

            if (entry == null) return null;

            var lines = await _context.JournalEntryLines
                .Where(l => l.JournalEntryId == entry.Id)
                .Include(l => l.Account)
                .ToListAsync();

            return new JournalEntryResponseDto
            {
                Id = entry.Id,
                EntryNumber = entry.EntryNumber,
                EntryDate = entry.EntryDate,
                Description = entry.Description,
                IsApproved = entry.IsApproved,
                ApprovedAt = entry.ApprovedAt,
                CreatedAt = entry.CreatedAt,
                Lines = lines.Select(line => new JournalEntryLineResponseDto
                {
                    Id = line.Id,
                    AccountId = line.AccountId,
                    AccountCode = line.Account?.AccountCode ?? "",
                    AccountName = line.Account?.Name ?? "",
                    AccountType = line.Account?.Type ?? "",
                    Debit = line.Debit,
                    Credit = line.Credit,
                    LineDescription = line.LineDescription,
                    ReferenceNumber = line.ReferenceNumber,
                    TaxAmount = line.TaxAmount,
                    TotalAmount = line.TotalAmount
                }).ToList()
            };
        }

        public async Task<IEnumerable<JournalEntryResponseDto>> GetAllJournalEntriesAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var query = _context.JournalEntries
                    .Where(j => j.IsActive && j.UserId == userId);

                if (fromDate.HasValue)
                    query = query.Where(j => j.EntryDate >= fromDate.Value);
                if (toDate.HasValue)
                    query = query.Where(j => j.EntryDate <= toDate.Value);

                var entries = await query
                    .OrderByDescending(j => j.EntryDate)
                    .ToListAsync();

                var results = new List<JournalEntryResponseDto>();

                foreach (var entry in entries)
                {
                    var lines = await _context.JournalEntryLines
                        .Where(l => l.JournalEntryId == entry.Id)
                        .Include(l => l.Account)
                        .ToListAsync();

                    results.Add(new JournalEntryResponseDto
                    {
                        Id = entry.Id,
                        EntryNumber = entry.EntryNumber,
                        EntryDate = entry.EntryDate,
                        Description = entry.Description,
                        IsApproved = entry.IsApproved,
                        ApprovedAt = entry.ApprovedAt,
                        CreatedAt = entry.CreatedAt,
                        Lines = lines.Select(line => new JournalEntryLineResponseDto
                        {
                            Id = line.Id,
                            AccountId = line.AccountId,
                            AccountCode = line.Account?.AccountCode ?? "",
                            AccountName = line.Account?.Name ?? "",
                            AccountType = line.Account?.Type ?? "",
                            Debit = line.Debit,
                            Credit = line.Credit,
                            LineDescription = line.LineDescription,
                            ReferenceNumber = line.ReferenceNumber,
                            TaxAmount = line.TaxAmount,
                            TotalAmount = line.TotalAmount
                        }).ToList()
                    });
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting journal entries");
                throw;
            }
        }
        public async Task<bool> ApproveJournalEntryAsync(int id, string userId)
        {
            var entry = await _context.JournalEntries
                .FirstOrDefaultAsync(j => j.Id == id && j.UserId == userId && j.IsActive);
            if (entry == null) return false;

            entry.IsApproved = true;
            entry.ApprovedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Journal entry approved: {entry.EntryNumber}");
            return true;
        }

        public async Task<bool> DeleteJournalEntryAsync(int id, string userId)
        {
            var entry = await _context.JournalEntries
                .FirstOrDefaultAsync(j => j.Id == id && j.UserId == userId);
            if (entry == null) return false;

            entry.IsActive = false;
            entry.UpdatedAt = DateTime.UtcNow;
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

        public async Task<decimal> GetAccountBalanceAsync(int accountId, string userId, DateTime? asOfDate = null)
        {
            var lines = await _context.JournalEntryLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == accountId
                    && l.JournalEntry!.IsApproved
                    && l.JournalEntry!.UserId == userId
                    && (!asOfDate.HasValue || l.JournalEntry.EntryDate <= asOfDate.Value))
                .ToListAsync();

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);
            if (account == null) return 0;

            var totalDebits = lines.Sum(l => l.Debit);
            var totalCredits = lines.Sum(l => l.Credit);

            return account.NormalSide == "DEBIT"
                ? totalDebits - totalCredits
                : totalCredits - totalDebits;
        }

        public async Task<Dictionary<string, decimal>> GetTrialBalanceAsync(string userId, DateTime? asOfDate = null)
        {
            var accounts = await _context.Accounts
                .Where(a => a.IsActive && a.UserId == userId)
                .ToListAsync();

            var trialBalance = new Dictionary<string, decimal>();

            foreach (var account in accounts)
            {
                var balance = await GetAccountBalanceAsync(account.Id, userId, asOfDate);
                if (balance != 0)
                {
                    trialBalance.Add($"{account.AccountCode} - {account.Name}", balance);
                }
            }

            return trialBalance;
        }

        public async Task<(decimal Income, decimal Expenses, decimal NetProfit)> GetProfitLossAsync(DateTime startDate, DateTime endDate, string userId)
        {
            // Get all journal entries in date range
            var entries = await _context.JournalEntries
                .Include(j => j.Lines!)
                .ThenInclude(l => l.Account)
                .Where(j => j.EntryDate >= startDate && j.EntryDate <= endDate && j.IsApproved && j.UserId == userId)
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

        private async Task<string> GenerateEntryNumberAsync(string userId)
        {
            var lastEntry = await _context.JournalEntries
                .Where(j => j.UserId == userId)
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
