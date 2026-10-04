namespace MyTasksClient.Features.Tasks.Models;

public enum TaskSortField
{
    DueDate,
    Title,
    Status,
}

/// <summary>Optional filters, sorting and paging for GET /api/tasks.</summary>
public sealed record TaskQuery
{
    public TaskItemStatus? Status { get; init; }
    public string? Category { get; init; }
    public DateTime? DueBefore { get; init; }
    public DateTime? DueAfter { get; init; }
    public string? Search { get; init; }
    public TaskSortField? SortBy { get; init; }
    public bool Desc { get; init; }
    public int? Limit { get; init; }
    public int? Offset { get; init; }
}