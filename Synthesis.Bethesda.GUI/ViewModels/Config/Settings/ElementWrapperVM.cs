using Noggog.WPF;
using ReactiveUI;
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

        private readonly ObservableAsPropertyHelper<string> _DisplayName;
        public string DisplayName => _DisplayName.Value;

        public ListElementWrapperVM(TWrapper value)
        {
            Value = value;
            _DisplayName = value.WhenAnyValue(x => x.DisplayName)
                .ToGuiProperty(this, nameof(DisplayName), string.Empty, deferSubscription: true);
        }

        public void WrapUp() => Value.WrapUp();
    }
}
