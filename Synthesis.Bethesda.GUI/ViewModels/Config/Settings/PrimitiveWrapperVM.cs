using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    public class PrimitiveWrapperVM<T> : ViewModel
        where T : struct
    {
        [Reactive]
        public T Value { get; set; }

        public ICommand DeleteCommand { get; }

        public PrimitiveWrapperVM(Action<PrimitiveWrapperVM<T>> deleteAction)
        {
            DeleteCommand = ReactiveCommand.Create(() => deleteAction(this));
        }
    }
}
