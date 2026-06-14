using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLedger.API.Data;
using SmartLedger.API.DTOs;
using SmartLedger.API.Models;
using System.Security.Claims;

namespace SmartLedger.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TodoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TodoController> _logger;

        public TodoController(ApplicationDbContext context, ILogger<TodoController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ============================================================
        // GET: api/todo
        // Get all todo items for current user
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetMyTodos()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var todos = await _context.TodoItems
                .Where(t => t.UserId == userId && t.IsActive)
                .OrderByDescending(t => t.Priority == "HIGH")
                .ThenBy(t => t.DueDate)
                .ToListAsync();

            var response = todos.Select(t => MapToTodoResponseDto(t));
            return Ok(response);
        }

        // ============================================================
        // GET: api/todo/pending
        // Get pending todo items
        // ============================================================
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingTodos()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var todos = await _context.TodoItems
                .Where(t => t.UserId == userId && t.Status != "COMPLETED" && t.IsActive)
                .OrderByDescending(t => t.Priority == "HIGH")
                .ThenBy(t => t.DueDate)
                .ToListAsync();

            var response = todos.Select(t => MapToTodoResponseDto(t));
            return Ok(response);
        }

        // ============================================================
        // GET: api/todo/{id}
        // Get todo by ID
        // ============================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTodoById(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var todo = await _context.TodoItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (todo == null)
                return NotFound(new { message = "Todo item not found" });

            return Ok(MapToTodoResponseDto(todo));
        }

        // ============================================================
        // POST: api/todo
        // Create a new todo item
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> CreateTodo([FromBody] CreateTodoDto model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var todo = new TodoItem
            {
                Title = model.Title,
                Description = model.Description,
                Priority = model.Priority ?? "MEDIUM",
                Status = "PENDING",
                DueDate = model.DueDate,
                UserId = userId,
                RelatedProductId = model.RelatedProductId,
                RelatedInvoiceId = model.RelatedInvoiceId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.TodoItems.Add(todo);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Todo created: {todo.Title} for user {userId}");
            return CreatedAtAction(nameof(GetTodoById), new { id = todo.Id }, MapToTodoResponseDto(todo));
        }

        // ============================================================
        // PUT: api/todo/{id}
        // Update a todo item
        // ============================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTodo(int id, [FromBody] UpdateTodoDto model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var todo = await _context.TodoItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (todo == null)
                return NotFound(new { message = "Todo item not found" });

            if (!string.IsNullOrEmpty(model.Title))
                todo.Title = model.Title;
            if (model.Description != null)
                todo.Description = model.Description;
            if (!string.IsNullOrEmpty(model.Priority))
                todo.Priority = model.Priority;
            if (model.DueDate.HasValue)
                todo.DueDate = model.DueDate;
            if (!string.IsNullOrEmpty(model.Status))
                todo.Status = model.Status;

            todo.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(MapToTodoResponseDto(todo));
        }

        // ============================================================
        // PATCH: api/todo/{id}/complete
        // Mark todo as completed
        // ============================================================
        [HttpPatch("{id}/complete")]
        public async Task<IActionResult> CompleteTodo(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var todo = await _context.TodoItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (todo == null)
                return NotFound(new { message = "Todo item not found" });

            todo.Status = "COMPLETED";
            todo.CompletedAt = DateTime.UtcNow;
            todo.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Todo completed", todo = MapToTodoResponseDto(todo) });
        }

        // ============================================================
        // PATCH: api/todo/{id}/in-progress
        // Mark todo as in progress
        // ============================================================
        [HttpPatch("{id}/in-progress")]
        public async Task<IActionResult> MarkInProgress(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var todo = await _context.TodoItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (todo == null)
                return NotFound(new { message = "Todo item not found" });

            todo.Status = "IN_PROGRESS";
            todo.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(MapToTodoResponseDto(todo));
        }

        // ============================================================
        // DELETE: api/todo/{id}
        // Delete a todo item (soft delete)
        // ============================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodo(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var todo = await _context.TodoItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (todo == null)
                return NotFound(new { message = "Todo item not found" });

            todo.IsActive = false;
            todo.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Todo deleted successfully" });
        }

        // ============================================================
        // PRIVATE HELPER METHODS
        // ============================================================

        private TodoResponseDto MapToTodoResponseDto(TodoItem todo)
        {
            string? relatedProductName = null;
            if (todo.RelatedProductId.HasValue)
            {
                var product = _context.Products.Find(todo.RelatedProductId.Value);
                relatedProductName = product?.Name;
            }

            return new TodoResponseDto
            {
                Id = todo.Id,
                Title = todo.Title,
                Description = todo.Description,
                Priority = todo.Priority,
                Status = todo.Status,
                DueDate = todo.DueDate,
                CompletedAt = todo.CompletedAt,
                RelatedProductId = todo.RelatedProductId,
                RelatedProductName = relatedProductName,
                CreatedAt = todo.CreatedAt
            };
        }
    }
}