namespace MyTasksClient.Features.Tasks.Models;

/// <summary>Fields accepted by POST /api/tasks.</summary>
public sealed record CreateTaskRequest(
    string Title,
    string Description,
    DateTime? DueDate,
    string Category);

/// <summary>Fields accepted by PUT /api/tasks/{id}. The API replaces the whole task.</summary>
public sealed record UpdateTaskRequest(
    string Title,
    string Description,
    DateTime? DueDate,
    string Category,
    TaskItemStatus Status);