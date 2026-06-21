
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
        private readonly ILogger<AccountsController> _logger;

        public AccountsController(ILedgerService ledgerService, ILogger<AccountsController> logger)
        {
            _ledgerService = ledgerService;
            _logger = logger;
        }

        // ============================================================
        // GET: api/accounts
        // Get all accounts
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetAllAccounts()
        {
            try
            {
                var userId = User.GetSmartLedgerUserId();
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized();

                _logger.LogInformation($"Getting all accounts for user: {userId}");
                
                var accounts = await _ledgerService.GetAllAccountsAsync(userId);
                _logger.LogInformation($"Found {accounts.Count()} accounts");
                
                var response = await Task.WhenAll(accounts.Select(account => MapToAccountResponseDtoAsync(account, userId)));
                _logger.LogInformation("Successfully mapped accounts to response DTOs");
                
                return Ok(response.OrderBy(a => a.AccountCode));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all accounts");
                return StatusCode(500, new { message = "Error getting accounts", details = ex.Message });
            }
        }

        // ============================================================
        // GET: api/accounts/{id}
        // Get account by ID
        // ============================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAccountById(int id)
        {
            try
            {
                var userId = User.GetSmartLedgerUserId();
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized();

                var account = await _ledgerService.GetAccountByIdAsync(id, userId);
                if (account == null)
                    return NotFound(new { message = $"Account with ID {id} not found" });

                return Ok(await MapToAccountResponseDtoAsync(account, userId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting account with ID {id}");
                return StatusCode(500, new { message = "Error getting account", details = ex.Message });
            }
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
            try
            {
                if (account.ParentAccountId.HasValue)
                {
                    var parentAccount = await _ledgerService.GetAccountByIdAsync(account.ParentAccountId.Value, userId);
                    parentAccountName = parentAccount?.Name ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error getting parent account for account {account.Id}");
            }

            var balance = 0m;
            try
            {
                balance = await _ledgerService.GetAccountBalanceAsync(account.Id, userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error getting balance for account {account.Id}");
            }

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
