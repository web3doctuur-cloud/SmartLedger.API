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
    public class ProductsController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IInventoryService inventoryService, ILogger<ProductsController> logger)
        {
            _inventoryService = inventoryService;
            _logger = logger;
        }

        // ============================================================
        // GET: api/products
        // Get all products
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            var userId = User.GetSmartLedgerUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var products = await _inventoryService.GetAllProductsAsync(userId);
            var response = products.Select(p => MapToProductResponseDto(p));
            return Ok(response);
        }

        // ============================================================
        // GET: api/products/{id}
        // Get product by ID
        // ============================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var userId = User.GetSmartLedgerUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var product = await _inventoryService.GetProductByIdAsync(id, userId);
            if (product == null)
                return NotFound(new { message = $"Product with ID {id} not found" });

            return Ok(MapToProductResponseDto(product));
        }

        // ============================================================
        // GET: api/products/category/{category}
        // Get products by category
        // ============================================================
        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetProductsByCategory(string category)
        {
            var userId = User.GetSmartLedgerUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var products = await _inventoryService.GetProductsByCategoryAsync(category, userId);
            var response = products.Select(p => MapToProductSummaryDto(p));
            return Ok(response);
        }

        // ============================================================
        // GET: api/products/low-stock
        // Get products with low stock
        // ============================================================
        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStockProducts([FromQuery] int threshold = 10)
        {
            var userId = User.GetSmartLedgerUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var products = await _inventoryService.GetLowStockProductsAsync(userId, threshold);
            var response = products.Select(p => new
            {
                p.Id,
                p.Name,
                p.Quantity,
                p.LowStockThreshold,
                p.SellingPrice,
                ExpectedRevenue = p.ExpectedRevenue
            });
            return Ok(response);
        }

        // ============================================================
        // GET: api/products/statistics/summary
        // Get inventory statistics
        // ============================================================
        [HttpGet("statistics/summary")]
        public async Task<IActionResult> GetInventoryStatistics()
        {
            var userId = User.GetSmartLedgerUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var totalValue = await _inventoryService.GetTotalInventoryValueAsync(userId);
            var expectedRevenue = await _inventoryService.GetTotalExpectedRevenueAsync(userId);
            var potentialProfit = await _inventoryService.GetTotalPotentialProfitAsync(userId);
            var byCategory = await _inventoryService.GetInventoryValueByCategoryAsync(userId);

            return Ok(new
            {
                totalInventoryValue = totalValue,
                totalExpectedRevenue = expectedRevenue,
                totalPotentialProfit = potentialProfit,
                profitMargin = expectedRevenue > 0 ? (potentialProfit / expectedRevenue) * 100 : 0,
                valueByCategory = byCategory
            });
        }

        // ============================================================
        // POST: api/products
        // Create a new product
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto productDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = User.GetSmartLedgerUserId();
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized();

                var product = new Product
                {
                    Name = productDto.Name,
                    Description = productDto.Description,
                    Category = productDto.Category,
                    Quantity = productDto.Quantity,
                    CostPrice = productDto.CostPrice,
                    SellingPrice = productDto.SellingPrice,
                    LowStockThreshold = productDto.LowStockThreshold,
                    ImageUrl = productDto.ImageUrl,
                    SKU = productDto.SKU
                };

                var createdProduct = await _inventoryService.CreateProductAsync(product, userId);
                return CreatedAtAction(nameof(GetProductById), new { id = createdProduct.Id }, MapToProductResponseDto(createdProduct));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, new { message = "An error occurred while creating the product" });
            }
        }

        // ============================================================
        // PUT: api/products/{id}
        // Update a product
        // ============================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto productDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetSmartLedgerUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var existingProduct = await _inventoryService.GetProductByIdAsync(id, userId);
            if (existingProduct == null)
                return NotFound(new { message = $"Product with ID {id} not found" });

            // Update only provided fields
            if (!string.IsNullOrEmpty(productDto.Name))
                existingProduct.Name = productDto.Name;
            if (productDto.Description != null)
                existingProduct.Description = productDto.Description;
            if (!string.IsNullOrEmpty(productDto.Category))
                existingProduct.Category = productDto.Category;
            if (productDto.CostPrice.HasValue)
                existingProduct.CostPrice = productDto.CostPrice.Value;
            if (productDto.SellingPrice.HasValue)
                existingProduct.SellingPrice = productDto.SellingPrice.Value;
            if (productDto.LowStockThreshold.HasValue)
                existingProduct.LowStockThreshold = productDto.LowStockThreshold.Value;
            if (productDto.ImageUrl != null)
                existingProduct.ImageUrl = productDto.ImageUrl;
            if (productDto.SKU != null)
                existingProduct.SKU = productDto.SKU;
            if (productDto.IsActive.HasValue)
                existingProduct.IsActive = productDto.IsActive.Value;

            var updatedProduct = await _inventoryService.UpdateProductAsync(id, existingProduct, userId);
            return Ok(MapToProductResponseDto(updatedProduct!));
        }

        // ============================================================
        // PATCH: api/products/{id}/increase-quantity
        // Increase product quantity (add stock)
        // ============================================================
        [HttpPatch("{id}/increase-quantity")]
        public async Task<IActionResult> IncreaseQuantity(int id, [FromBody] QuantityUpdateDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetSmartLedgerUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var product = await _inventoryService.IncreaseQuantityAsync(id, model.Quantity, userId, model.Notes, model.UnitPrice);
            if (product == null)
                return NotFound(new { message = $"Product with ID {id} not found" });

            return Ok(new
            {
                message = $"Added {model.Quantity} units",
                product = new
                {
                    product.Id,
                    product.Name,
                    product.Quantity,
                    product.ExpectedRevenue,
                    product.TotalCost,
                    product.PotentialProfit
                }
            });
        }

        // ============================================================
        // PATCH: api/products/{id}/decrease-quantity
        // Decrease product quantity (sell stock)
        // ============================================================
        [HttpPatch("{id}/decrease-quantity")]
        public async Task<IActionResult> DecreaseQuantity(int id, [FromBody] QuantityUpdateDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = User.GetSmartLedgerUserId();
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized();

                var product = await _inventoryService.DecreaseQuantityAsync(id, model.Quantity, userId, model.Notes, model.UnitPrice);
                if (product == null)
                    return NotFound(new { message = $"Product with ID {id} not found" });

                return Ok(new
                {
                    message = $"Sold {model.Quantity} units",
                    product = new
                    {
                        product.Id,
                        product.Name,
                        product.Quantity,
                        product.ExpectedRevenue,
                        product.TotalCost,
                        product.PotentialProfit
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ============================================================
        // PATCH: api/products/{id}/adjust-quantity
        // Adjust product quantity to exact value
        // ============================================================
        [HttpPatch("{id}/adjust-quantity")]
        public async Task<IActionResult> AdjustQuantity(int id, [FromBody] QuantityAdjustDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetSmartLedgerUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var product = await _inventoryService.AdjustQuantityAsync(id, model.NewQuantity, userId, model.Notes);
            if (product == null)
                return NotFound(new { message = $"Product with ID {id} not found" });

            return Ok(new
            {
                message = $"Quantity adjusted to {model.NewQuantity} units",
                product = new
                {
                    product.Id,
                    product.Name,
                    product.Quantity,
                    product.ExpectedRevenue,
                    product.TotalCost,
                    product.PotentialProfit
                }
            });
        }

        // ============================================================
        // GET: api/products/{id}/transactions
        // Get transaction history for a product
        // ============================================================
        [HttpGet("{id}/transactions")]
        public async Task<IActionResult> GetTransactionHistory(int id, [FromQuery] int days = 30)
        {
            var userId = User.GetSmartLedgerUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var product = await _inventoryService.GetProductByIdAsync(id, userId);
            if (product == null)
                return NotFound(new { message = $"Product with ID {id} not found" });

            var transactions = await _inventoryService.GetTransactionHistoryAsync(id, userId, days);
            var response = transactions.Select(t => new InventoryTransactionResponseDto
            {
                Id = t.Id,
                ProductId = t.ProductId,
                ProductName = product.Name,
                TransactionType = t.TransactionType,
                QuantityChange = t.QuantityChange,
                PreviousQuantity = t.PreviousQuantity,
                NewQuantity = t.NewQuantity,
                Notes = t.Notes,
                UnitPrice = t.UnitPrice,
                TotalAmount = t.TotalAmount,
                ReferenceNumber = t.ReferenceNumber,
                CreatedAt = t.CreatedAt
            });

            return Ok(response);
        }

        // ============================================================
        // DELETE: api/products/{id}
        // Delete a product (soft delete)
        // ============================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var userId = User.GetSmartLedgerUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var deleted = await _inventoryService.DeleteProductAsync(id, userId);
            if (!deleted)
                return NotFound(new { message = $"Product with ID {id} not found" });

            return Ok(new { message = "Product deleted successfully" });
        }

        // ============================================================
        // PRIVATE HELPER METHODS
        // ============================================================

        private ProductResponseDto MapToProductResponseDto(Product product)
        {
            return new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Category = product.Category,
                Quantity = product.Quantity,
                CostPrice = product.CostPrice,
                SellingPrice = product.SellingPrice,
                ExpectedRevenue = product.ExpectedRevenue,
                TotalCost = product.TotalCost,
                PotentialProfit = product.PotentialProfit,
                ProfitMargin = product.ProfitMargin,
                IsLowStock = product.IsLowStock,
                LowStockThreshold = product.LowStockThreshold,
                ImageUrl = product.ImageUrl,
                SKU = product.SKU,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                IsActive = product.IsActive
            };
        }

        private ProductSummaryDto MapToProductSummaryDto(Product product)
        {
            return new ProductSummaryDto
            {
                Id = product.Id,
                Name = product.Name,
                Category = product.Category,
                Quantity = product.Quantity,
                SellingPrice = product.SellingPrice,
                ExpectedRevenue = product.ExpectedRevenue,
                IsLowStock = product.IsLowStock,
                ImageUrl = product.ImageUrl
            };
        }
    }
}
