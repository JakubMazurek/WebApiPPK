using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApiPPK.Models;

namespace WebApiPPK.Data;

/// <summary>
/// Kontekst bazy danych dla aplikacji WebApiPPK.
/// Dziedziczy po IdentityDbContext aby obsługiwać Microsoft Identity.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Konfiguracja relacji: ApplicationUser -> Projects (1:N)
        builder.Entity<ApplicationUser>()
            .HasMany(u => u.OwnedProjects)
            .WithOne(p => p.Owner)
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Konfiguracja relacji: ApplicationUser -> Tasks (1:N)
        builder.Entity<ApplicationUser>()
            .HasMany(u => u.AssignedTasks)
            .WithOne(t => t.Assignee)
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull); // Jeśli użytkownik zostanie usunięty, AssigneeId -> null

        // Konfiguracja relacji: Project -> Tasks (1:N)
        builder.Entity<Project>()
            .HasMany(p => p.Tasks)
            .WithOne(t => t.Project)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade); // Usunięcie projektu usuwa wszystkie zadania

        // Indeksy dla poprawy wydajności
        builder.Entity<Project>()
            .HasIndex(p => p.OwnerId);

        builder.Entity<TaskItem>()
            .HasIndex(t => t.ProjectId);

        builder.Entity<TaskItem>()
            .HasIndex(t => t.AssigneeId);
    }
}