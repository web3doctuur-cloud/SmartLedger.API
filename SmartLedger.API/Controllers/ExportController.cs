using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SmartLedger.API.Data;
using SmartLedger.API.Services;
using System.Text;

namespace SmartLedger.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryService _inventoryService;
        private readonly ILedgerService _ledgerService;  // ← ADD THIS FIELD
        private readonly ILogger<ExportController> _logger;

        public ExportController(
            ApplicationDbContext context,
            IInventoryService inventoryService,
            ILedgerService ledgerService,  // ← ADD THIS PARAMETER
            ILogger<ExportController> logger)
        {
            _context = context;
            _inventoryService = inventoryService;
            _ledgerService = ledgerService;  // ← ADD THIS ASSIGNMENT
            _logger = logger;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        // ============================================================
        // GET: api/export/products/excel
        // Export products to Excel
        // ============================================================
        [HttpGet("products/excel")]
        public async Task<IActionResult> ExportProductsToExcel()
        {
            var products = await _inventoryService.GetAllProductsAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Products");

            // Headers
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Name";
            worksheet.Cells[1, 3].Value = "Category";
            worksheet.Cells[1, 4].Value = "Quantity";
            worksheet.Cells[1, 5].Value = "Cost Price";
            worksheet.Cells[1, 6].Value = "Selling Price";
            worksheet.Cells[1, 7].Value = "Expected Revenue";
            worksheet.Cells[1, 8].Value = "Total Cost";
            worksheet.Cells[1, 9].Value = "Potential Profit";
            worksheet.Cells[1, 10].Value = "Profit Margin (%)";
            worksheet.Cells[1, 11].Value = "Low Stock";
            worksheet.Cells[1, 12].Value = "Created At";

            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 12])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Data
            var row = 2;
            foreach (var product in products)
            {
                worksheet.Cells[row, 1].Value = product.Id;
                worksheet.Cells[row, 2].Value = product.Name;
                worksheet.Cells[row, 3].Value = product.Category;
                worksheet.Cells[row, 4].Value = product.Quantity;
                worksheet.Cells[row, 5].Value = product.CostPrice;
                worksheet.Cells[row, 6].Value = product.SellingPrice;
                worksheet.Cells[row, 7].Value = product.ExpectedRevenue;
                worksheet.Cells[row, 8].Value = product.TotalCost;
                worksheet.Cells[row, 9].Value = product.PotentialProfit;
                worksheet.Cells[row, 10].Value = (double)product.ProfitMargin;
                worksheet.Cells[row, 11].Value = product.IsLowStock ? "Yes" : "No";
                worksheet.Cells[row, 12].Value = product.CreatedAt.ToString("yyyy-MM-dd");
                row++;
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            var bytes = await package.GetAsByteArrayAsync();
            var fileName = $"Products_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ============================================================
        // GET: api/export/transactions/excel
        // Export inventory transactions to Excel
        // ============================================================
        [HttpGet("transactions/excel")]
        public async Task<IActionResult> ExportTransactionsToExcel(
            [FromQuery] int productId,
            [FromQuery] int days = 30)
        {
            var product = await _inventoryService.GetProductByIdAsync(productId);
            if (product == null)
                return NotFound(new { message = "Product not found" });

            var transactions = await _inventoryService.GetTransactionHistoryAsync(productId, days);

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add($"Transactions_{product.Name}");

            // Headers
            worksheet.Cells[1, 1].Value = "Date";
            worksheet.Cells[1, 2].Value = "Type";
            worksheet.Cells[1, 3].Value = "Quantity Change";
            worksheet.Cells[1, 4].Value = "Previous Quantity";
            worksheet.Cells[1, 5].Value = "New Quantity";
            worksheet.Cells[1, 6].Value = "Unit Price";
            worksheet.Cells[1, 7].Value = "Total Amount";
            worksheet.Cells[1, 8].Value = "Reference";
            worksheet.Cells[1, 9].Value = "Notes";

            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 9])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Data
            var row = 2;
            foreach (var transaction in transactions)
            {
                worksheet.Cells[row, 1].Value = transaction.CreatedAt.ToString("yyyy-MM-dd HH:mm");
                worksheet.Cells[row, 2].Value = transaction.TransactionType;
                worksheet.Cells[row, 3].Value = transaction.QuantityChange;
                worksheet.Cells[row, 4].Value = transaction.PreviousQuantity;
                worksheet.Cells[row, 5].Value = transaction.NewQuantity;
                worksheet.Cells[row, 6].Value = transaction.UnitPrice;
                worksheet.Cells[row, 7].Value = transaction.TotalAmount;
                worksheet.Cells[row, 8].Value = transaction.ReferenceNumber;
                worksheet.Cells[row, 9].Value = transaction.Notes;
                row++;
            }

            worksheet.Cells.AutoFitColumns();

            var bytes = await package.GetAsByteArrayAsync();
            var fileName = $"Transactions_{product.Name}_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ============================================================
        // GET: api/export/sales/csv
        // Export sales report to CSV
        // ============================================================
        [HttpGet("sales/csv")]
        public async Task<IActionResult> ExportSalesToCsv(
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
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Date,Product,Quantity,Unit Price,Total Amount,Reference,Notes");

            foreach (var sale in sales)
            {
                csv.AppendLine($"\"{sale.CreatedAt:yyyy-MM-dd HH:mm}\",\"{sale.Product?.Name}\",{Math.Abs(sale.QuantityChange)},{sale.UnitPrice},{sale.TotalAmount},\"{sale.ReferenceNumber}\",\"{sale.Notes}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"Sales_Report_{DateTime.Now:yyyyMMdd}.csv";

            return File(bytes, "text/csv", fileName);
        }

        // ============================================================
        // GET: api/export/inventory-summary/excel
        // Export inventory summary report to Excel
        // ============================================================
        [HttpGet("inventory-summary/excel")]
        public async Task<IActionResult> ExportInventorySummaryToExcel()
        {
            var products = await _inventoryService.GetAllProductsAsync();
            var byCategory = await _inventoryService.GetInventoryValueByCategoryAsync();

            using var package = new ExcelPackage();

            // Summary Sheet
            var summarySheet = package.Workbook.Worksheets.Add("Summary");
            summarySheet.Cells[1, 1].Value = "Metric";
            summarySheet.Cells[1, 2].Value = "Value";
            summarySheet.Cells[1, 1, 1, 2].Style.Font.Bold = true;

            summarySheet.Cells[2, 1].Value = "Total Products";
            summarySheet.Cells[2, 2].Value = products.Count();
            summarySheet.Cells[3, 1].Value = "Total Quantity";
            summarySheet.Cells[3, 2].Value = products.Sum(p => p.Quantity);
            summarySheet.Cells[4, 1].Value = "Total Inventory Value";
            summarySheet.Cells[4, 2].Value = products.Sum(p => p.TotalCost);
            summarySheet.Cells[5, 1].Value = "Total Expected Revenue";
            summarySheet.Cells[5, 2].Value = products.Sum(p => p.ExpectedRevenue);
            summarySheet.Cells[6, 1].Value = "Total Potential Profit";
            summarySheet.Cells[6, 2].Value = products.Sum(p => p.PotentialProfit);

            // By Category Sheet
            var categorySheet = package.Workbook.Worksheets.Add("By Category");
            categorySheet.Cells[1, 1].Value = "Category";
            categorySheet.Cells[1, 2].Value = "Value";
            categorySheet.Cells[1, 1, 1, 2].Style.Font.Bold = true;

            var row = 2;
            foreach (var category in byCategory)
            {
                categorySheet.Cells[row, 1].Value = category.Key;
                categorySheet.Cells[row, 2].Value = category.Value;
                row++;
            }

            categorySheet.Cells.AutoFitColumns();

            // Products Sheet
            var productsSheet = package.Workbook.Worksheets.Add("Products");
            productsSheet.Cells[1, 1].Value = "Name";
            productsSheet.Cells[1, 2].Value = "Category";
            productsSheet.Cells[1, 3].Value = "Quantity";
            productsSheet.Cells[1, 4].Value = "Cost Price";
            productsSheet.Cells[1, 5].Value = "Selling Price";
            productsSheet.Cells[1, 6].Value = "Expected Revenue";
            productsSheet.Cells[1, 7].Value = "Potential Profit";
            productsSheet.Cells[1, 1, 1, 7].Style.Font.Bold = true;

            row = 2;
            foreach (var product in products)
            {
                productsSheet.Cells[row, 1].Value = product.Name;
                productsSheet.Cells[row, 2].Value = product.Category;
                productsSheet.Cells[row, 3].Value = product.Quantity;
                productsSheet.Cells[row, 4].Value = product.CostPrice;
                productsSheet.Cells[row, 5].Value = product.SellingPrice;
                productsSheet.Cells[row, 6].Value = product.ExpectedRevenue;
                productsSheet.Cells[row, 7].Value = product.PotentialProfit;
                row++;
            }

            productsSheet.Cells.AutoFitColumns();

            var bytes = await package.GetAsByteArrayAsync();
            var fileName = $"Inventory_Summary_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ============================================================
        // GET: api/export/ledger/excel
        // Export general ledger to Excel
        // ============================================================
        [HttpGet("ledger/excel")]
        public async Task<IActionResult> ExportLedgerToExcel()
        {
            var accounts = await _context.Accounts
                .Where(a => a.IsActive)
                .ToListAsync();

            using var package = new ExcelPackage();
            var ledgerSheet = package.Workbook.Worksheets.Add("General Ledger");

            ledgerSheet.Cells[1, 1].Value = "Account Code";
            ledgerSheet.Cells[1, 2].Value = "Account Name";
            ledgerSheet.Cells[1, 3].Value = "Account Type";
            ledgerSheet.Cells[1, 4].Value = "Normal Side";
            ledgerSheet.Cells[1, 5].Value = "Current Balance";
            ledgerSheet.Cells[1, 1, 1, 5].Style.Font.Bold = true;

            var row = 2;
            decimal totalDebits = 0;
            decimal totalCredits = 0;

            foreach (var account in accounts)
            {
                var balance = await _ledgerService.GetAccountBalanceAsync(account.Id);

                ledgerSheet.Cells[row, 1].Value = account.AccountCode;
                ledgerSheet.Cells[row, 2].Value = account.Name;
                ledgerSheet.Cells[row, 3].Value = account.Type;
                ledgerSheet.Cells[row, 4].Value = account.NormalSide;
                ledgerSheet.Cells[row, 5].Value = balance;

                if (balance > 0)
                    totalDebits += balance;
                else
                    totalCredits += Math.Abs(balance);

                row++;
            }

            // Add totals row
            ledgerSheet.Cells[row, 4].Value = "TOTALS:";
            ledgerSheet.Cells[row, 4].Style.Font.Bold = true;
            ledgerSheet.Cells[row, 5].Value = totalDebits;
            ledgerSheet.Cells[row + 1, 5].Value = totalCredits;
            ledgerSheet.Cells[row + 1, 4].Value = "Total Credits:";
            ledgerSheet.Cells[row + 1, 4].Style.Font.Bold = true;

            ledgerSheet.Cells.AutoFitColumns();

            var bytes = await package.GetAsByteArrayAsync();
            var fileName = $"General_Ledger_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}