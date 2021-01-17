using Noggog.WPF;
using ReactiveUI;
using System;

namespace Synthesis.Bethesda.GUI
{
    public class ListElementWrapperVM<TItem, TWrapper> : ViewModel, IBasicSettingsNodeVM
        where TWrapper : BasicSettingsNodeVM<TItem>, new()
    {
        public TWrapper Value { get; }

        object IBasicSettingsNodeVM.Value => this.Value;

        public ListElementWrapperVM(TItem value)
        {
            Value = new TWrapper();
            Value.Value = value;
        }
    }
}
