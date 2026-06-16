using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartLedger.API.DTOs;
using SmartLedger.API.Models;
using SmartLedger.API.Services;

namespace SmartLedger.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class JournalEntriesController : ControllerBase
    {
        private readonly ILedgerService _ledgerService;

        public JournalEntriesController(ILedgerService ledgerService)
        {
            _ledgerService = ledgerService;
        }

        // ============================================================
        // GET: api/journalentries
        // Get all journal entries
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetAllEntries()
        {
            var entries = await _ledgerService.GetAllJournalEntriesAsync();
            return Ok(entries);
        }

        // ============================================================
        // GET: api/journalentries/{id}
        // Get journal entry by ID
        // ============================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEntryById(int id)
        {
            var entry = await _ledgerService.GetJournalEntryByIdAsync(id);
            if (entry == null)
                return NotFound(new { message = $"Journal entry with ID {id} not found" });
            return Ok(entry);
        }

        // ============================================================
        // POST: api/journalentries
        // Create a new journal entry
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> CreateEntry([FromBody] CreateJournalEntryDto entryDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Validate double-entry
                var totalDebits = entryDto.Lines.Sum(l => l.Debit);
                var totalCredits = entryDto.Lines.Sum(l => l.Credit);

                if (totalDebits != totalCredits)
                {
                    return BadRequest(new
                    {
                        message = "Total debits must equal total credits in double-entry accounting",
                        totalDebits = totalDebits,
                        totalCredits = totalCredits
                    });
                }

                // Convert DTO to entities
                var entry = new JournalEntry
                {
                    EntryDate = entryDto.EntryDate,
                    Description = entryDto.Description,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? ""
                };

                var lines = entryDto.Lines.Select(line => new JournalEntryLine
                {
                    AccountId = line.AccountId,
                    Debit = line.Debit,
                    Credit = line.Credit,
                    LineDescription = line.LineDescription,
                    ReferenceNumber = line.ReferenceNumber,
                    TaxAmount = line.TaxAmount,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                }).ToList();

                var createdEntry = await _ledgerService.CreateJournalEntryAsync(entry, lines);
                var response = await _ledgerService.GetJournalEntryByIdAsync(createdEntry.Id);
                return CreatedAtAction(nameof(GetEntryById), new { id = response.Id }, response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ============================================================
        // POST: api/journalentries/{id}/approve
        // Approve a journal entry
        // ============================================================
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveEntry(int id)
        {
            var result = await _ledgerService.ApproveJournalEntryAsync(id);
            if (!result)
                return NotFound(new { message = $"Journal entry with ID {id} not found" });
            return Ok(new { message = "Journal entry approved successfully" });
        }

        // ============================================================
        // DELETE: api/journalentries/{id}
        // Delete a journal entry
        // ============================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEntry(int id)
        {
            var result = await _ledgerService.DeleteJournalEntryAsync(id);
            if (!result)
                return NotFound(new { message = $"Journal entry with ID {id} not found" });
            return Ok(new { message = "Journal entry deleted successfully" });
        }
    }
}