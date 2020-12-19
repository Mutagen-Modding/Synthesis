using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    public class ErrorVM : ViewModel
    {
        public string Title { get; }
        public string String { get; }
        public ICommand? BackCommand { get; }

        public ErrorVM(string title, string str, Action? backAction = null)
        {
            Title = title;
            String = str;
            BackCommand = backAction == null ? null : ReactiveCommand.Create(backAction);
        }
    }
}
