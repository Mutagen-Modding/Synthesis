using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    public class ErrorVM : ViewModel
    {
        public string Title { get; }

        [Reactive]
        public string? String { get; set; } = string.Empty;

        [Reactive]
        public Action? BackAction { get; set; }

        public ICommand? BackCommand { get; }

        public ErrorVM(string title, string? str = null, Action? backAction = null)
        {
            Title = title;
            String = str;
            BackAction = backAction;
            BackCommand = NoggogCommand.CreateFromObject(
                objectSource: this.WhenAnyValue(x => x.BackAction),
                canExecute: x => x != null,
                execute: x =>
                {
                    x?.Invoke();
                },
                disposable: this);

            // Go back automatically if things no longer apply
            this.WhenAnyValue(x => x.String)
                .DistinctUntilChanged()
                .Where(x => x.IsNullOrWhitespace())
                .Subscribe(_ =>
                {
                    BackAction?.Invoke();
                })
                .DisposeWith(this);
        }
    }
}
