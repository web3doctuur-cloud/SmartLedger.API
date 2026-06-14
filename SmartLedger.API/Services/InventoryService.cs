using Microsoft.EntityFrameworkCore;
using SmartLedger.API.Data;
using SmartLedger.API.Models;

namespace SmartLedger.API.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(ApplicationDbContext context, ILogger<InventoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ============================================================
        // PRODUCT MANAGEMENT
        // ============================================================

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category)
        {
            return await _context.Products
                .Where(p => p.Category == category && p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetLowStockProductsAsync(int threshold = 10)
        {
            return await _context.Products
                .Where(p => p.IsActive && p.Quantity <= threshold)
                .OrderBy(p => p.Quantity)
                .ToListAsync();
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            product.CreatedAt = DateTime.UtcNow;
            product.IsActive = true;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Create initial inventory transaction
            if (product.Quantity > 0)
            {
                await RecordTransactionAsync(product.Id, "PURCHASE", product.Quantity, 0, product.Quantity,
                    $"Initial stock added: {product.Quantity} units", product.CostPrice);
            }

            _logger.LogInformation($"Product created: {product.Name} (ID: {product.Id})");
            return product;
        }

        public async Task<Product?> UpdateProductAsync(int id, Product product)
        {
            var existingProduct = await _context.Products.FindAsync(id);
            if (existingProduct == null) return null;

            // Store old values for logging
            var oldQuantity = existingProduct.Quantity;
            var oldCostPrice = existingProduct.CostPrice;
            var oldSellingPrice = existingProduct.SellingPrice;

            // Update fields
            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Category = product.Category;
            existingProduct.CostPrice = product.CostPrice;
            existingProduct.SellingPrice = product.SellingPrice;
            existingProduct.LowStockThreshold = product.LowStockThreshold;
            existingProduct.ImageUrl = product.ImageUrl;
            existingProduct.SKU = product.SKU;
            existingProduct.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log significant changes
            if (oldCostPrice != product.CostPrice || oldSellingPrice != product.SellingPrice)
            {
                _logger.LogInformation($"Product price updated: {existingProduct.Name} (ID: {id})");
            }

            return existingProduct;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;

            // Soft delete
            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Product deleted (soft): {product.Name} (ID: {id})");

            return true;
        }

        // ============================================================
        // QUANTITY MANAGEMENT
        // ============================================================

        public async Task<Product?> IncreaseQuantityAsync(int id, int quantity, string? notes = null, decimal? unitPrice = null)
        {
            if (quantity <= 0) throw new ArgumentException("Quantity must be positive");

            var product = await _context.Products.FindAsync(id);
            if (product == null) return null;

            var oldQuantity = product.Quantity;
            var newQuantity = oldQuantity + quantity;

            await RecordTransactionAsync(id, "PURCHASE", quantity, oldQuantity, newQuantity, notes, unitPrice ?? product.CostPrice);

            product.Quantity = newQuantity;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Stock increased for {product.Name}: +{quantity} (now: {newQuantity})");

            return product;
        }

        public async Task<Product?> DecreaseQuantityAsync(int id, int quantity, string? notes = null, decimal? unitPrice = null)
        {
            if (quantity <= 0) throw new ArgumentException("Quantity must be positive");

            var product = await _context.Products.FindAsync(id);
            if (product == null) return null;

            if (product.Quantity < quantity)
                throw new InvalidOperationException($"Insufficient stock. Available: {product.Quantity}, Requested: {quantity}");

            var oldQuantity = product.Quantity;
            var newQuantity = oldQuantity - quantity;

            await RecordTransactionAsync(id, "SALE", -quantity, oldQuantity, newQuantity, notes, unitPrice ?? product.SellingPrice);

            product.Quantity = newQuantity;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Check if low stock alert needed
            if (product.IsLowStock)
            {
                _logger.LogWarning($"Low stock alert: {product.Name} has only {product.Quantity} units left");
            }

            return product;
        }

        public async Task<Product?> AdjustQuantityAsync(int id, int newQuantity, string? notes = null)
        {
            if (newQuantity < 0) throw new ArgumentException("Quantity cannot be negative");

            var product = await _context.Products.FindAsync(id);
            if (product == null) return null;

            var oldQuantity = product.Quantity;
            var quantityChange = newQuantity - oldQuantity;

            await RecordTransactionAsync(id, "ADJUSTMENT", quantityChange, oldQuantity, newQuantity, notes, null);

            product.Quantity = newQuantity;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Stock adjusted for {product.Name}: {oldQuantity} → {newQuantity}");

            return product;
        }

        // ============================================================
        // CALCULATIONS
        // ============================================================

        public async Task<decimal> GetTotalInventoryValueAsync()
        {
            return await _context.Products
                .Where(p => p.IsActive)
                .SumAsync(p => p.TotalCost);
        }

        public async Task<decimal> GetTotalExpectedRevenueAsync()
        {
            return await _context.Products
                .Where(p => p.IsActive)
                .SumAsync(p => p.ExpectedRevenue);
        }

        public async Task<decimal> GetTotalPotentialProfitAsync()
        {
            var products = await _context.Products
                .Where(p => p.IsActive)
                .ToListAsync();

            return products.Sum(p => p.PotentialProfit);
        }

        public async Task<Dictionary<string, decimal>> GetInventoryValueByCategoryAsync()
        {
            return await _context.Products
                .Where(p => p.IsActive && p.Category != null)
                .GroupBy(p => p.Category!)
                .Select(g => new
                {
                    Category = g.Key,
                    TotalValue = g.Sum(p => p.TotalCost)
                })
                .ToDictionaryAsync(k => k.Category, v => v.TotalValue);
        }

        // ============================================================
        // TRANSACTION HISTORY
        // ============================================================

        public async Task<IEnumerable<InventoryTransaction>> GetTransactionHistoryAsync(int productId, int days = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            return await _context.InventoryTransactions
                .Where(t => t.ProductId == productId && t.CreatedAt >= cutoffDate)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        // ============================================================
        // PRIVATE HELPERS
        // ============================================================

        private async Task RecordTransactionAsync(
            int productId,
            string transactionType,
            int quantityChange,
            int previousQuantity,
            int newQuantity,
            string? notes,
            decimal? unitPrice)
        {
            var transaction = new InventoryTransaction
            {
                ProductId = productId,
                TransactionType = transactionType,
                QuantityChange = quantityChange,
                PreviousQuantity = previousQuantity,
                NewQuantity = newQuantity,
                Notes = notes,
                UnitPrice = unitPrice,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.InventoryTransactions.Add(transaction);
            await _context.SaveChangesAsync();
        }
    }
}