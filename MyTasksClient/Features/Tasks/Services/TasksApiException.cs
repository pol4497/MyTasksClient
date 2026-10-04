using System.Net;

namespace MyTasksClient.Features.Tasks.Services;

/// <summary>
/// Raised for any failed API call (no connection, timeout, validation error, server error).
/// <see cref="Exception.Message"/> is written for the end user and can be shown directly.
/// </summary>
public sealed class TasksApiException(
    string message,
    HttpStatusCode? statusCode = null,
    Exception? innerException = null) : Exception(message, innerException)
{

    /// <summary>The HTTP status, or null when no response was received.</summary>
    public HttpStatusCode? StatusCode { get; } = statusCode;
}