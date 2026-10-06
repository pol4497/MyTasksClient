using System.ComponentModel;
using System.Globalization;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyTasksClient.Features.Tasks.Models;
using MyTasksClient.Features.Tasks.Services;

namespace MyTasksClient.Features.Tasks.ViewModels;

/// <summary>
/// One task card: the text it shows, plus the in-place editing and delete state.
/// The task itself is never changed here. After a successful save or delete the card
/// raises <see cref="Changed"/> and the list reloads from the server.
/// </summary>
public sealed partial class TaskItemViewModel : ObservableObject
{
    private static readonly IReadOnlyList<Option<TaskItemStatus>> Choices =
    [
        new("Pending", TaskItemStatus.Pending),
        new("In progress", TaskItemStatus.InProgress),
        new("Completed", TaskItemStatus.Completed),
        new("Cancelled", TaskItemStatus.Cancelled),
    ];

    private readonly TaskItem _task;
    private readonly ITasksApiClient _tasksApi;

    public TaskItemViewModel(TaskItem task, ITasksApiClient tasksApi)
    {
        _task = task;
        _tasksApi = tasksApi;
        EditText = string.Empty;
    }

    /// <summary>Raised after the task was saved or deleted, so the list should reload.</summary>
    public event EventHandler? Changed;

    /// <summary>Raised when the user starts editing or deleting, so other cards close theirs.</summary>
    public event EventHandler? InteractionStarted;

    // ===== What the card shows =====

    public int Id => _task.Id;

    public string Title => _task.Title;

    public string Description => _task.Description;

    public TaskItemStatus Status => _task.Status;

    public string StatusText => _task.Status switch
    {
        TaskItemStatus.Pending => "Pending",
        TaskItemStatus.InProgress => "In progress",
        TaskItemStatus.Completed => "Completed",
        TaskItemStatus.Cancelled => "Cancelled",
        _ => _task.Status.ToString(),
    };

    public string CategoryText => string.IsNullOrWhiteSpace(_task.Category) ? "Uncategorised" : _task.Category;

    // "d MMM yyyy" reads the same in every culture; month names follow the device language.
    public string DueDateText => _task.DueDate is { } due
        ? due.ToString("d MMM yyyy", CultureInfo.CurrentCulture)
        : "No due date";

    // ===== Editing state =====

