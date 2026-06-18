using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLedger.API.Data;
using SmartLedger.API.DTOs;
using SmartLedger.API.Extensions;
using SmartLedger.API.Services;

namespace SmartLedger.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryService _inventoryService;
        private readonly ILedgerService _ledgerService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ApplicationDbContext context,
            IInventoryService inventoryService,
            ILedgerService ledgerService,
            ILogger<DashboardController> logger)
        {
            _context = context;
            _inventoryService = inventoryService;
            _ledgerService = ledgerService;
            _logger = logger;
        }

        // ============================================================
        // GET: api/dashboard/summary
        // Get complete dashboard summary
        // ============================================================
        [HttpGet("summary")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            var userId = User.GetSmartLedgerUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            // Inventory stats
            var totalProducts = await _context.Products.CountAsync(p => p.IsActive && p.UserId == userId);
            var totalValue = await _inventoryService.GetTotalInventoryValueAsync(userId);
            var expectedRevenue = await _inventoryService.GetTotalExpectedRevenueAsync(userId);
            var lowStockProducts = await _inventoryService.GetLowStockProductsAsync(userId, 10);
            var lowStockCount = lowStockProducts.Count();

            // Todo stats
            var pendingTasks = await _context.TodoItems.CountAsync(t => t.Status != "COMPLETED" && t.IsActive && t.UserId == userId);
            var completedTasks = await _context.TodoItems.CountAsync(t => t.Status == "COMPLETED" && t.IsActive && t.UserId == userId);

            // Accounting stats (last 30 days)
            var startDate = DateTime.UtcNow.AddDays(-30);
            var (income, expenses, netProfit) = await _ledgerService.GetProfitLossAsync(startDate, DateTime.UtcNow, userId);

            // FIX: Convert decimal to double for ProfitMargin
            var profitMarginValue = income > 0 ? (double)((netProfit / income) * 100) : 0;

            var response = new DashboardSummaryDto
            {
                Inventory = new InventorySummaryDto
                {
                    TotalProducts = totalProducts,
                    TotalInventoryValue = totalValue,
                    TotalExpectedRevenue = expectedRevenue,
                    LowStockCount = lowStockCount,
                    LowStockProducts = lowStockProducts.Take(5).Select(p => new LowStockProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Quantity = p.Quantity,
                        LowStockThreshold = p.LowStockThreshold
                    }).ToList()
                },
                Tasks = new TaskSummaryDto
                {
                    Pending = pendingTasks,
                    Completed = completedTasks,
                    CompletionRate = (pendingTasks + completedTasks) > 0
                        ? Math.Round((double)completedTasks / (pendingTasks + completedTasks) * 100, 2)
                        : 0
                },
                Accounting = new AccountingSummaryDto
                {
                    PeriodFrom = startDate,
                    PeriodTo = DateTime.UtcNow,
                    Income = income,
                    Expenses = expenses,
                    NetProfit = netProfit,
                    ProfitMargin = profitMarginValue  // ← FIXED: Now uses double
                },
                LastUpdated = DateTime.UtcNow
            };

            return Ok(response);
        }

        // ============================================================
        // GET: api/dashboard/recent-activity
        // Get recent activity across all modules
        // ============================================================
        [HttpGet("recent-activity")]
        public async Task<IActionResult> GetRecentActivity([FromQuery] int limit = 20)
        {
            var userId = User.GetSmartLedgerUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var activities = new List<RecentActivityDto>();

            // Recent inventory transactions
            var recentTransactions = await _context.InventoryTransactions
                .Include(t => t.Product)
                .Where(t => t.UserId == userId && t.IsActive)
                .OrderByDescending(t => t.CreatedAt)
                .Take(limit)
                .Select(t => new RecentActivityDto
                {
                    Type = "INVENTORY",
                    Action = t.TransactionType,
                    Description = $"{t.TransactionType}: {Math.Abs(t.QuantityChange)} units of {t.Product!.Name}",
                    Timestamp = t.CreatedAt,
                    ReferenceId = t.ProductId,
                    ReferenceName = t.Product!.Name
                })
                .ToListAsync();

            activities.AddRange(recentTransactions);

            // Recent journal entries
            var recentEntries = await _context.JournalEntries
                .Where(j => j.UserId == userId && j.IsActive)
                .OrderByDescending(j => j.CreatedAt)
                .Take(limit)
                .Select(j => new RecentActivityDto
                {
                    Type = "ACCOUNTING",
                    Action = j.IsApproved ? "APPROVED" : "PENDING",
                    Description = $"Journal entry {j.EntryNumber}: {j.Description}",
                    Timestamp = j.CreatedAt,
                    ReferenceId = j.Id,
                    ReferenceName = j.EntryNumber
                })
                .ToListAsync();

            activities.AddRange(recentEntries);

            // Recent todo completions
            var recentTodos = await _context.TodoItems
                .Where(t => t.CompletedAt.HasValue && t.UserId == userId && t.IsActive)
                .OrderByDescending(t => t.CompletedAt)
                .Take(limit)
                .Select(t => new RecentActivityDto
                {
                    Type = "TASK",
                    Action = "COMPLETED",
                    Description = $"Task completed: {t.Title}",
                    Timestamp = t.CompletedAt!.Value,
                    ReferenceId = t.Id,
                    ReferenceName = t.Title
                })
                .ToListAsync();

            activities.AddRange(recentTodos);

            // Sort by timestamp and take top limit
            var allActivities = activities
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToList();

            return Ok(allActivities);
        }

        // ============================================================
        // GET: api/dashboard/quick-stats
        // Get quick stats cards
        // ============================================================
        [HttpGet("quick-stats")]
        public async Task<IActionResult> GetQuickStats()
        {
            var userId = User.GetSmartLedgerUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var totalProducts = await _context.Products.CountAsync(p => p.IsActive && p.UserId == userId);
            var lowStockCount = (await _inventoryService.GetLowStockProductsAsync(userId)).Count();
            var pendingTasks = await _context.TodoItems.CountAsync(t => t.Status != "COMPLETED" && t.IsActive && t.UserId == userId);
            var totalValue = await _inventoryService.GetTotalInventoryValueAsync(userId);

            var startDate = DateTime.UtcNow.AddDays(-30);
            var (income, expenses, netProfit) = await _ledgerService.GetProfitLossAsync(startDate, DateTime.UtcNow, userId);

            var response = new QuickStatsDto
            {
                Products = new ProductStatsDto { Total = totalProducts, LowStock = lowStockCount },
                Tasks = new TaskStatsDto { Pending = pendingTasks },
                Inventory = new InventoryStatsDto { Value = totalValue },
                Profit = new ProfitStatsDto { Amount = netProfit, Period = "Last 30 days" }
            };

            return Ok(response);
        }
    }
}
