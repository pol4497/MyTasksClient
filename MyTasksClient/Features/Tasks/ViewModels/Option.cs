namespace MyTasksClient.Features.Tasks.ViewModels;

/// <summary>A choice in a dropdown: the text shown and the value it stands for.</summary>
public sealed record Option<T>(string Label, T Value)
{
    // Pickers display an item's ToString().
    public override string ToString() => Label;
}