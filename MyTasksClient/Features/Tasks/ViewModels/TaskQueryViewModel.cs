using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyTasksClient.Features.Tasks.Models;

namespace MyTasksClient.Features.Tasks.ViewModels;

/// <summary>
/// Everything the user chose for the list: search text, filters, sorting, paging, and
/// whether the filter panel is open. Raises <see cref="QueryChanged"/> when the list
/// should be reloaded.
/// </summary>
public partial class TaskQueryViewModel : ObservableObject
{
    private static readonly TimeSpan TypingDelay = TimeSpan.FromMilliseconds(300);
    private CancellationTokenSource? _typingCts;
    private bool _suppressChanges;

    public TaskQueryViewModel()
    {
        // Setting the starting values must not look like the user changing a filter.
        _suppressChanges = true;
        SearchText = string.Empty;
        Category = string.Empty;
        SelectedStatus = StatusOptions[0];
        SelectedSortField = SortOptions[0];
        SelectedDirection = DirectionOptions[0];
        SelectedPageSize = PageSizeOptions[0];
        _suppressChanges = false;
    }

    /// <summary>Raised when the list should be reloaded with <see cref="ToQuery"/>.</summary>
    public event EventHandler? QueryChanged;

    // ===== Dropdown choices =====

    public IReadOnlyList<Option<TaskItemStatus?>> StatusOptions { get; } =
    [
        new("All statuses", null),
        new("Pending", TaskItemStatus.Pending),
        new("In progress", TaskItemStatus.InProgress),
        new("Completed", TaskItemStatus.Completed),
        new("Cancelled", TaskItemStatus.Cancelled),
    ];

    public IReadOnlyList<Option<TaskSortField>> SortOptions { get; } =
    [
        new("Due date", TaskSortField.DueDate),
        new("Title", TaskSortField.Title),
        new("Status", TaskSortField.Status),
    ];

    public IReadOnlyList<Option<bool>> DirectionOptions { get; } =
    [
        new("Ascending", false),
        new("Descending", true),
    ];

    public IReadOnlyList<Option<int?>> PageSizeOptions { get; } =
    [
        new("All", null),
        new("5", 5),
        new("10", 10),
        new("25", 25),
        new("50", 50),
    ];

    // ===== What the user chose =====

    [ObservableProperty]
    public partial string SearchText { get; set; }

    [ObservableProperty]
    public partial string Category { get; set; }

    [ObservableProperty]
    public partial Option<TaskItemStatus?>? SelectedStatus { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDueAfter))]
    public partial DateTime? DueAfter { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDueBefore))]
    public partial DateTime? DueBefore { get; set; }

    [ObservableProperty]
    public partial Option<TaskSortField>? SelectedSortField { get; set; }

    [ObservableProperty]
    public partial Option<bool>? SelectedDirection { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageText))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand), nameof(PreviousPageCommand))]
    public partial Option<int?>? SelectedPageSize { get; set; }

    // ===== Paging state =====

    /// <summary>Number of tasks skipped, so a multiple of the page size.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageText))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    public partial int Offset { get; set; }

    /// <summary>Set by the list after each load: did the server return more than one page?</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    public partial bool HasNextPage { get; set; }

    [ObservableProperty]
    public partial bool IsPanelOpen { get; set; }

    // ===== Derived values =====

    public int? PageSize => SelectedPageSize?.Value;

    public string PageText => $"Page {(PageSize is { } size ? (Offset / size) + 1 : 1)}";

    public bool HasDueAfter => DueAfter is not null;

    public bool HasDueBefore => DueBefore is not null;

    /// <summary>True when search or filters narrow the list (sorting and paging don't count).</summary>
    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchText)
        || SelectedStatus?.Value is not null
        || !string.IsNullOrWhiteSpace(Category)
        || DueAfter is not null
        || DueBefore is not null;

    /// <summary>Builds the API query. Asks for one row more than a page to detect a next page.</summary>
    public TaskQuery ToQuery() => new()
    {
        Search = SearchText,
        Status = SelectedStatus?.Value,
        Category = Category,
        DueAfter = DueAfter,
        DueBefore = DueBefore,
        SortBy = SelectedSortField?.Value,
        Desc = SelectedDirection?.Value ?? false,
        Limit = PageSize is { } size ? size + 1 : null,
        Offset = Offset > 0 ? Offset : null,
    };

    /// <summary>Steps back one page, for example when the current page no longer exists.</summary>
    public void GoToPreviousPage()
    {
        Offset = Math.Max(0, Offset - (PageSize ?? 0));
        RaiseQueryChangedNow();
    }

    // ===== Reacting to the user's choices =====

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        switch (e.PropertyName)
        {
            case nameof(SearchText):
            case nameof(Category):
                FilterChanged(debounce: true);
                break;

            case nameof(SelectedStatus):
            case nameof(DueAfter):
            case nameof(DueBefore):
            case nameof(SelectedSortField):
            case nameof(SelectedDirection):
            case nameof(SelectedPageSize):
                FilterChanged(debounce: false);
                break;
        }
    }

    private void FilterChanged(bool debounce)
    {
        if (_suppressChanges)
        {
            return;
        }

        // A different filter means a different result set, so start again from page 1.
        Offset = 0;
        HasNextPage = false;

        if (debounce)
        {
            ScheduleQueryChanged();
        }
        else
        {
            RaiseQueryChangedNow();
        }
    }

    private void RaiseQueryChangedNow()
    {
        CancelPendingTyping();
        QueryChanged?.Invoke(this, EventArgs.Empty);
    }

    // Wait for a pause in typing so we don't query on every keystroke.
    private void ScheduleQueryChanged()
    {
        CancelPendingTyping();

        var cts = _typingCts = new CancellationTokenSource();
        _ = RaiseAfterTypingDelayAsync(cts.Token);
    }

    private async Task RaiseAfterTypingDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TypingDelay, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return; // The user typed again, or a change was applied immediately.
        }

        if (!cancellationToken.IsCancellationRequested)
        {
            QueryChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void CancelPendingTyping()
    {
        _typingCts?.Cancel();
        _typingCts?.Dispose();
        _typingCts = null;
    }

    // ===== Commands =====

    [RelayCommand]
    private void TogglePanel() => IsPanelOpen = !IsPanelOpen;

    [RelayCommand]
    private void ClosePanel() => IsPanelOpen = false;

    // The search icon or Enter key: apply the search now instead of waiting for the delay.
    [RelayCommand]
    private void SearchNow() => RaiseQueryChangedNow();

    [RelayCommand]
    private void ClearDueAfter() => DueAfter = null;

    [RelayCommand]
    private void ClearDueBefore() => DueBefore = null;

    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private void NextPage()
    {
        if (PageSize is not { } size)
        {
            return;
        }

        Offset += size;
        HasNextPage = false; // disabled until the new page loads, so a double tap can't skip a page
        RaiseQueryChangedNow();
    }

    private bool CanGoToNextPage() => PageSize is not null && HasNextPage;

    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private void PreviousPage() => GoToPreviousPage();

    private bool CanGoToPreviousPage() => Offset > 0;

    // Back to the defaults, applied as a single reload.
    [RelayCommand]
    private void Reset()
    {
        _suppressChanges = true;
        SearchText = string.Empty;
        Category = string.Empty;
        SelectedStatus = StatusOptions[0];
        DueAfter = null;
        DueBefore = null;
        SelectedSortField = SortOptions[0];
        SelectedDirection = DirectionOptions[0];
        SelectedPageSize = PageSizeOptions[0];
        Offset = 0;
        HasNextPage = false;
        _suppressChanges = false;

        RaiseQueryChangedNow();
    }
}