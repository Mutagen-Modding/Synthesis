using System.Reactive;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Synthesis.Bethesda.GUI
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
            DiscardActionCommand = NoggogCommand.CreateFromObject(
                objectSource: this.WhenAnyValue(x => x.TargetConfirmation),
                canExecute: target => target != null,
                execute: (_) =>
                {
                    TargetConfirmation = null;
                },
                disposable: this.CompositeDisposable);
            ConfirmActionCommand = NoggogCommand.CreateFromObject(
                objectSource: this.WhenAnyFallback(x => x.TargetConfirmation!.ToDo),
                canExecute: toDo => toDo != null,
                execute: toDo =>
                {
                    toDo?.Invoke();
                    TargetConfirmation = null;
                },
                disposable: this.CompositeDisposable);
        }
    }
}