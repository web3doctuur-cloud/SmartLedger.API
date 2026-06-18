using System.ComponentModel.DataAnnotations;

namespace SmartLedger.API.DTOs
{
    // ============================================================
    // ACCOUNT DTOs
    // ============================================================

    /// <summary>
    /// DTO for creating a new account
    /// </summary>
    public class CreateAccountDto
    {
        [Required(ErrorMessage = "Account code is required")]
        [StringLength(20, ErrorMessage = "Account code cannot exceed 20 characters")]
        public string AccountCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Account name is required")]
        [StringLength(100, ErrorMessage = "Account name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Account type is required")]
        [RegularExpression("Asset|Liability|Equity|Income|Expense",
            ErrorMessage = "Type must be Asset, Liability, Equity, Income, or Expense")]
        public string Type { get; set; } = string.Empty;

        [Required(ErrorMessage = "Normal side is required")]
        [RegularExpression("DEBIT|CREDIT", ErrorMessage = "Normal side must be DEBIT or CREDIT")]
        public string NormalSide { get; set; } = string.Empty;

        public int? ParentAccountId { get; set; }
    }

    /// <summary>
    /// DTO for updating an account
    /// </summary>
    public class UpdateAccountDto
    {
        [StringLength(100, ErrorMessage = "Account name cannot exceed 100 characters")]
        public string? Name { get; set; }

        [RegularExpression("Asset|Liability|Equity|Income|Expense",
            ErrorMessage = "Type must be Asset, Liability, Equity, Income, or Expense")]
        public string? Type { get; set; }

        [RegularExpression("DEBIT|CREDIT", ErrorMessage = "Normal side must be DEBIT or CREDIT")]
        public string? NormalSide { get; set; }

        public int? ParentAccountId { get; set; }

        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// DTO for account response
    /// </summary>
    public class AccountResponseDto
    {
        public int Id { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string NormalSide { get; set; } = string.Empty;
        public int? ParentAccountId { get; set; }
        public string? ParentAccountName { get; set; }
        public decimal Balance { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
