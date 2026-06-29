using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Noggog.Reactive;
using Noggog.UI;
using Noggog.WPF;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git;

public interface IAvailableProjects
{
    IObservableCollection<string> Projects { get; }
}

public class AvailableProjects : ViewModel, IAvailableProjects
{
    public IObservableCollection<string> Projects { get; }

    public AvailableProjects(
        IDriverRepositoryPreparationFollower driverRepositoryPreparation,
        ISchedulerProvider schedulerProvider)
    {
        Projects = driverRepositoryPreparation.DriverInfo
            .Select(x => x.Item?.AvailableProjects ?? Enumerable.Empty<string>())
            .Select(x => x.AsObservableChangeSet<string>())
            .Switch()
            .ToObservableCollection(this, schedulerProvider.MainThread);
    }
}