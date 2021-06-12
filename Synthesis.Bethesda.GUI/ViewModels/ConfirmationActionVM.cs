using Noggog.WPF;
using System;

namespace Synthesis.Bethesda.GUI
{
    public interface IConfirmationActionVM
    {
        Action? ToDo { get; }
        string Title { get; }
        string Description { get; }
    }

    public class ConfirmationActionVM : ViewModel, IConfirmationActionVM
    {
        public Action? ToDo { get; }
        public string Title { get; }
        public string Description { get; }

        public ConfirmationActionVM(string title, string description, Action? toDo)
        {
            Title = title;
            Description = description;
            ToDo = toDo;
        }
    }
}
