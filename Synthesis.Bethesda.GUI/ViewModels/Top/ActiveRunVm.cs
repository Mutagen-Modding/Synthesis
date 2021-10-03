using System.Reactive.Linq;
using System.Windows.Input;
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
        public RunVm? CurrentRun { get; set; }
        
        public ActiveRunVm(IActivePanelControllerVm activePanelController)
        {
            this.WhenAnyValue(x => x.CurrentRun)
                .Do(run =>
                {
                    if (run != null)
                    {
                        activePanelController.ActivePanel = run;
                    }
                })
                .ObserveOn(RxApp.TaskpoolScheduler)
                .StartWith(default(RunVm?))
                .Pairwise()
                .Subscribe(async r =>
                {
                    if (r.Previous != null)
                    {
                        if (r.Previous.Running)
                        {
                            await r.Previous.Cancel();
                        }
                        r.Previous.Dispose();
                    }
                    
                    if (r.Current != null)
                    {
                        await r.Current.Run(); 
                    }
                })
                .DisposeWith(this);
        }
    }
}