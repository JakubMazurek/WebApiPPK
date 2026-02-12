namespace WebApiPPK.Models;

/// <summary>
/// Projekt należy do dokładnie jednego właściciela (Owner).
/// </summary>
public class Project
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;

    public string? Description { get; set; }

    /// <summary>Znacznik czasu utworzenia projektu (UTC).</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>ID właściciela projektu (IdentityUser).</summary>
    public string OwnerId { get; set; } = default!;

    /// <summary>Nawigacja do właściciela.</summary>
    public ApplicationUser Owner { get; set; } = default!;

    /// <summary>Zadania w projekcie.</summary>
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
