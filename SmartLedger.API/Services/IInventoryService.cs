using SmartLedger.API.Models;

namespace SmartLedger.API.Services
{
    public interface IInventoryService
    {
        // Product Management
        Task<Product?> GetProductByIdAsync(int id);
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category);
        Task<IEnumerable<Product>> GetLowStockProductsAsync(int threshold = 10);
        Task<Product> CreateProductAsync(Product product);
        Task<Product?> UpdateProductAsync(int id, Product product);
        Task<bool> DeleteProductAsync(int id);

        // Quantity Management
        Task<Product?> IncreaseQuantityAsync(int id, int quantity, string? notes = null, decimal? unitPrice = null);
        Task<Product?> DecreaseQuantityAsync(int id, int quantity, string? notes = null, decimal? unitPrice = null);
        Task<Product?> AdjustQuantityAsync(int id, int newQuantity, string? notes = null);

        // Calculations
        Task<decimal> GetTotalInventoryValueAsync();
        Task<decimal> GetTotalExpectedRevenueAsync();
        Task<decimal> GetTotalPotentialProfitAsync();
        Task<Dictionary<string, decimal>> GetInventoryValueByCategoryAsync();

        // Transactions History
        Task<IEnumerable<InventoryTransaction>> GetTransactionHistoryAsync(int productId, int days = 30);
    }
}