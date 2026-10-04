using MyTasksClient.Features.Tasks.ViewModels;

namespace MyTasksClient.Features.Tasks.Views;

public partial class TasksPage : ContentPage
{
    private readonly TasksViewModel _viewModel;

    public TasksPage(TasksViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_viewModel.CheckConnectionCommand.CanExecute(null))
        {
            _viewModel.CheckConnectionCommand.Execute(null);
        }
    }
}