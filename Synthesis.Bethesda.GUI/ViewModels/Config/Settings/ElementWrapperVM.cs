using Noggog.WPF;
using ReactiveUI.Fody.Helpers;

namespace Synthesis.Bethesda.GUI
{
    public class ListElementWrapperVM<TItem, TWrapper> : ViewModel, IBasicSettingsNodeVM
        where TWrapper : BasicSettingsVM<TItem>, new()
    {
        public TWrapper Value { get; }

        object IBasicSettingsNodeVM.Value => this.Value;

        [Reactive]
        public bool IsSelected { get; set; }

        public ListElementWrapperVM(TItem value)
        {
            Value = new TWrapper();
            Value.Value = value;
        }
    }
}
