
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartLedger.API.DTOs;
using SmartLedger.API.Extensions;
using SmartLedger.API.Models;
using SmartLedger.API.Services;

namespace SmartLedger.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountsController : ControllerBase
    {
        private readonly ILedgerService _ledgerService;

        public AccountsController(ILedgerService ledgerService)
        {
            _ledgerService = ledgerService;
        }

        // ============================================================
        // GET: api/accounts
        // Get all accounts
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetAllAccounts()
        {
            var userId = User.GetSmartLedgerUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var accounts = await _ledgerService.GetAllAccountsAsync(userId);
            var response = await Task.WhenAll(accounts.Select(account => MapToAccountResponseDtoAsync(account, userId)));
            return Ok(response.OrderBy(a => a.AccountCode));
        }

        // ============================================================
        // GET: api/accounts/{id}
        // Get account by ID
        // ============================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAccountById(int id)
        {
            var userId = User.GetSmartLedgerUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var account = await _ledgerService.GetAccountByIdAsync(id, userId);
            if (account == null)
                return NotFound(new { message = $"Account with ID {id} not found" });

            return Ok(await MapToAccountResponseDtoAsync(account, userId));
        }

        // ============================================================
        // POST: api/accounts
        // Create a new account
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = User.GetSmartLedgerUserId();
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized();

                var account = new Account
                {
                    AccountCode = model.AccountCode,
                    Name = model.Name,
                    Type = model.Type,
                    NormalSide = model.NormalSide,
                    ParentAccountId = model.ParentAccountId
                };

                var result = await _ledgerService.CreateAccountAsync(account, userId);
                return CreatedAtAction(nameof(GetAccountById), new { id = result.Id }, await MapToAccountResponseDtoAsync(result, userId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ============================================================
        // PUT: api/accounts/{id}
        // Update an account
        // ============================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAccount(int id, [FromBody] UpdateAccountDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetSmartLedgerUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var existing = await _ledgerService.GetAccountByIdAsync(id, userId);
            if (existing == null)
                return NotFound(new { message = $"Account with ID {id} not found" });

            if (!string.IsNullOrWhiteSpace(model.Name))
                existing.Name = model.Name;
            if (!string.IsNullOrWhiteSpace(model.Type))
                existing.Type = model.Type;
            if (!string.IsNullOrWhiteSpace(model.NormalSide))
                existing.NormalSide = model.NormalSide;
            if (model.ParentAccountId.HasValue)
                existing.ParentAccountId = model.ParentAccountId;
            if (model.IsActive.HasValue)
                existing.IsActive = model.IsActive.Value;

            var result = await _ledgerService.UpdateAccountAsync(id, existing, userId);
            if (result == null)
                return NotFound(new { message = $"Account with ID {id} not found" });

            return Ok(await MapToAccountResponseDtoAsync(result, userId));
        }

        // ============================================================
        // DELETE: api/accounts/{id}
        // Delete an account
        // ============================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            var userId = User.GetSmartLedgerUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            try
            {
                var result = await _ledgerService.DeleteAccountAsync(id, userId);
                if (!result)
                    return NotFound(new { message = $"Account with ID {id} not found" });
                return Ok(new { message = "Account deleted successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private async Task<AccountResponseDto> MapToAccountResponseDtoAsync(Account account, string userId)
        {
            var parentAccountName = string.Empty;
            if (account.ParentAccountId.HasValue)
            {
                var parentAccount = await _ledgerService.GetAccountByIdAsync(account.ParentAccountId.Value, userId);
                parentAccountName = parentAccount?.Name ?? string.Empty;
            }

            var balance = await _ledgerService.GetAccountBalanceAsync(account.Id, userId);

            return new AccountResponseDto
            {
                Id = account.Id,
                AccountCode = account.AccountCode,
                Name = account.Name,
                Type = account.Type,
                NormalSide = account.NormalSide,
                ParentAccountId = account.ParentAccountId,
                ParentAccountName = parentAccountName,
                Balance = balance,
                CreatedAt = account.CreatedAt,
                IsActive = account.IsActive
            };
        }
    }
}
