using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiPPK.Data;
using WebApiPPK.Dtos;
using WebApiPPK.Models;

namespace WebApiPPK.Controllers;

[Authorize]
[ApiController]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProjectsController(AppDbContext db) => _db = db;

    // Pobieramy ID zalogowanego usera z tokena JWT
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Zwraca projekty, do których użytkownik ma dostęp:
    /// - jest właścicielem (Owner)
    /// - LUB ma w projekcie przypisane zadanie
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectReadDto>>> GetAccessibleProjects()
    {
        var projects = await _db.Projects
            .AsNoTracking()
            .Where(p => p.OwnerId == UserId || p.Tasks.Any(t => t.AssigneeId == UserId))
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new ProjectReadDto(p.Id, p.Name, p.Description, p.CreatedAtUtc, p.OwnerId))
            .ToListAsync();

        return Ok(projects);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectReadDto>> GetProject(int id)
    {
        // Sprawdzamy dostęp: owner lub assigned
        var project = await _db.Projects
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.CreatedAtUtc,
                p.OwnerId,
                CanAccess = (p.OwnerId == UserId) || p.Tasks.Any(t => t.AssigneeId == UserId)
            })
            .FirstOrDefaultAsync();

        if (project is null) return NotFound();
        if (!project.CanAccess) return Forbid();

        return Ok(new ProjectReadDto(project.Id, project.Name, project.Description, project.CreatedAtUtc, project.OwnerId));
    }

    [HttpPost]
    public async Task<ActionResult<ProjectReadDto>> CreateProject(ProjectCreateDto dto)
    {
        var project = new Project
        {
            Name = dto.Name,
            Description = dto.Description,
            OwnerId = UserId
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        var read = new ProjectReadDto(project.Id, project.Name, project.Description, project.CreatedAtUtc, project.OwnerId);
        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, read);
    }

    /// <summary>Projekt może edytować tylko właściciel.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProject(int id, ProjectUpdateDto dto)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project is null) return NotFound();
        if (project.OwnerId != UserId) return Forbid();

        project.Name = dto.Name;
        project.Description = dto.Description;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Projekt może usunąć tylko właściciel.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project is null) return NotFound();
        if (project.OwnerId != UserId) return Forbid();

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // REST-owo: zadania jako zasób podrzędny projektu

    [HttpGet("{projectId:int}/tasks")]
    public async Task<ActionResult<IEnumerable<TaskReadDto>>> GetProjectTasks(int projectId)
    {
        var canAccess = await _db.Projects
            .AnyAsync(p => p.Id == projectId && (p.OwnerId == UserId || p.Tasks.Any(t => t.AssigneeId == UserId)));

        if (!canAccess) return Forbid();

        var tasks = await _db.Tasks
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId)
            .Select(t => new TaskReadDto(t.Id, t.Title, t.Description, t.Status, t.ProjectId, t.AssigneeId))
            .ToListAsync();

        return Ok(tasks);
    }

    /// <summary>Dodawanie zadań w projekcie: tylko właściciel.</summary>
    [HttpPost("{projectId:int}/tasks")]
    public async Task<ActionResult<TaskReadDto>> CreateTask(int projectId, TaskCreateDto dto)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
        if (project is null) return NotFound();
        if (project.OwnerId != UserId) return Forbid();

        if (dto.AssigneeId is not null)
        {
            var userExists = await _db.Users.AnyAsync(u => u.Id == dto.AssigneeId);
            if (!userExists) return BadRequest("AssigneeId wskazuje na nieistniejącego użytkownika.");
        }

        var task = new TaskItem
        {
            ProjectId = projectId,
            Title = dto.Title,
            Description = dto.Description,
            AssigneeId = dto.AssigneeId
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        var read = new TaskReadDto(task.Id, task.Title, task.Description, task.Status, task.ProjectId, task.AssigneeId);

        // Uwaga: referencja do akcji w innym kontrolerze
        return CreatedAtAction(
            actionName: nameof(TasksController.GetTaskById),
            controllerName: "Tasks",
            routeValues: new { id = task.Id },
            value: read
        );
    }
}
