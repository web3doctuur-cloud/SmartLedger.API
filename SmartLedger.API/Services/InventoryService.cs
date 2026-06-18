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

        public async Task<Product?> GetProductByIdAsync(int id, string userId)
        {
            return await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId && p.IsActive);
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync(string userId)
        {
            return await _context.Products
                .Where(p => p.IsActive && p.UserId == userId)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category, string userId)
        {
            return await _context.Products
                .Where(p => p.Category == category && p.IsActive && p.UserId == userId)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetLowStockProductsAsync(string userId, int threshold = 10)
        {
            return await _context.Products
                .Where(p => p.IsActive && p.Quantity <= threshold && p.UserId == userId)
                .OrderBy(p => p.Quantity)
                .ToListAsync();
        }

        public async Task<Product> CreateProductAsync(Product product, string userId)
        {
            product.UserId = userId;
            product.CreatedAt = DateTime.UtcNow;
            product.IsActive = true;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Create initial inventory transaction
            if (product.Quantity > 0)
            {
                await RecordTransactionAsync(product.Id, userId, "PURCHASE", product.Quantity, 0, product.Quantity,
                    $"Initial stock added: {product.Quantity} units", product.CostPrice);
            }

            _logger.LogInformation($"Product created: {product.Name} (ID: {product.Id})");
            return product;
        }

        public async Task<Product?> UpdateProductAsync(int id, Product product, string userId)
        {
            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
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

        public async Task<bool> DeleteProductAsync(int id, string userId)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
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

        public async Task<Product?> IncreaseQuantityAsync(int id, int quantity, string userId, string? notes = null, decimal? unitPrice = null)
        {
            if (quantity <= 0) throw new ArgumentException("Quantity must be positive");

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if (product == null) return null;

            var oldQuantity = product.Quantity;
            var newQuantity = oldQuantity + quantity;

            await RecordTransactionAsync(id, userId, "PURCHASE", quantity, oldQuantity, newQuantity, notes, unitPrice ?? product.CostPrice);

            product.Quantity = newQuantity;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Stock increased for {product.Name}: +{quantity} (now: {newQuantity})");

            return product;
        }

        public async Task<Product?> DecreaseQuantityAsync(int id, int quantity, string userId, string? notes = null, decimal? unitPrice = null)
        {
            if (quantity <= 0) throw new ArgumentException("Quantity must be positive");

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if (product == null) return null;

            if (product.Quantity < quantity)
                throw new InvalidOperationException($"Insufficient stock. Available: {product.Quantity}, Requested: {quantity}");

            var oldQuantity = product.Quantity;
            var newQuantity = oldQuantity - quantity;

            await RecordTransactionAsync(id, userId, "SALE", -quantity, oldQuantity, newQuantity, notes, unitPrice ?? product.SellingPrice);

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

        public async Task<Product?> AdjustQuantityAsync(int id, int newQuantity, string userId, string? notes = null)
        {
            if (newQuantity < 0) throw new ArgumentException("Quantity cannot be negative");

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if (product == null) return null;

            var oldQuantity = product.Quantity;
            var quantityChange = newQuantity - oldQuantity;

            await RecordTransactionAsync(id, userId, "ADJUSTMENT", quantityChange, oldQuantity, newQuantity, notes, null);

            product.Quantity = newQuantity;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Stock adjusted for {product.Name}: {oldQuantity} → {newQuantity}");

            return product;
        }

        // ============================================================
        // CALCULATIONS
        // ============================================================

        public async Task<decimal> GetTotalInventoryValueAsync(string userId)
        {
            return await _context.Products
                .Where(p => p.IsActive && p.UserId == userId)
                .SumAsync(p => p.TotalCost);
        }

        public async Task<decimal> GetTotalExpectedRevenueAsync(string userId)
        {
            return await _context.Products
                .Where(p => p.IsActive && p.UserId == userId)
                .SumAsync(p => p.ExpectedRevenue);
        }

        public async Task<decimal> GetTotalPotentialProfitAsync(string userId)
        {
            var products = await _context.Products
                .Where(p => p.IsActive && p.UserId == userId)
                .ToListAsync();

            return products.Sum(p => p.PotentialProfit);
        }

        public async Task<Dictionary<string, decimal>> GetInventoryValueByCategoryAsync(string userId)
        {
            return await _context.Products
                .Where(p => p.IsActive && p.Category != null && p.UserId == userId)
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

        public async Task<IEnumerable<InventoryTransaction>> GetTransactionHistoryAsync(int productId, string userId, int days = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            return await _context.InventoryTransactions
                .Include(t => t.Product)
                .Where(t => t.ProductId == productId && t.UserId == userId && t.CreatedAt >= cutoffDate)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        // ============================================================
        // PRIVATE HELPERS
        // ============================================================

        private async Task RecordTransactionAsync(
            int productId,
            string userId,
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
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.InventoryTransactions.Add(transaction);
            await _context.SaveChangesAsync();
        }
    }
}
