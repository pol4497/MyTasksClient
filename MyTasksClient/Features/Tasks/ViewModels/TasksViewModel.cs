using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyTasksClient.Features.Tasks.Models;
using MyTasksClient.Features.Tasks.Services;

namespace MyTasksClient.Features.Tasks.ViewModels;

public partial class TasksViewModel : ObservableObject
{
    private readonly ITasksApiClient _tasksApi;

    public TasksViewModel(ITasksApiClient tasksApi)
    {
        _tasksApi = tasksApi;
        Tasks.CollectionChanged += (_, _) => OnPropertyChanged(nameof(TotalText));
    }

    public ObservableCollection<TaskItemViewModel> Tasks { get; } = [];

    // Starts as LoadState.Loading, the first enum value.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoading), nameof(HasError), nameof(IsEmpty), nameof(HasTasks))]
    public partial LoadState State { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public bool IsLoading => State == LoadState.Loading;

    public bool HasError => State == LoadState.Error;

    public bool IsEmpty => State == LoadState.Empty;

    public bool HasTasks => State == LoadState.Content;

    public string TotalText => $"{Tasks.Count} total";

    // Generates LoadTasksCommand. The CancellationToken is supplied by the command
    // The command can't run twice at once.
    [RelayCommand]
    private async Task LoadTasksAsync(CancellationToken cancellationToken)
    {
        // Keep a list that is already on screen while it refreshes; otherwise show "Loading".
        if (State != LoadState.Content)
        {
            State = LoadState.Loading;
        }

        try
        {
            var tasks = await _tasksApi.GetTasksAsync(new TaskQuery(), cancellationToken);

            Tasks.Clear();
            foreach (var task in tasks)
            {
                Tasks.Add(new TaskItemViewModel(task));
            }

            ErrorMessage = null;
            State = Tasks.Count == 0 ? LoadState.Empty : LoadState.Content;
        }
        catch (TasksApiException ex)
        {
            Tasks.Clear();
            ErrorMessage = $"Could not load tasks: {ex.Message}";
            State = LoadState.Error;
        }
    }
}