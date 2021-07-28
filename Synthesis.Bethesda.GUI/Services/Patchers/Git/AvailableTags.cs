using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.GUI.Services.Patchers.Solution;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git
{
    public interface IAvailableTags
    {
        IObservableCollection<string> Tags { get; }
    }

    public class AvailableTags : ViewModel, IAvailableTags
    {
        public IObservableCollection<string> Tags { get; }

        public AvailableTags(
            ISelectedProjectInputVm selectedProjectInput,
            IDriverRepositoryPreparationFollower driverRepositoryPreparation,
            IAvailableProjects availableProjects)
        {
            var tagInput = Observable.CombineLatest(
                selectedProjectInput.Picker.WhenAnyValue(x => x.TargetPath),
                availableProjects.Projects.WhenAnyValue(x => x.Count),
                (targetPath, count) => (targetPath, count));
            Tags = driverRepositoryPreparation.DriverInfo
                .Select(x => x.Item?.Tags ?? Enumerable.Empty<(int Index, string Name, string Sha)>())
                .Select(x => x.AsObservableChangeSet())
                .Switch()
                .Filter(
                    tagInput.Select(x =>
                    {
                        if (x.count == 0) return new Func<(int Index, string Name, string Sha), bool>(_ => false);
                        if (x.count == 1) return new Func<(int Index, string Name, string Sha), bool>(_ => true);
                        if (!x.targetPath.EndsWith(".csproj"))
                            return new Func<(int Index, string Name, string Sha), bool>(_ => false);
                        var projName = Path.GetFileName(x.targetPath);
                        return new Func<(int Index, string Name, string Sha), bool>(i =>
                            i.Name.StartsWith(projName, StringComparison.OrdinalIgnoreCase));
                    }))
                .Sort(SortExpressionComparer<(int Index, string Name, string Sha)>.Descending(x => x.Index))
                .Transform(x => x.Name)
                .ToObservableCollection(this);
        }
    }
}