    public IReadOnlyList<Option<TaskItemStatus>> StatusChoices => Choices;

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(IsEditing),
        nameof(IsEditingStatus),
        nameof(IsEditingTitle),
        nameof(IsEditingDescription),
        nameof(IsEditingCategory),
        nameof(IsEditingDueDate),
        nameof(ShowSaveButton))]
    public partial EditableField EditingField { get; set; }

    /// <summary>The text being typed, for the title or the category (only one is edited at a time).</summary>
    [ObservableProperty]
    public partial string EditText { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEditDueDate))]
    public partial DateTime? EditDueDate { get; set; }

    [ObservableProperty]
    public partial Option<TaskItemStatus>? EditStatus { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    public bool IsEditing => EditingField != EditableField.None;

    public bool IsEditingStatus => EditingField == EditableField.Status;

    public bool IsEditingTitle => EditingField == EditableField.Title;

    public bool IsEditingDescription => EditingField == EditableField.Description;

    public bool IsEditingCategory => EditingField == EditableField.Category;

    public bool IsEditingDueDate => EditingField == EditableField.DueDate;

    // Changing the status saves immediately, so it has no Save button.
    public bool ShowSaveButton => IsEditing && EditingField != EditableField.Status;

    public bool HasEditDueDate => EditDueDate is not null;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>Closes any open edit or delete confirmation. Called when another card is used.</summary>
    public void CancelInteractions()
    {
        EditingField = EditableField.None;
        IsConfirmingDelete = false;
        ErrorMessage = null;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        // Choosing a different status in the dropdown saves it straight away.
        if (e.PropertyName == nameof(EditStatus)
            && EditingField == EditableField.Status
            && EditStatus is { } choice
            && choice.Value != _task.Status)
        {
            SaveEditCommand.Execute(null);
        }
    }

    // ===== Editing =====

    [RelayCommand]
    private void BeginEdit(EditableField field)
    {
        if (field == EditableField.None || field == EditingField)
        {
            return;
        }

        InteractionStarted?.Invoke(this, EventArgs.Empty);

        ErrorMessage = null;
        IsConfirmingDelete = false;

        // Fill in the value to edit before switching the card into edit mode.
        switch (field)
        {
            case EditableField.Title:
                EditText = _task.Title ?? string.Empty;
                break;
            case EditableField.Description: 
                EditText = _task.Description ?? string.Empty;
                break;
            case EditableField.Category:
                EditText = _task.Category ?? string.Empty;
                break;
            case EditableField.DueDate:
                EditDueDate = _task.DueDate;
                break;
            case EditableField.Status:
                EditStatus = Choices.FirstOrDefault(choice => choice.Value == _task.Status);
                break;
        }

        EditingField = field;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        EditingField = EditableField.None;
        ErrorMessage = null;
    }

    [RelayCommand]
    private void ClearEditDueDate() => EditDueDate = null;

    // Generates SaveEditCommand. The buttons disable themselves while it runs.
    [RelayCommand]
    private async Task SaveEditAsync()
    {
        var (request, error) = BuildRequest();

        if (error is not null)
        {
            ErrorMessage = error;
            return;
        }

        if (request is null)
        {
            return;
        }

        if (request == CurrentRequest())
        {
            CancelEdit(); // nothing changed, so there is nothing to send
            return;
        }

        ErrorMessage = null;

        try
        {
            await _tasksApi.UpdateTaskAsync(Id, request);

            EditingField = EditableField.None;
            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch (TasksApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Deleted elsewhere in the meantime: reload so the card disappears.
            EditingField = EditableField.None;
            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch (TasksApiException ex)
        {
            // Keep the editor open so the user can fix the value or cancel.
            ErrorMessage = ex.Message;
        }
    }

    // The API replaces the whole task, so every update carries all of its fields.
    private UpdateTaskRequest CurrentRequest() => new(
        _task.Title,
        _task.Description ?? string.Empty,
        _task.DueDate?.Date,
        _task.Category ?? string.Empty,
        _task.Status);

    private (UpdateTaskRequest? Request, string? Error) BuildRequest()
    {
        var current = CurrentRequest();

        switch (EditingField)
        {
            case EditableField.Title:
                var title = Clean(EditText);
                return title.Length == 0
                    ? (null, "A task title is required.")
                    : (current with { Title = title }, null);

            case EditableField.Description:
                return (current with { Description = Clean(EditText) }, null);

            case EditableField.Category:
                return (current with { Category = Clean(EditText) }, null);

            case EditableField.DueDate:
                return (current with { DueDate = EditDueDate?.Date }, null);

            case EditableField.Status when EditStatus is { } choice:
                return (current with { Status = choice.Value }, null);

            default:
                return (null, null);
        }
    }

    private static string Clean(string? value) => value?.Trim() ?? string.Empty;

    // ===== Deleting =====

    [RelayCommand]
    private void RequestDelete()
    {
        InteractionStarted?.Invoke(this, EventArgs.Empty);

        EditingField = EditableField.None;
        ErrorMessage = null;
        IsConfirmingDelete = true;
    }

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand]
    private async Task ConfirmDeleteAsync()
    {
        ErrorMessage = null;

        try
        {
            await _tasksApi.DeleteTaskAsync(Id);
            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch (TasksApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Already deleted elsewhere: the end result is what the user wanted.
            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch (TasksApiException ex)
        {
            IsConfirmingDelete = false;
            ErrorMessage = ex.Message;
        }
    }
}