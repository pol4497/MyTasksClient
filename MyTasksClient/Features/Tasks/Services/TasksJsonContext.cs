using System.Text.Json;
using System.Text.Json.Serialization;
using MyTasksClient.Features.Tasks.Models;

namespace MyTasksClient.Features.Tasks.Services;

/// <summary>
/// Compile-time JSON serialization metadata. "Web" defaults give camelCase property names
/// and case-insensitive reading, which is what ASP.NET Core produces and expects.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(List<TaskItem>))]
[JsonSerializable(typeof(TaskItem))]
[JsonSerializable(typeof(CreateTaskRequest))]
[JsonSerializable(typeof(UpdateTaskRequest))]
[JsonSerializable(typeof(ApiProblemDetails))]
internal partial class TasksJsonContext : JsonSerializerContext
{
}

/// <summary>The part of the API's RFC 7807 "problem details" body that we display.</summary>
internal sealed record ApiProblemDetails(
    string? Title,
    string? Detail,
    Dictionary<string, string[]>? Errors);