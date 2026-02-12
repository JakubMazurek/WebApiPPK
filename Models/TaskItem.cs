namespace WebApiPPK.Models;

/// <summary>Status zadania.</summary>
public enum TaskItemStatus
{
    Todo = 0,
    InProgress = 1,
    Done = 2
}

/// <summary>
/// Zadanie należy do projektu i może mieć przypisanego wykonawcę (Assignee).
/// </summary>
public class TaskItem
{
    public int Id { get; set; }

    public string Title { get; set; } = default!;

    public string? Description { get; set; }

    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;

    /// <summary>FK do projektu.</summary>
    public int ProjectId { get; set; }

    public Project Project { get; set; } = default!;

    /// <summary>FK do wykonawcy (może być null).</summary>
    public string? AssigneeId { get; set; }

    public ApplicationUser? Assignee { get; set; }
}
