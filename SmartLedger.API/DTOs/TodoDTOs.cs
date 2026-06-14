using System.ComponentModel.DataAnnotations;

namespace SmartLedger.API.DTOs
{
    // ============================================================
    // TODO DTOs
    // ============================================================

    /// <summary>
    /// DTO for creating a new todo item
    /// </summary>
    public class CreateTodoDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [RegularExpression("HIGH|MEDIUM|LOW", ErrorMessage = "Priority must be HIGH, MEDIUM, or LOW")]
        public string? Priority { get; set; } = "MEDIUM";

        public DateTime? DueDate { get; set; }

        public int? RelatedProductId { get; set; }
        public int? RelatedInvoiceId { get; set; }
    }

    /// <summary>
    /// DTO for updating a todo item
    /// </summary>
    public class UpdateTodoDto
    {
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string? Title { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [RegularExpression("HIGH|MEDIUM|LOW", ErrorMessage = "Priority must be HIGH, MEDIUM, or LOW")]
        public string? Priority { get; set; }

        public DateTime? DueDate { get; set; }

        [RegularExpression("PENDING|IN_PROGRESS|COMPLETED", ErrorMessage = "Status must be PENDING, IN_PROGRESS, or COMPLETED")]
        public string? Status { get; set; }
    }

    /// <summary>
    /// DTO for todo response
    /// </summary>
    public class TodoResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? RelatedProductId { get; set; }
        public string? RelatedProductName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsOverdue => DueDate.HasValue && DueDate.Value < DateTime.UtcNow && Status != "COMPLETED";
    }
}