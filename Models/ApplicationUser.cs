using Microsoft.AspNetCore.Identity;

namespace WebApiPPK.Models;

/// <summary>
/// Użytkownik aplikacji oparty o Microsoft Identity.
/// Rozszerzamy IdentityUser o relacje na projekty i zadania.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Projekty, których użytkownik jest właścicielem.</summary>
    public ICollection<Project> OwnedProjects { get; set; } = new List<Project>();

    /// <summary>Zadania przypisane do użytkownika (wykonawca).</summary>
    public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
}
