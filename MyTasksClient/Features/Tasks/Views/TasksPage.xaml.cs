using MyTasksClient.Features.Tasks.ViewModels;

namespace MyTasksClient.Features.Tasks.Views;

public partial class TasksPage : ContentPage
{
    public TasksPage(TasksViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}