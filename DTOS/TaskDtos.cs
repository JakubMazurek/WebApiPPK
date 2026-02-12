using WebApiPPK.Models;

namespace WebApiPPK.Dtos;

public record TaskCreateDto(string Title, string? Description, string? AssigneeId);
public record TaskUpdateDto(string Title, string? Description, TaskItemStatus Status, string? AssigneeId);

public record TaskReadDto(
    int Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    int ProjectId,
    string? AssigneeId
);

