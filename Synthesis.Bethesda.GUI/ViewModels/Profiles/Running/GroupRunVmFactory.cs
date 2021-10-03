using System;
using System.Linq;
using System.Threading;
using DynamicData;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.GUI.ViewModels.Groups;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Running
{
    public interface IGroupRunVmFactory
    {
        GroupRunVm ToRunner(GroupVm groupVm, CancellationToken cancel);
    }

    public class GroupRunVmFactory : IGroupRunVmFactory
    {
        private readonly RunDisplayControllerVm _runDisplayControllerVm;
        private readonly IPatcherRunnerFactory _runnerFactory;
        private readonly IPrepPatcherForRun _prepPatcherForRun;

        public GroupRunVmFactory(
            RunDisplayControllerVm runDisplayControllerVm,
            IPatcherRunnerFactory runnerFactory,
            IPrepPatcherForRun prepPatcherForRun)
        {
            _runDisplayControllerVm = runDisplayControllerVm;
            _runnerFactory = runnerFactory;
            _prepPatcherForRun = prepPatcherForRun;
        }
        
        public GroupRunVm ToRunner(GroupVm groupVm, CancellationToken cancel)
        {
            if (groupVm.ModKey.Failed)
            {
                throw new ArgumentException($"Group ModKey was not valid: {groupVm.ModKey.Reason}");
            }
            
            var patcherVms = groupVm.Patchers.Items
                .Where(x => x.IsOn)
                .Select(x => _runnerFactory.ToRunner(x))
                .ToArray();
            var preps = patcherVms
                        .Select(p => p.Run)
                        .Select(x => _prepPatcherForRun.Prep(x, cancel))
                        .ToArray();
            return new GroupRunVm(
                groupVm,
                new GroupRun(
                    groupVm.ModKey.Value,
                    preps),
                _runDisplayControllerVm,
                patcherVms);
        }
    }
}