
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            var accounts = await _ledgerService.GetAllAccountsAsync();
            return Ok(accounts);
        }

        // ============================================================
        // GET: api/accounts/{id}
        // Get account by ID
        // ============================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAccountById(int id)
        {
            var account = await _ledgerService.GetAccountByIdAsync(id);
            if (account == null)
                return NotFound(new { message = $"Account with ID {id} not found" });
            return Ok(account);
        }

        // ============================================================
        // POST: api/accounts
        // Create a new account
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] Account account)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _ledgerService.CreateAccountAsync(account);
                return CreatedAtAction(nameof(GetAccountById), new { id = result.Id }, result);
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
        public async Task<IActionResult> UpdateAccount(int id, [FromBody] Account account)
        {
            if (id != account.Id)
                return BadRequest(new { message = "ID mismatch" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _ledgerService.UpdateAccountAsync(id, account);
            if (result == null)
                return NotFound(new { message = $"Account with ID {id} not found" });

            return Ok(result);
        }

        // ============================================================
        // DELETE: api/accounts/{id}
        // Delete an account
        // ============================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            try
            {
                var result = await _ledgerService.DeleteAccountAsync(id);
                if (!result)
                    return NotFound(new { message = $"Account with ID {id} not found" });
                return Ok(new { message = "Account deleted successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}