using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyTasksClient.Features.Tasks.Models;
using MyTasksClient.Features.Tasks.Services;

namespace MyTasksClient.Features.Tasks.ViewModels;

/// <summary>State and behaviour of the "Add a task" form.</summary>
public partial class AddTaskViewModel(ITasksApiClient tasksApi) : ObservableObject
{

    /// <summary>Raised after the API accepted a new task, so the list can reload.</summary>
    public event EventHandler? TaskAdded;

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Category { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDueDate))]
    public partial DateTime? DueDate { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SubmitButtonText))]
    public partial bool IsSubmitting { get; set; }

    public bool HasDueDate => DueDate is not null;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public string SubmitButtonText => IsSubmitting ? "Adding task…" : "Add task";

    /// <summary>The earliest selectable due date. The API rejects dates in the past.</summary>
    public DateTime MinimumDueDate => DateTime.Today;

    [RelayCommand]
    private void ClearDueDate() => DueDate = null;

    // Generates SubmitCommand. It can't run twice at once, and the button disables itself
    // while it runs.
    [RelayCommand]
    private async Task SubmitAsync()
    {
        var title = Clean(Title);
        if (title.Length == 0)
        {
            ErrorMessage = "A task title is required.";
            return;
        }

        ErrorMessage = null;
        IsSubmitting = true;

        try
        {
            var request = new CreateTaskRequest(title, Clean(Description), DueDate, Clean(Category));
            await tasksApi.CreateTaskAsync(request);

            Reset();
            TaskAdded?.Invoke(this, EventArgs.Empty);
        }
        catch (TasksApiException ex)
        {
            // Keep what the user typed so they can fix it or try again.
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private void Reset()
    {
        Title = string.Empty;
        Category = string.Empty;
        Description = string.Empty;
        DueDate = null;
        ErrorMessage = null;
    }

    private static string Clean(string? value) => value?.Trim() ?? string.Empty;
}