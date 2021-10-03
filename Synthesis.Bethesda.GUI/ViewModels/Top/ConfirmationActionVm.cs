using System;
using Noggog.WPF;

namespace Synthesis.Bethesda.GUI.ViewModels.Top
{
    public interface IConfirmationActionVm
    {
        Action? ToDo { get; }
        string Title { get; }
        string Description { get; }
    }

    public class ConfirmationActionVm : ViewModel, IConfirmationActionVm
    {
        public Action? ToDo { get; }
        public string Title { get; }
        public string Description { get; }

        public ConfirmationActionVm(string title, string description, Action? toDo)
        {
            Title = title;
            Description = description;
            ToDo = toDo;
        }
    }
}
