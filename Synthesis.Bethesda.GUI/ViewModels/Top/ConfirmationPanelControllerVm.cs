using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Synthesis.Bethesda.GUI.ViewModels.Top
{
    public interface IConfirmationPanelControllerVm
    {
        IConfirmationActionVm? TargetConfirmation { get; set; }
        ReactiveCommand<Unit, Unit> ConfirmActionCommand { get; }
        ReactiveCommand<Unit, Unit> DiscardActionCommand { get; }
    }

    public class ConfirmationPanelControllerVm : ViewModel, IConfirmationPanelControllerVm
    {
        [Reactive]
        public IConfirmationActionVm? TargetConfirmation { get; set; }
        
        public ReactiveCommand<Unit, Unit> ConfirmActionCommand { get; }
        
        public ReactiveCommand<Unit, Unit> DiscardActionCommand { get; }

        public ConfirmationPanelControllerVm()
        {
            DiscardActionCommand = NoggogCommand.CreateFromObjectz(
                objectSource: this.WhenAnyValue(x => x.TargetConfirmation),
                canExecute: target =>
                {
                    if (target == null) return Observable.Return(false);
                    return target.DiscardActionCommand.CanExecute;
                },
                execute: x =>
                {
                    (x?.DiscardActionCommand as ICommand)?.Execute(Unit.Default);
                    TargetConfirmation = null;
                },
                disposable: this);
            ConfirmActionCommand = NoggogCommand.CreateFromObjectz(
                objectSource: this.WhenAnyValue(x => x.TargetConfirmation),
                canExecute: target =>
                {
                    if (target == null) return Observable.Return(false);
                    return target.ConfirmActionCommand.CanExecute;
                },
                execute: x =>
                {
                    (x?.ConfirmActionCommand as ICommand)?.Execute(Unit.Default);
                    TargetConfirmation = null;
                },
                disposable: this);
        }
    }
}