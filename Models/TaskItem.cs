namespace WebApiPPK.Models;

/// <summary>Status zadania.</summary>
public enum TaskItemStatus
{
    Todo = 0,
    InProgress = 1,
    Done = 2
}

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;
    public int ProjectId { get; set; }
    public Project Project { get; set; } = default!;
    public string? AssigneeId { get; set; }
    public ApplicationUser? Assignee { get; set; }
}
