using System.Reactive.Linq;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

namespace Synthesis.Bethesda.GUI.ViewModels.Top
{
    public class ActiveRunVm : ViewModel
    {
        [Reactive]
        public RunVm? CurrentRun { get; private set; }

        public ActiveRunVm(
            IActivePanelControllerVm activePanelController)
        {
            this.WhenAnyValue(x => x.CurrentRun)
                .Do(run => activePanelController.ActivePanel = run)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .StartWith(default(RunVm?))
                .Pairwise()
                .Subscribe(async r =>
                {
                    if (r.Previous != null
                        && r.Previous.Running)
                    {
                        await r.Previous.Cancel();
                    }
                    
                    if (r.Current != null)
                    {
                        await r.Current.Run(); 
                    }
                })
                .DisposeWith(this);
        }

        public void SetCurrentRun(RunVm runVm)
        {
            CurrentRun = runVm;
        }
    }
}