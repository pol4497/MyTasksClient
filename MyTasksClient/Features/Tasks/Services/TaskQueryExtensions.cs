using System.Globalization;
using MyTasksClient.Features.Tasks.Models;

namespace MyTasksClient.Features.Tasks.Services;

internal static class TaskQueryExtensions
{
    public static string ToQueryString(this TaskQuery query)
    {
        var parts = new List<string>();

        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                parts.Add($"{key}={Uri.EscapeDataString(value)}");
            }
        }

        Add("status", query.Status?.ToString());
        Add("category", query.Category?.Trim());
        Add("dueBefore", FormatDate(query.DueBefore));
        Add("dueAfter", FormatDate(query.DueAfter));
        Add("search", query.Search?.Trim());
        Add("sortBy", query.SortBy?.ToString());

        if (query.Desc)
        {
            Add("desc", "true");
        }

        Add("limit", query.Limit?.ToString(CultureInfo.InvariantCulture));
        Add("offset", query.Offset?.ToString(CultureInfo.InvariantCulture));

        return parts.Count == 0 ? string.Empty : "?" + string.Join('&', parts);
    }

    private static string? FormatDate(DateTime? value) =>
        value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}