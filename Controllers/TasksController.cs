using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiPPK.Data;
using WebApiPPK.Dtos;

namespace WebApiPPK.Controllers;

[Authorize]
[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _db;
    public TasksController(AppDbContext db) => _db = db;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Odczyt zadania: właściciel projektu lub przypisany wykonawca.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskReadDto>> GetTaskById(int id)
    {
        var task = await _db.Tasks
            .AsNoTracking()
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task is null) return NotFound();

        var canAccess = task.Project.OwnerId == UserId || task.AssigneeId == UserId;
        if (!canAccess) return Forbid();

        return Ok(new TaskReadDto(task.Id, task.Title, task.Description, task.Status, task.ProjectId, task.AssigneeId));
    }

    /// <summary>
    /// Aktualizacja: właściciel projektu lub assignee.
    /// Assignee (nie-owner) może zmienić tylko status (przykładowa polityka).
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateTask(int id, TaskUpdateDto dto)
    {
        var task = await _db.Tasks
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task is null) return NotFound();

        var isOwner = task.Project.OwnerId == UserId;
        var isAssignee = task.AssigneeId == UserId;

        if (!isOwner && !isAssignee) return Forbid();

        if (isOwner)
        {
            // Owner może edytować wszystko
            if (dto.AssigneeId is not null)
            {
                var userExists = await _db.Users.AnyAsync(u => u.Id == dto.AssigneeId);
                if (!userExists) return BadRequest("AssigneeId wskazuje na nieistniejącego użytkownika.");
            }

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.Status = dto.Status;
            task.AssigneeId = dto.AssigneeId;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // Assignee – ograniczamy do zmiany statusu
        task.Status = dto.Status;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Usuwanie: tylko właściciel projektu.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var task = await _db.Tasks
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task is null) return NotFound();
        if (task.Project.OwnerId != UserId) return Forbid();

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
