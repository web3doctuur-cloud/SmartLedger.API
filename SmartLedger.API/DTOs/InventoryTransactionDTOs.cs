using System.ComponentModel.DataAnnotations;

namespace SmartLedger.API.DTOs
{
    // ============================================================
    // INVENTORY TRANSACTION DTOs
    // ============================================================

    /// <summary>
    /// DTO for increasing/decreasing quantity
    /// </summary>
    public class QuantityUpdateDto
    {
        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
        public decimal? UnitPrice { get; set; }
    }

    /// <summary>
    /// DTO for adjusting quantity to exact value
    /// </summary>
    public class QuantityAdjustDto
    {
        [Required(ErrorMessage = "New quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative")]
        public int NewQuantity { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO for inventory transaction response
    /// </summary>
    public class InventoryTransactionResponseDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public int QuantityChange { get; set; }
        public int PreviousQuantity { get; set; }
        public int NewQuantity { get; set; }
        public string? Notes { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? ReferenceNumber { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
