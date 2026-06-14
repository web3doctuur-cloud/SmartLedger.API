using System.ComponentModel.DataAnnotations;

namespace SmartLedger.API.DTOs
{
    // ============================================================
    // JOURNAL ENTRY DTOs
    // ============================================================

    /// <summary>
    /// DTO for a single journal entry line
    /// </summary>
    public class JournalEntryLineDto
    {
        [Required(ErrorMessage = "Account ID is required")]
        public int AccountId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Debit amount cannot be negative")]
        public decimal Debit { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "Credit amount cannot be negative")]
        public decimal Credit { get; set; } = 0;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? LineDescription { get; set; }

        public string? ReferenceNumber { get; set; }
        public decimal TaxAmount { get; set; } = 0;
    }

    /// <summary>
    /// DTO for creating a journal entry
    /// </summary>
    public class CreateJournalEntryDto
    {
        [Required(ErrorMessage = "Entry date is required")]
        public DateTime EntryDate { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "At least one journal entry line is required")]
        [MinLength(2, ErrorMessage = "A journal entry must have at least 2 lines")]
        public List<JournalEntryLineDto> Lines { get; set; } = new();
    }

    /// <summary>
    /// DTO for journal entry response
    /// </summary>
    public class JournalEntryResponseDto
    {
        public int Id { get; set; }
        public string EntryNumber { get; set; } = string.Empty;
        public DateTime EntryDate { get; set; }
        public string? Description { get; set; }
        public List<JournalEntryLineResponseDto> Lines { get; set; } = new();
        public bool IsApproved { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for journal entry line response
    /// </summary>
    public class JournalEntryLineResponseDto
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string? LineDescription { get; set; }
        public string? ReferenceNumber { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}