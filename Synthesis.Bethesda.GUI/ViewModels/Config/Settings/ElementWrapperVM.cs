using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Text.Json;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    public class ListElementWrapperVM<TItem, TWrapper> : ViewModel, IBasicSettingsNodeVM
        where TWrapper : BasicSettingsNodeVM<TItem>, new()
    {
        public TWrapper Value { get; }

        public ICommand DeleteCommand { get; }

        object IBasicSettingsNodeVM.Value => this.Value;

        public ListElementWrapperVM(TItem value, Action<ListElementWrapperVM<TItem, TWrapper>> deleteAction)
        {
            DeleteCommand = ReactiveCommand.Create(() => deleteAction(this));
            Value = new TWrapper();
            Value.Value = value;
        }
    }
}
