using System.ComponentModel.DataAnnotations;

namespace SmartLedger.API.Models
{
    public class Account : BaseEntity
    {
        [Required]
        [StringLength(20)]
        public string AccountCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(20)]
        public string Type { get; set; } = string.Empty; // Asset, Liability, Equity, Income, Expense

        [StringLength(6)]
        public string NormalSide { get; set; } = "DEBIT"; // DEBIT or CREDIT

        public int? ParentAccountId { get; set; }
        public decimal Balance { get; set; } = 0;
    }
}
