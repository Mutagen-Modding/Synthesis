using System.Reactive.Linq;
using Noggog;
using Noggog.Reactive;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.GUI.Services;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

namespace Synthesis.Bethesda.GUI.ViewModels.Top;

public class ActiveRunVm : ViewModel
{
    [Reactive]
    public RunVm? CurrentRun { get; set; }

    public ActiveRunVm(IActivePanelControllerVm activePanelController, ISchedulerProvider schedulerProvider)
    {
        this.WhenAnyValue(x => x.CurrentRun)
            .Do(run =>
            {
                if (run != null)
                {
                    activePanelController.ActivePanel = new ActiveViewModel(run,
                        async () =>
                        {
                            if (run.Running)
                            {
                                await run.Cancel().ConfigureAwait(false);
                            }
                            run.Dispose();
                        });
                }
            })
            .ObserveOn(schedulerProvider.TaskPool)
            .StartWith(default(RunVm?))
            .NotNull()
            .SubscribeAsyncConcat(x => x.Run())
            .DisposeWith(this);
    }
}