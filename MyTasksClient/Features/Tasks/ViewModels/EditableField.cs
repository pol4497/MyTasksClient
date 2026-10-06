namespace MyTasksClient.Features.Tasks.ViewModels;

/// <summary>The part of a task card that is currently being edited in place.</summary>
public enum EditableField
{
    None,
    Status,
    Title,
    Description,
    Category,
    DueDate,
}