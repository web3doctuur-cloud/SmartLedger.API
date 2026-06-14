using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLedger.API.Data;
using SmartLedger.API.Services;

namespace SmartLedger.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryService _inventoryService;
        private readonly ILedgerService _ledgerService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            ApplicationDbContext context,
            IInventoryService inventoryService,
            ILedgerService ledgerService,
            ILogger<ReportsController> logger)
        {
            _context = context;
            _inventoryService = inventoryService;
            _ledgerService = ledgerService;
            _logger = logger;
        }

        // ============================================================
        // INCOME STATEMENT (Profit & Loss)
        // ============================================================
        [HttpGet("income-statement")]
        public async Task<IActionResult> GetIncomeStatement(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            // Default to current month if no dates provided
            if (!startDate.HasValue)
                startDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            if (!endDate.HasValue)
                endDate = DateTime.UtcNow;

            var (income, expenses, netProfit) = await _ledgerService.GetProfitLossAsync(startDate.Value, endDate.Value);

            // Get detailed breakdowns
            var incomeAccounts = await _context.JournalEntries
                .Include(j => j.Lines!)
                .ThenInclude(l => l.Account)
                .Where(j => j.EntryDate >= startDate && j.EntryDate <= endDate && j.IsApproved)
                .SelectMany(j => j.Lines!)
                .Where(l => l.Account!.Type == "Income")
                .GroupBy(l => new { l.Account!.Id, l.Account!.Name, l.Account!.AccountCode })
                .Select(g => new
                {
                    AccountId = g.Key.Id,
                    AccountCode = g.Key.AccountCode,
                    AccountName = g.Key.Name,
                    Amount = g.Sum(l => l.Credit - l.Debit)
                })
                .ToListAsync();

            var expenseAccounts = await _context.JournalEntries
                .Include(j => j.Lines!)
                .ThenInclude(l => l.Account)
                .Where(j => j.EntryDate >= startDate && j.EntryDate <= endDate && j.IsApproved)
                .SelectMany(j => j.Lines!)
                .Where(l => l.Account!.Type == "Expense")
                .GroupBy(l => new { l.Account!.Id, l.Account!.Name, l.Account!.AccountCode })
                .Select(g => new
                {
                    AccountId = g.Key.Id,
                    AccountCode = g.Key.AccountCode,
                    AccountName = g.Key.Name,
                    Amount = g.Sum(l => l.Debit - l.Credit)
                })
                .ToListAsync();

            return Ok(new
            {
                Period = new { From = startDate, To = endDate },
                Income = new
                {
                    Total = income,
                    Details = incomeAccounts
                },
                Expenses = new
                {
                    Total = expenses,
                    Details = expenseAccounts
                },
                NetProfit = netProfit,
                IsProfit = netProfit > 0,
                ProfitMargin = income > 0 ? Math.Round((netProfit / income) * 100, 2) : 0
            });
        }

        // ============================================================
        // BALANCE SHEET
        // ============================================================
        [HttpGet("balance-sheet")]
        public async Task<IActionResult> GetBalanceSheet([FromQuery] DateTime? asOfDate)
        {
            var date = asOfDate ?? DateTime.UtcNow;

            // Get all accounts with their balances
            var accounts = await _context.Accounts
                .Where(a => a.IsActive)
                .ToListAsync();

            var assets = new List<object>();
            var liabilities = new List<object>();
            var equity = new List<object>();

            decimal totalAssets = 0;
            decimal totalLiabilities = 0;
            decimal totalEquity = 0;

            foreach (var account in accounts)
            {
                var balance = await _ledgerService.GetAccountBalanceAsync(account.Id);

                if (balance != 0)
                {
                    var accountInfo = new
                    {
                        account.Id,
                        account.AccountCode,
                        account.Name,
                        Balance = balance
                    };

                    switch (account.Type)
                    {
                        case "Asset":
                            assets.Add(accountInfo);
                            totalAssets += balance;
                            break;
                        case "Liability":
                            liabilities.Add(accountInfo);
                            totalLiabilities += balance;
                            break;
                        case "Equity":
                            equity.Add(accountInfo);
                            totalEquity += balance;
                            break;
                    }
                }
            }

            return Ok(new
            {
                AsOfDate = date,
                Assets = new
                {
                    Total = totalAssets,
                    Details = assets
                },
                Liabilities = new
                {
                    Total = totalLiabilities,
                    Details = liabilities
                },
                Equity = new
                {
                    Total = totalEquity,
                    Details = equity
                },
                TotalLiabilitiesAndEquity = totalLiabilities + totalEquity,
                IsBalanced = totalAssets == (totalLiabilities + totalEquity)
            });
        }

        // ============================================================
        // TRIAL BALANCE
        // ============================================================
        [HttpGet("trial-balance")]
        public async Task<IActionResult> GetTrialBalance([FromQuery] DateTime? asOfDate)
        {
            var date = asOfDate ?? DateTime.UtcNow;

            var trialBalance = await _ledgerService.GetTrialBalanceAsync();

            var totalDebits = trialBalance.Values.Where(v => v > 0).Sum();
            var totalCredits = trialBalance.Values.Where(v => v < 0).Sum() * -1;

            return Ok(new
            {
                AsOfDate = date,
                Accounts = trialBalance.Select(kv => new
                {
                    Account = kv.Key,
                    Debit = kv.Value > 0 ? kv.Value : 0,
                    Credit = kv.Value < 0 ? Math.Abs(kv.Value) : 0
                }),
                Totals = new
                {
                    Debits = totalDebits,
                    Credits = totalCredits,
                    IsBalanced = totalDebits == totalCredits
                }
            });
        }

        // ============================================================
        // INVENTORY SUMMARY REPORT
        // ============================================================
        [HttpGet("inventory-summary")]
        public async Task<IActionResult> GetInventorySummary()
        {
            var products = await _inventoryService.GetAllProductsAsync();
            var byCategory = await _inventoryService.GetInventoryValueByCategoryAsync();
            var lowStock = await _inventoryService.GetLowStockProductsAsync();

            return Ok(new
            {
                Summary = new
                {
                    TotalProducts = products.Count(),
                    TotalQuantity = products.Sum(p => p.Quantity),
                    TotalInventoryValue = products.Sum(p => p.TotalCost),
                    TotalExpectedRevenue = products.Sum(p => p.ExpectedRevenue),
                    TotalPotentialProfit = products.Sum(p => p.PotentialProfit)
                },
                ByCategory = byCategory.Select(kv => new
                {
                    Category = kv.Key,
                    Value = kv.Value,
                    Percentage = products.Sum(p => p.TotalCost) > 0
                        ? (kv.Value / products.Sum(p => p.TotalCost)) * 100
                        : 0
                }),
                LowStockProducts = lowStock.Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Quantity,
                    p.LowStockThreshold,
                    p.SellingPrice
                }),
                TopProducts = products
                    .OrderByDescending(p => p.PotentialProfit)
                    .Take(10)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Quantity,
                        p.CostPrice,
                        p.SellingPrice,
                        p.PotentialProfit
                    })
            });
        }

        // ============================================================
        // INVENTORY VALUATION REPORT
        // ============================================================
        [HttpGet("inventory-valuation")]
        public async Task<IActionResult> GetInventoryValuation(
            [FromQuery] string method = "FIFO") // FIFO, LIFO, WeightedAverage
        {
            var products = await _inventoryService.GetAllProductsAsync();

            var valuation = new List<object>();

            foreach (var product in products)
            {
                // Get all purchase transactions for this product
                var purchases = await _context.InventoryTransactions
                    .Where(t => t.ProductId == product.Id && t.TransactionType == "PURCHASE")
                    .OrderBy(t => t.CreatedAt)
                    .ToListAsync();

                decimal valuationValue = 0;

                switch (method.ToUpper())
                {
                    case "FIFO": // First In First Out
                        var remainingQty = product.Quantity;
                        foreach (var purchase in purchases)
                        {
                            var qtyToUse = Math.Min(purchase.QuantityChange, remainingQty);
                            if (purchase.UnitPrice.HasValue)
                                valuationValue += qtyToUse * purchase.UnitPrice.Value;
                            remainingQty -= qtyToUse;
                            if (remainingQty <= 0) break;
                        }
                        break;

                    case "LIFO": // Last In First Out
                        remainingQty = product.Quantity;
                        foreach (var purchase in purchases.OrderByDescending(t => t.CreatedAt))
                        {
                            var qtyToUse = Math.Min(purchase.QuantityChange, remainingQty);
                            if (purchase.UnitPrice.HasValue)
                                valuationValue += qtyToUse * purchase.UnitPrice.Value;
                            remainingQty -= qtyToUse;
                            if (remainingQty <= 0) break;
                        }
                        break;

                    default: // Weighted Average
                        var totalCost = product.TotalCost;
                        valuationValue = totalCost;
                        break;
                }

                valuation.Add(new
                {
                    product.Id,
                    product.Name,
                    product.Quantity,
                    CurrentCostPrice = product.CostPrice,
                    CurrentTotalCost = product.TotalCost,
                    ValuationMethod = method,
                    ValuationValue = valuationValue,
                    Difference = product.TotalCost - valuationValue
                });
            }

            return Ok(new
            {
                Method = method,
                ValuationDate = DateTime.UtcNow,
                Products = valuation,
                TotalValuation = valuation.Sum(v => (decimal)v.GetType().GetProperty("ValuationValue")!.GetValue(v)!),
                TotalBookValue = products.Sum(p => p.TotalCost)
            });
        }

        // ============================================================
        // SALES REPORT (Inventory Movement)
        // ============================================================
        [HttpGet("sales-report")]
        public async Task<IActionResult> GetSalesReport(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            if (!startDate.HasValue)
                startDate = DateTime.UtcNow.AddDays(-30);
            if (!endDate.HasValue)
                endDate = DateTime.UtcNow;

            var sales = await _context.InventoryTransactions
                .Include(t => t.Product)
                .Where(t => t.TransactionType == "SALE"
                    && t.CreatedAt >= startDate
                    && t.CreatedAt <= endDate)
                .ToListAsync();

            var byProduct = sales
                .GroupBy(s => new { s.ProductId, ProductName = s.Product!.Name })
                .Select(g => new
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    QuantitySold = g.Sum(s => Math.Abs(s.QuantityChange)),
                    TotalRevenue = g.Sum(s => s.TotalAmount ?? 0),
                    Transactions = g.Count()
                })
                .OrderByDescending(g => g.TotalRevenue)
                .ToList();

            return Ok(new
            {
                Period = new { From = startDate, To = endDate },
                Summary = new
                {
                    TotalTransactions = sales.Count,
                    TotalQuantitySold = sales.Sum(s => Math.Abs(s.QuantityChange)),
                    TotalRevenue = sales.Sum(s => s.TotalAmount ?? 0),
                    AverageTransactionValue = sales.Any()
                        ? sales.Average(s => s.TotalAmount ?? 0)
                        : 0
                },
                ByProduct = byProduct,
                DailySales = sales
                    .GroupBy(s => s.CreatedAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Quantity = g.Sum(s => Math.Abs(s.QuantityChange)),
                        Revenue = g.Sum(s => s.TotalAmount ?? 0)
                    })
                    .OrderBy(g => g.Date)
            });
        }

        // ============================================================
        // EXPENSES REPORT
        // ============================================================
        [HttpGet("expenses-report")]
        public async Task<IActionResult> GetExpensesReport(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? category)
        {
            if (!startDate.HasValue)
                startDate = DateTime.UtcNow.AddDays(-30);
            if (!endDate.HasValue)
                endDate = DateTime.UtcNow;

            var expenseQuery = _context.JournalEntries
                .Include(j => j.Lines!)
                .ThenInclude(l => l.Account)
                .Where(j => j.EntryDate >= startDate
                    && j.EntryDate <= endDate
                    && j.IsApproved)
                .SelectMany(j => j.Lines!)
                .Where(l => l.Account!.Type == "Expense");

            if (!string.IsNullOrEmpty(category))
            {
                expenseQuery = expenseQuery.Where(l => l.Account!.Name.Contains(category));
            }

            var expenses = await expenseQuery
                .GroupBy(l => new { l.Account!.Id, l.Account!.Name, l.Account!.AccountCode })
                .Select(g => new
                {
                    AccountId = g.Key.Id,
                    AccountCode = g.Key.AccountCode,
                    AccountName = g.Key.Name,
                    Total = g.Sum(l => l.Debit - l.Credit)
                })
                .OrderByDescending(g => g.Total)
                .ToListAsync();

            return Ok(new
            {
                Period = new { From = startDate, To = endDate },
                TotalExpenses = expenses.Sum(e => e.Total),
                ExpensesByAccount = expenses,
                FilterCategory = category
            });
        }
    }
}
