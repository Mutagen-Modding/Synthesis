using System;
using System.Reactive;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles
{
    public class OverallErrorVm : ErrorVM
    {
        public IProfileDisplayControllerVm DisplayController { get; }

        public OverallErrorVm(IProfileDisplayControllerVm displayControllerVm)
            : base("Overall Blocking Error")
        {
            DisplayController = displayControllerVm;
        }
        
        public ReactiveCommand<Unit, Unit> CreateCommand(IObservable<GetResponse<ViewModel>> errs)
        {
            return NoggogCommand.CreateFromObject(
                objectSource: errs,
                canExecute: o => o.Failed,
                execute: o =>
                {
                    if (o.Value is { } patcher)
                    {
                        DisplayController.SelectedObject = patcher;
                    }
                    else
                    {
                        var curDisplayed = DisplayController.SelectedObject;
                        if (curDisplayed is not ErrorVM)
                        {
                            BackAction = () => DisplayController.SelectedObject = curDisplayed;
                        }
                        else
                        {
                            BackAction = null;
                        }

                        DisplayController.SelectedObject = this;
                    }
                },
                disposable: this);
        }
    }
}