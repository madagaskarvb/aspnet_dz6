using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureTodo.Data;
using SecureTodo.Models;

namespace SecureTodo.Controllers
{
    [ApiController]
    [Route("api/tasks")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class TasksApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public TasksApiController(ApplicationDbContext db) => _db = db;

        private string UserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? string.Empty;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            IQueryable<TaskItem> q = _db.Tasks;

            if (!User.IsInRole("Admin"))
                q = q.Where(t => t.UserId == UserId);

            var list = await q.OrderByDescending(t => t.CreatedAt).ToListAsync();
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var task = await _db.Tasks.FindAsync(id);
            if (task == null) return NotFound();

            if (!User.IsInRole("Admin") && task.UserId != UserId)
                return Forbid();

            return Ok(task);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TaskDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var task = new TaskItem
            {
                Title = dto.Title,
                Description = dto.Description,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow,
                UserId = UserId          // берётся ТОЛЬКО из токена
            };

            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] TaskDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var task = await _db.Tasks.FindAsync(id);
            if (task == null) return NotFound();

            if (!User.IsInRole("Admin") && task.UserId != UserId)
                return Forbid();

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.IsCompleted = dto.IsCompleted;

            await _db.SaveChangesAsync();
            return Ok(task);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _db.Tasks.FindAsync(id);
            if (task == null) return NotFound();

            if (!User.IsInRole("Admin") && task.UserId != UserId)
                return Forbid();

            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}