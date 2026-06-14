using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLedger.API.Models
{
    /// <summary>
    /// Represents a single line in a journal entry (double-entry accounting)
    /// Each journal entry must have at least two lines: one debit and one credit
    /// </summary>
    public class JournalEntryLine : BaseEntity
    {
        // ============================================================
        // Foreign Keys
        // ============================================================

        /// <summary>
        /// ID of the parent journal entry
        /// </summary>
        [Required(ErrorMessage = "Journal Entry ID is required")]
        public int JournalEntryId { get; set; }

        /// <summary>
        /// Navigation property to the parent journal entry
        /// </summary>
        [ForeignKey("JournalEntryId")]
        public virtual JournalEntry? JournalEntry { get; set; }


        /// <summary>
        /// ID of the account being debited or credited
        /// </summary>
        [Required(ErrorMessage = "Account ID is required")]
        public int AccountId { get; set; }

        /// <summary>
        /// Navigation property to the account
        /// </summary>
        [ForeignKey("AccountId")]
        public virtual Account? Account { get; set; }


        // ============================================================
        // Amount Fields (Double-Entry Accounting)
        // ============================================================

        /// <summary>
        /// Debit amount (left side of the accounting equation)
        /// Increases assets and expenses
        /// Decreases liabilities, equity, and revenue
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Debit amount cannot be negative")]
        public decimal Debit { get; set; } = 0;

        /// <summary>
        /// Credit amount (right side of the accounting equation)
        /// Increases liabilities, equity, and revenue
        /// Decreases assets and expenses
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Credit amount cannot be negative")]
        public decimal Credit { get; set; } = 0;


        // ============================================================
        // Additional Fields
        // ============================================================

        /// <summary>
        /// Optional description specific to this line
        /// If not provided, uses the parent journal entry's description
        /// </summary>
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? LineDescription { get; set; }

        /// <summary>
        /// Reference number (invoice number, PO number, etc.)
        /// </summary>
        [StringLength(50, ErrorMessage = "Reference number cannot exceed 50 characters")]
        public string? ReferenceNumber { get; set; }

        /// <summary>
        /// Tax amount for this line (if applicable)
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Tax amount cannot be negative")]
        public decimal TaxAmount { get; set; } = 0;


        // ============================================================
        // Calculated Properties
        // ============================================================

        /// <summary>
        /// Total amount including tax
        /// </summary>
        public decimal TotalAmount => (Debit > 0 ? Debit : Credit) + TaxAmount;

        /// <summary>
        /// Indicates if this is a debit entry
        /// </summary>
        public bool IsDebit => Debit > 0;

        /// <summary>
        /// Indicates if this is a credit entry
        /// </summary>
        public bool IsCredit => Credit > 0;


        // ============================================================
        // Validation Methods
        // ============================================================

        /// <summary>
        /// Validates that the line has either a debit OR a credit, not both
        /// </summary>
        public bool IsValid()
        {
            return (Debit > 0 && Credit == 0) || (Debit == 0 && Credit > 0);
        }

        /// <summary>
        /// Gets the amount (either debit or credit)
        /// </summary>
        public decimal GetAmount()
        {
            return Debit > 0 ? Debit : Credit;
        }

        /// <summary>
        /// Gets the entry type (Debit or Credit)
        /// </summary>
        public string GetEntryType()
        {
            return Debit > 0 ? "DEBIT" : "CREDIT";
        }
    }
}
