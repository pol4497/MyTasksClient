using System.ComponentModel;
using MyTasksClient.Features.Tasks.ViewModels;

namespace MyTasksClient.Features.Tasks.Views;

public partial class TaskCard : ContentView
{
    private TaskItemViewModel? _viewModel;

    public TaskCard()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = BindingContext as TaskItemViewModel;

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    // When a text field switches to its editor, put the cursor in it so the user can type.
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TaskItemViewModel.EditingField) || _viewModel is null)
        {
            return;
        }

        VisualElement? editor = _viewModel.EditingField switch
        {
            EditableField.Title => TitleEntry,
            EditableField.Description => DescriptionEntry,
            EditableField.Category => CategoryEntry,
            _ => null,
        };

        if (editor is not null)
        {
            // Wait until the editor is actually visible, otherwise Focus does nothing.
            Dispatcher.Dispatch(() => editor.Focus());
        }
    }
}