using System.Text.Json.Serialization;

namespace MyTasksClient.Features.Tasks.Models;

[JsonConverter(typeof(JsonStringEnumConverter<TaskItemStatus>))]
public enum TaskItemStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3,
}