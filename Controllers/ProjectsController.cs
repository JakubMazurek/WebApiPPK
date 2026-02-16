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

    // pobieranie ID zalogowanego usera z tokena JWT
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

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
        //sprawdzanie dostępu do projektu w zapytaniu do bazy (czy jestem właścicielem lub przypisanym do zadania)
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

    ///Projekt może edytować tylko właściciel
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

    ///Projekt może usunąć tylko właściciel. Usunięcie projektu powinno również usunąć powiązane z nim zadania (kaskadowo w bazie danych).
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

    //zestawienie zadań w projekcie: dostępne dla właściciela i przypisanych do zadań

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

    ///Dodanie zadania do projektu: tylko właściciel projektu może dodać zadanie. Właściciel może przypisać zadanie do dowolnego użytkownika (w tym siebie) lub pozostawić bez przypisania.
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

        //Odwołanie do endpointu TasksController.GetTaskById, aby klient
        return CreatedAtAction(
            actionName: nameof(TasksController.GetTaskById),
            controllerName: "Tasks",
            routeValues: new { id = task.Id },
            value: read
        );
    }
}
