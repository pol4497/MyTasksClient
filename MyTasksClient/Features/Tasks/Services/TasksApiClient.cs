using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;
using MyTasksClient.Features.Tasks.Models;

namespace MyTasksClient.Features.Tasks.Services;

internal sealed class TasksApiClient(HttpClient httpClient, ILogger<TasksApiClient> logger) : ITasksApiClient
{
    // Relative (no leading slash) so it combines with the BaseAddress, which ends in "/".
    private const string TasksPath = "api/tasks";

    public async Task<IReadOnlyList<TaskItem>> GetTasksAsync(
        TaskQuery query, CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, TasksPath + query.ToQueryString());
        using var response = await SendAsync(message, cancellationToken);

        return await ReadJsonAsync(response, TasksJsonContext.Default.ListTaskItem, cancellationToken);
    }

    public async Task<TaskItem> CreateTaskAsync(
        CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var body = request with { DueDate = ToUnspecifiedDate(request.DueDate) };

        using var message = new HttpRequestMessage(HttpMethod.Post, TasksPath)
        {
            Content = JsonContent.Create(body, TasksJsonContext.Default.CreateTaskRequest),
        };
        using var response = await SendAsync(message, cancellationToken);

        return await ReadJsonAsync(response, TasksJsonContext.Default.TaskItem, cancellationToken);
    }

    public async Task UpdateTaskAsync(
        int id, UpdateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var body = request with { DueDate = ToUnspecifiedDate(request.DueDate) };

        using var message = new HttpRequestMessage(HttpMethod.Put, $"{TasksPath}/{id}")
        {
            Content = JsonContent.Create(body, TasksJsonContext.Default.UpdateTaskRequest),
        };
        using var response = await SendAsync(message, cancellationToken); // 204 No Content on success
    }

    public async Task DeleteTaskAsync(int id, CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Delete, $"{TasksPath}/{id}");
        using var response = await SendAsync(message, cancellationToken); // 204 No Content on success
    }

    /// <summary>
    /// Sends the request and returns a successful response, or throws <see cref="TasksApiException"/>.
    /// Cancellation requested by the caller is NOT wrapped; it propagates as OperationCanceledException.
    /// </summary>
    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;

        try
        {
            response = await httpClient.SendAsync(message, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "{Method} {Uri} could not reach the server", message.Method, message.RequestUri);
            throw new TasksApiException("Can't reach the server. Check your connection and try again.", innerException: ex);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient.Timeout surfaces as a cancellation the caller did not request.
            logger.LogWarning(ex, "{Method} {Uri} timed out", message.Method, message.RequestUri);
            throw new TasksApiException("The server took too long to respond. Please try again.", innerException: ex);
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        try
        {
            logger.LogWarning("{Method} {Uri} failed with {StatusCode}", message.Method, message.RequestUri, (int)response.StatusCode);
            var errorMessage = await GetErrorMessageAsync(response, cancellationToken);
            throw new TasksApiException(errorMessage, response.StatusCode);
        }
        finally
        {
            response.Dispose();
        }
    }

    private static async Task<T> ReadJsonAsync<T>(
        HttpResponseMessage response, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken)
    {
        try
        {
            var value = await response.Content.ReadFromJsonAsync(typeInfo, cancellationToken);
            return value ?? throw new TasksApiException("The server returned an empty response.", response.StatusCode);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            throw new TasksApiException("The server returned data the app could not understand.", response.StatusCode, ex);
        }
    }

    private static async Task<string> GetErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        // Only 400s carry messages meant for users (e.g. "DueDate cannot be in the past.").
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problemMessage = await TryReadProblemMessageAsync(response, cancellationToken);
            if (problemMessage is not null)
            {
                return problemMessage;
            }
        }

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => "That task could not be found. It may have been deleted.",
            _ when (int)response.StatusCode >= 500 => "The server ran into a problem. Please try again later.",
            _ => "The request failed. Please try again.",
        };
    }

    private static async Task<string?> TryReadProblemMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync(TasksJsonContext.Default.ApiProblemDetails, cancellationToken);
            if (problem is null)
            {
                return null;
            }

            // Specific validation messages first. The generic title ("One or more validation
            // errors occurred.") is only a last resort.
            var validation = problem.Errors is null
                ? null
                : string.Join(' ', problem.Errors.Values.SelectMany(messages => messages));

            return FirstNonEmpty(validation, problem.Detail, problem.Title);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return null; // Not a problem-details body.
        }
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    /// <summary>
    /// The API stores a calendar date. Send midnight with an *unspecified* kind so no UTC offset
    /// is attached; an offset would make the server shift the date to its own time zone and
    /// could save the wrong day.
    /// </summary>
    private static DateTime? ToUnspecifiedDate(DateTime? value) =>
        value is { } date ? DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified) : null;
}