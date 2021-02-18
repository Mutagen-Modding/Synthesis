using Noggog.WPF;
using ReactiveUI.Fody.Helpers;

namespace Synthesis.Bethesda.GUI
{
    public class ListElementWrapperVM<TItem, TWrapper> : ViewModel, IBasicSettingsNodeVM
        where TWrapper : IBasicSettingsNodeVM
    {
        public TWrapper Value { get; }

        object IBasicSettingsNodeVM.Value => this.Value;

        [Reactive]
        public bool IsSelected { get; set; }

        public ListElementWrapperVM(TWrapper value)
        {
            Value = value;
        }
    }
}
