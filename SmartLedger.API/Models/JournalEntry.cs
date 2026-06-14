using System.ComponentModel.DataAnnotations;

namespace SmartLedger.API.Models
{
    public class JournalEntry : BaseEntity
    {
        [Required]
        [StringLength(20)]
        public string EntryNumber { get; set; } = string.Empty;

        [Required]
        public DateTime EntryDate { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public bool IsApproved { get; set; } = false;
        public DateTime? ApprovedAt { get; set; }

        public virtual ICollection <JournalEntryLine>? Lines { get; set; }
    }
}
