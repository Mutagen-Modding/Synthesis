using System;
using DynamicData;
using Noggog;
using Noggog.WPF;
using Serilog;
using Synthesis.Bethesda.GUI.ViewModels.Groups;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles
{
    public interface IProfileGroupsList
    {
        SourceList<GroupVm> Groups { get; }
    }

    public class ProfileGroupsList : ViewModel, IProfileGroupsList
    {
        public SourceList<GroupVm> Groups { get; } = new();

        public ProfileGroupsList(ILogger logger)
        {
            Groups.Connect()
                .OnItemRemoved(p =>
                {
                    logger.Information($"Disposing of {p.Name} because it was removed.");
                    p.Dispose();
                })
                .Subscribe()
                .DisposeWith(this);
        }
    }
}