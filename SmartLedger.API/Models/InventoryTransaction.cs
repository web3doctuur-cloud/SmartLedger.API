using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLedger.API.Models
{
    public class InventoryTransaction : BaseEntity
    {
        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [Required]
        [StringLength(20)]
        public string TransactionType { get; set; } = string.Empty; // PURCHASE, SALE, ADJUSTMENT

        public int QuantityChange { get; set; } // Positive = add, Negative = remove
        public int PreviousQuantity { get; set; }
        public int NewQuantity { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public decimal? UnitPrice { get; set; } // Price at time of transaction
        public decimal? TotalAmount => UnitPrice.HasValue ? UnitPrice.Value * Math.Abs(QuantityChange) : null;

        public string? ReferenceNumber { get; set; } // Invoice number, PO number
        public string UserId { get; set; } = string.Empty;
    }
}
