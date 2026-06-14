using System.ComponentModel.DataAnnotations;

namespace SmartLedger.API.Models
{
    public class TodoItem : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(20)]
        public string Priority { get; set; } = "MEDIUM"; // HIGH, MEDIUM, LOW

        [StringLength(20)]
        public string Status { get; set; } = "PENDING"; // PENDING, IN_PROGRESS, COMPLETED

        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int? RelatedProductId { get; set; } // Link to product for restock reminders
        public int? RelatedInvoiceId { get; set; } // Link to invoice
    }
}