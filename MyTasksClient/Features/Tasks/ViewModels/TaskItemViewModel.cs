using System.Globalization;
using MyTasksClient.Features.Tasks.Models;

namespace MyTasksClient.Features.Tasks.ViewModels;

/// <summary>
/// Display model for one task card: turns a <see cref="TaskItem"/> into the exact text
/// the card shows. Read-only for now;
/// </summary>
public sealed class TaskItemViewModel(TaskItem task)
{
    public int Id => task.Id;

    public string Title => task.Title;

    public string Description => task.Description;

    public bool HasDescription => !string.IsNullOrWhiteSpace(task.Description);

    public TaskItemStatus Status => task.Status;

    public string StatusText => task.Status switch
    {
        TaskItemStatus.Pending => "Pending",
        TaskItemStatus.InProgress => "In progress",
        TaskItemStatus.Completed => "Completed",
        TaskItemStatus.Cancelled => "Cancelled",
        _ => task.Status.ToString(),
    };

    public string CategoryText => string.IsNullOrWhiteSpace(task.Category) ? "Uncategorised" : task.Category;

    // "d MMM yyyy" reads the same in every culture; month names follow the device language.
    public string DueDateText => task.DueDate is { } dd
        ? dd.ToString("d MMM yyyy", CultureInfo.CurrentCulture)
        : "No due date";
}