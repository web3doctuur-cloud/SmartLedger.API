using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLedger.API.Data;
using SmartLedger.API.DTOs;
using SmartLedger.API.Extensions;
using SmartLedger.API.Models;
using SmartLedger.API.Services;

namespace SmartLedger.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InventoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryService _inventoryService;

        public InventoryController(ApplicationDbContext context, IInventoryService inventoryService)
        {
            _context = context;
            _inventoryService = inventoryService;
        }

        // ============================================================
        // GET: api/inventory/transactions
        // Get inventory transactions for the current user
        // ============================================================
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] int? productId = null,
            [FromQuery] int days = 30)
        {
            var userId = User.GetSmartLedgerUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            if (productId.HasValue)
            {
                var product = await _inventoryService.GetProductByIdAsync(productId.Value, userId);
                if (product == null)
                    return NotFound(new { message = $"Product with ID {productId.Value} not found" });

                var transactions = await _inventoryService.GetTransactionHistoryAsync(productId.Value, userId, days);
                return Ok(transactions.Select(t => MapToResponseDto(t, product.Name)));
            }

            var response = await _context.InventoryTransactions
                .Include(t => t.Product)
                .Where(t => t.UserId == userId && t.IsActive && t.CreatedAt >= cutoffDate)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new InventoryTransactionResponseDto
                {
                    Id = t.Id,
                    ProductId = t.ProductId,
                    ProductName = t.Product!.Name,
                    TransactionType = t.TransactionType,
                    QuantityChange = t.QuantityChange,
                    PreviousQuantity = t.PreviousQuantity,
                    NewQuantity = t.NewQuantity,
                    Notes = t.Notes,
                    UnitPrice = t.UnitPrice,
                    TotalAmount = t.TotalAmount,
                    ReferenceNumber = t.ReferenceNumber,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return Ok(response);
        }

        // ============================================================
        // POST: api/inventory/transactions
        // Create an inventory transaction
        // ============================================================
        [HttpPost("transactions")]
        public async Task<IActionResult> CreateTransaction([FromBody] CreateInventoryTransactionDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetSmartLedgerUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var product = await _inventoryService.GetProductByIdAsync(model.ProductId, userId);
            if (product == null)
                return NotFound(new { message = $"Product with ID {model.ProductId} not found" });

            try
            {
                Product? updatedProduct = model.TransactionType.ToUpperInvariant() switch
                {
                    "PURCHASE" => await _inventoryService.IncreaseQuantityAsync(
                        model.ProductId,
                        model.Quantity,
                        userId,
                        model.Notes,
                        model.UnitPrice),
                    "SALE" => await _inventoryService.DecreaseQuantityAsync(
                        model.ProductId,
                        model.Quantity,
                        userId,
                        model.Notes,
                        model.UnitPrice),
                    "ADJUSTMENT" => await _inventoryService.AdjustQuantityAsync(
                        model.ProductId,
                        model.NewQuantity ?? throw new InvalidOperationException("New quantity is required for ADJUSTMENT transactions"),
                        userId,
                        model.Notes),
                    _ => throw new InvalidOperationException("Unsupported transaction type")
                };

                if (updatedProduct == null)
                    return NotFound(new { message = $"Product with ID {model.ProductId} not found" });

                return Ok(new
                {
                    message = $"Inventory transaction recorded successfully",
                    product = new
                    {
                        updatedProduct.Id,
                        updatedProduct.Name,
                        updatedProduct.Quantity,
                        updatedProduct.ExpectedRevenue,
                        updatedProduct.TotalCost,
                        updatedProduct.PotentialProfit
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static InventoryTransactionResponseDto MapToResponseDto(InventoryTransaction transaction, string productName)
        {
            return new InventoryTransactionResponseDto
            {
                Id = transaction.Id,
                ProductId = transaction.ProductId,
                ProductName = productName,
                TransactionType = transaction.TransactionType,
                QuantityChange = transaction.QuantityChange,
                PreviousQuantity = transaction.PreviousQuantity,
                NewQuantity = transaction.NewQuantity,
                Notes = transaction.Notes,
                UnitPrice = transaction.UnitPrice,
                TotalAmount = transaction.TotalAmount,
                ReferenceNumber = transaction.ReferenceNumber,
                CreatedAt = transaction.CreatedAt
            };
        }
    }
}
