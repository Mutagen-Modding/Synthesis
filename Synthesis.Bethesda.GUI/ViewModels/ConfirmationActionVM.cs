using Noggog.WPF;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    public class ConfirmationActionVM : ViewModel
    {
        public Action ToDo { get; }
        public string Title { get; }
        public string Description { get; }

        public ConfirmationActionVM(string title, string description, Action toDo)
        {
            Title = title;
            Description = description;
            ToDo = toDo;
        }
    }
}
