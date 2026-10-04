namespace MyTasksClient.Features.Tasks.ViewModels;

/// <summary>What the task list is currently showing. The first value is the starting state.</summary>
public enum LoadState
{
    Loading,
    Error,
    Empty,
    Content,
}