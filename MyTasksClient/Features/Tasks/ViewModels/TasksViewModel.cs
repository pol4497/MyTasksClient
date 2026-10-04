using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyTasksClient.Features.Tasks.Models;
using MyTasksClient.Features.Tasks.Services;

namespace MyTasksClient.Features.Tasks.ViewModels;

/// <summary>
/// State and behaviour for the tasks screen. Empty for now; later steps add the task
/// list, loading/error state, and commands.
/// </summary>
public partial class TasksViewModel(ITasksApiClient tasksApi) : ObservableObject
{
    // The toolkit generates the property body and change notification.
    [ObservableProperty]
    public partial string ConnectionStatus { get; set; } = "Connecting to the API…";

    // The toolkit generates CheckConnectionCommand (the "Async" suffix is dropped).
    [RelayCommand]
    private async Task CheckConnectionAsync()
    {
        try
        {
            var tasks = await tasksApi.GetTasksAsync(new TaskQuery());
            ConnectionStatus = $"Connected. {tasks.Count} task(s) returned.";
        }
        catch (TasksApiException ex)
        {
            ConnectionStatus = ex.Message;
        }
    }
}