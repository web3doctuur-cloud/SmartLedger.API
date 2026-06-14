namespace SmartLedger.API.DTOs
{
    // ============================================================
    // DASHBOARD DTOs
    // ============================================================

    /// <summary>
    /// Dashboard summary response
    /// </summary>
    public class DashboardSummaryDto
    {
        public InventorySummaryDto Inventory { get; set; } = new();
        public TaskSummaryDto Tasks { get; set; } = new();
        public AccountingSummaryDto Accounting { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    public class InventorySummaryDto
    {
        public int TotalProducts { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public decimal TotalExpectedRevenue { get; set; }
        public int LowStockCount { get; set; }
        public List<LowStockProductDto> LowStockProducts { get; set; } = new();
    }

    public class LowStockProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int LowStockThreshold { get; set; }
    }

    public class TaskSummaryDto
    {
        public int Pending { get; set; }
        public int Completed { get; set; }
        public double CompletionRate { get; set; }
    }

    public class AccountingSummaryDto
    {
        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }
        public decimal Income { get; set; }
        public decimal Expenses { get; set; }
        public decimal NetProfit { get; set; }
        public double ProfitMargin { get; set; }
    }

    /// <summary>
    /// Recent activity item
    /// </summary>
    public class RecentActivityDto
    {
        public string Type { get; set; } = string.Empty; // INVENTORY, ACCOUNTING, TASK
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int? ReferenceId { get; set; }
        public string? ReferenceName { get; set; }
    }

    /// <summary>
    /// Quick stats for dashboard cards
    /// </summary>
    public class QuickStatsDto
    {
        public ProductStatsDto Products { get; set; } = new();
        public TaskStatsDto Tasks { get; set; } = new();
        public InventoryStatsDto Inventory { get; set; } = new();
        public ProfitStatsDto Profit { get; set; } = new();
    }

    public class ProductStatsDto
    {
        public int Total { get; set; }
        public int LowStock { get; set; }
    }

    public class TaskStatsDto
    {
        public int Pending { get; set; }
    }

    public class InventoryStatsDto
    {
        public decimal Value { get; set; }
    }

    public class ProfitStatsDto
    {
        public decimal Amount { get; set; }
        public string Period { get; set; } = string.Empty;
    }
}
