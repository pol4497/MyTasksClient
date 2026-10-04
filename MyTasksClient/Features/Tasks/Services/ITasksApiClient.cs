using MyTasksClient.Features.Tasks.Models;

namespace MyTasksClient.Features.Tasks.Services;

/// <summary>Gateway to the MyTasks API. Every method throws <see cref="TasksApiException"/> on failure.</summary>
public interface ITasksApiClient
{
    Task<IReadOnlyList<TaskItem>> GetTasksAsync(TaskQuery query, CancellationToken cancellationToken = default);

    Task<TaskItem> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken = default);

    Task UpdateTaskAsync(int id, UpdateTaskRequest request, CancellationToken cancellationToken = default);

    Task DeleteTaskAsync(int id, CancellationToken cancellationToken = default);
}