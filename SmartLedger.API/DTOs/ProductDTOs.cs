using System.ComponentModel.DataAnnotations;

namespace SmartLedger.API.DTOs
{
    // ============================================================
    // PRODUCT DTOs
    // ============================================================

    /// <summary>
    /// DTO for creating a new product
    /// </summary>
    public class CreateProductDto
    {
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(100, ErrorMessage = "Product name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
        public string? Category { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be a positive number")]
        public int Quantity { get; set; } = 0;

        [Range(0.01, double.MaxValue, ErrorMessage = "Cost price must be greater than 0")]
        public decimal CostPrice { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Selling price must be greater than 0")]
        public decimal SellingPrice { get; set; }

        [Range(1, 1000, ErrorMessage = "Low stock threshold must be between 1 and 1000")]
        public int LowStockThreshold { get; set; } = 10;

        public string? ImageUrl { get; set; }
        public string? SKU { get; set; }
    }

    /// <summary>
    /// DTO for updating an existing product
    /// </summary>
    public class UpdateProductDto
    {
        [StringLength(100, ErrorMessage = "Product name cannot exceed 100 characters")]
        public string? Name { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
        public string? Category { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Cost price must be a positive number")]
        public decimal? CostPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Selling price must be a positive number")]
        public decimal? SellingPrice { get; set; }

        [Range(1, 1000, ErrorMessage = "Low stock threshold must be between 1 and 1000")]
        public int? LowStockThreshold { get; set; }

        public string? ImageUrl { get; set; }
        public string? SKU { get; set; }
        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// DTO for product response (what API returns)
    /// </summary>
    public class ProductResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public int Quantity { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal ExpectedRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal PotentialProfit { get; set; }
        public decimal ProfitMargin { get; set; }
        public bool IsLowStock { get; set; }
        public int LowStockThreshold { get; set; }
        public string? ImageUrl { get; set; }
        public string? SKU { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// DTO for product summary (list view)
    /// </summary>
    public class ProductSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public int Quantity { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal ExpectedRevenue { get; set; }
        public bool IsLowStock { get; set; }
        public string? ImageUrl { get; set; }
    }
}