namespace MyTasksClient.Features.Tasks.Models;

/// <summary>A task as returned by GET /api/tasks.</summary>
public sealed record TaskItem(
    int Id,
    string Title,
    string Description,
    DateTime? DueDate,
    string Category,
    TaskItemStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);