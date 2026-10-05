using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyTasksClient.Features.Tasks.Services;

namespace MyTasksClient.Features.Tasks.ViewModels;

public partial class TasksViewModel : ObservableObject
{
    private readonly ITasksApiClient _tasksApi;
    private CancellationTokenSource? _loadCts;

    public TasksViewModel(ITasksApiClient tasksApi, AddTaskViewModel addTask, TaskQueryViewModel query)
    {
        _tasksApi = tasksApi;
        AddTask = addTask;
        Query = query;

        Tasks.CollectionChanged += (_, _) => OnPropertyChanged(nameof(TotalText));
        AddTask.TaskAdded += (_, _) => LoadTasksCommand.Execute(null);
        Query.QueryChanged += (_, _) => LoadTasksCommand.Execute(null);
    }

    /// <summary>The "Add a task" form.</summary>
    public AddTaskViewModel AddTask { get; }

    /// <summary>Search, filter, sort and paging choices.</summary>
    public TaskQueryViewModel Query { get; }

    public ObservableCollection<TaskItemViewModel> Tasks { get; } = [];

    // Starts as LoadState.Loading, the first enum value.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoading), nameof(HasError), nameof(IsEmpty), nameof(HasTasks), nameof(EmptyMessage))]
    public partial LoadState State { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public bool IsLoading => State == LoadState.Loading;

    public bool HasError => State == LoadState.Error;

    public bool IsEmpty => State == LoadState.Empty;

    public bool HasTasks => State == LoadState.Content;

    public string TotalText => $"{Tasks.Count} total";

    public string EmptyMessage => Query.HasActiveFilters
        ? "No tasks match your search or filters."
        : "No tasks yet. Add one above to get started.";

    // Generates LoadTasksCommand. Concurrent runs are allowed on purpose: starting a new
    // load cancels the one in flight, so the newest request always wins.
    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task LoadTasksAsync()
    {
        _loadCts?.Cancel();

        using var cts = new CancellationTokenSource();
        _loadCts = cts;

        // Keep a list that is already on screen while it refreshes; otherwise show "Loading".
        if (State != LoadState.Content)
        {
            State = LoadState.Loading;
        }

        // Read once so the page size used to cut the result matches the one in the request.
        var pageSize = Query.PageSize;
        var apiQuery = Query.ToQuery();

        try
        {
            var tasks = await _tasksApi.GetTasksAsync(apiQuery, cts.Token);

            // A newer load replaced this one while it was running: drop the outdated result.
            if (cts.IsCancellationRequested)
            {
                return;
            }

            // The page we asked for has no tasks (for example they were deleted): step back.
            if (tasks.Count == 0 && Query.Offset > 0)
            {
                Query.GoToPreviousPage(); // raises QueryChanged, which starts a new load
                return;
            }

            // We asked for one row more than a page; an extra row means a next page exists.
            Query.HasNextPage = pageSize is { } size && tasks.Count > size;
            var visible = pageSize is { } limit ? tasks.Take(limit) : tasks;

            Tasks.Clear();
            foreach (var task in visible)
            {
                Tasks.Add(new TaskItemViewModel(task));
            }

            ErrorMessage = null;
            State = Tasks.Count == 0 ? LoadState.Empty : LoadState.Content;
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // Replaced by a newer load, which now owns the screen.
        }
        catch (TasksApiException ex)
        {
            if (cts.IsCancellationRequested)
            {
                return;
            }

            Tasks.Clear();
            Query.HasNextPage = false;
            ErrorMessage = $"Could not load tasks: {ex.Message}";
            State = LoadState.Error;
        }
        finally
        {
            // Never leave a reference to a source that is about to be disposed.
            if (ReferenceEquals(_loadCts, cts))
            {
                _loadCts = null;
            }
        }
    }
}