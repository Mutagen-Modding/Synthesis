using System;
using System.Reactive;
using Noggog.WPF;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI.ViewModels.Top;

public interface IConfirmationActionVm
{
    ReactiveCommand<Unit, Unit> ConfirmActionCommand { get; }
    ReactiveCommand<Unit, Unit>? DiscardActionCommand { get; }
    string Title { get; }
    string Description { get; }
}

public class ConfirmationActionVm : ViewModel, IConfirmationActionVm
{
    public string Title { get; }
    public string Description { get; }
    public ReactiveCommand<Unit, Unit> ConfirmActionCommand { get; }
    public ReactiveCommand<Unit, Unit>? DiscardActionCommand => null;

    public ConfirmationActionVm(string title, string description, Action? toDo)
    {
        Title = title;
        Description = description;
        ConfirmActionCommand = ReactiveCommand.Create(() => toDo?.Invoke());
    }
}