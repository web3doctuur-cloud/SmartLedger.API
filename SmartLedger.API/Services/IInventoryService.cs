using SmartLedger.API.Models;

namespace SmartLedger.API.Services
{
    public interface IInventoryService
    {
        // Product Management
        Task<Product?> GetProductByIdAsync(int id, string userId);
        Task<IEnumerable<Product>> GetAllProductsAsync(string userId);
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category, string userId);
        Task<IEnumerable<Product>> GetLowStockProductsAsync(string userId, int threshold = 10);
        Task<Product> CreateProductAsync(Product product, string userId);
        Task<Product?> UpdateProductAsync(int id, Product product, string userId);
        Task<bool> DeleteProductAsync(int id, string userId);

        // Quantity Management
        Task<Product?> IncreaseQuantityAsync(int id, int quantity, string userId, string? notes = null, decimal? unitPrice = null);
        Task<Product?> DecreaseQuantityAsync(int id, int quantity, string userId, string? notes = null, decimal? unitPrice = null);
        Task<Product?> AdjustQuantityAsync(int id, int newQuantity, string userId, string? notes = null);

        // Calculations
        Task<decimal> GetTotalInventoryValueAsync(string userId);
        Task<decimal> GetTotalExpectedRevenueAsync(string userId);
        Task<decimal> GetTotalPotentialProfitAsync(string userId);
        Task<Dictionary<string, decimal>> GetInventoryValueByCategoryAsync(string userId);

        // Transactions History
        Task<IEnumerable<InventoryTransaction>> GetTransactionHistoryAsync(int productId, string userId, int days = 30);
    }
}
