using Synthesis.Bethesda.GUI.ViewModels.Groups;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles
{
    public class StartRun
    {
        private readonly ActiveRunVm _activeRunVm;
        private readonly IRunFactory _runFactory;

        public StartRun(
            ActiveRunVm activeRunVm,
            IRunFactory runFactory)
        {
            _activeRunVm = activeRunVm;
            _runFactory = runFactory;
        }
        
        public void Start(params GroupVm[] groups)
        {
            _activeRunVm.CurrentRun = _runFactory.GetRun(groups);
        }
    }
}