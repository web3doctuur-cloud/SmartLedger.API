using System.ComponentModel.DataAnnotations;

namespace SmartLedger.API.Models
{
    public class Product : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int Quantity { get; set; } = 0;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal CostPrice { get; set; }  // What trader paid

        [Required]
        [Range(0, double.MaxValue)]
        public decimal SellingPrice { get; set; }  // What customer pays

        // Calculated properties (not stored in database)
        public decimal ExpectedRevenue => Quantity * SellingPrice;
        public decimal TotalCost => Quantity * CostPrice;
        public decimal PotentialProfit => ExpectedRevenue - TotalCost;
        public decimal ProfitMargin => SellingPrice > 0
            ? ((SellingPrice - CostPrice) / SellingPrice) * 100
            : 0;

        public int LowStockThreshold { get; set; } = 10;
        public bool IsLowStock => Quantity <= LowStockThreshold;

        // Optional: Product image
        public string? ImageUrl { get; set; }

        // Barcode/SKU
        [StringLength(50)]
        public string? SKU { get; set; }
    }
}