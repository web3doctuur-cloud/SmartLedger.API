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
    public class JournalEntriesController : ControllerBase
    {
        private readonly ILedgerService _ledgerService;
        private readonly ILogger<JournalEntriesController> _logger;

        public JournalEntriesController(ILedgerService ledgerService, ILogger<JournalEntriesController> logger)
        {
            _ledgerService = ledgerService;
            _logger = logger;
        }

        // ============================================================
        // GET: api/journalentries
        // Get all journal entries
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetAllEntries()
        {
            try
            {
                var userId = User.GetSmartLedgerUserId();
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized();

                _logger.LogInformation($"Getting all journal entries for user: {userId}");
                var entries = await _ledgerService.GetAllJournalEntriesAsync(userId);
                _logger.LogInformation($"Found {entries.Count()} journal entries");
                return Ok(entries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all journal entries");
                return StatusCode(500, new { message = "Error getting journal entries", details = ex.Message });
            }
        }

        // ============================================================
        // GET: api/journalentries/{id}
        // Get journal entry by ID
        // ============================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEntryById(int id)
        {
            try
            {
                var userId = User.GetSmartLedgerUserId();
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized();

                var entry = await _ledgerService.GetJournalEntryByIdAsync(id, userId);
                if (entry == null)
                    return NotFound(new { message = $"Journal entry with ID {id} not found" });
                return Ok(entry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting journal entry with ID {id}");
                return StatusCode(500, new { message = "Error getting journal entry", details = ex.Message });
            }
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
                var userId = User.GetSmartLedgerUserId();
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized();

                _logger.LogInformation($"Creating journal entry for user: {userId}, Description: {entryDto.Description}");

                // Convert DTO to entities
                var entry = new JournalEntry
                {
                    EntryDate = entryDto.EntryDate,
                    Description = entryDto.Description,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    UserId = userId
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

                _logger.LogInformation($"Creating journal entry with {lines.Count} lines");
                var createdEntry = await _ledgerService.CreateJournalEntryAsync(entry, lines, userId);
                
                _logger.LogInformation($"Journal entry created with ID: {createdEntry.Id}");
                
                var response = await _ledgerService.GetJournalEntryByIdAsync(createdEntry.Id, userId);
                if (response == null)
                    return StatusCode(500, new { message = "Journal entry was created but could not be reloaded" });

                return CreatedAtAction(nameof(GetEntryById), new { id = response.Id }, response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation creating journal entry");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating journal entry");
                return StatusCode(500, new { message = "Error creating journal entry", details = ex.Message });
            }
        }

        // ============================================================
        // POST: api/journalentries/{id}/approve
        // Approve a journal entry
        // ============================================================
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveEntry(int id)
        {
            var userId = User.GetSmartLedgerUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _ledgerService.ApproveJournalEntryAsync(id, userId);
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
            var userId = User.GetSmartLedgerUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _ledgerService.DeleteJournalEntryAsync(id, userId);
            if (!result)
                return NotFound(new { message = $"Journal entry with ID {id} not found" });
            return Ok(new { message = "Journal entry deleted successfully" });
        }
    }
}
