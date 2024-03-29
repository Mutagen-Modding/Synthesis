using System.IO;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Microsoft.WindowsAPICodePack.Dialogs;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.PatcherInstantiation;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Solution;

public class ExistingProjectInitVm : ASolutionInitializer
{
    public override IObservable<GetResponse<InitializerCall>> InitializationCall { get; }

    public PathPickerVM SolutionPath { get; } = new();

    public IObservableCollection<string> AvailableProjects { get; }

    public SourceList<string> SelectedProjects { get; } = new();

    public PathPickerVM SelectedProjectPath { get; } = new()
    {
        ExistCheckOption = PathPickerVM.CheckOptions.On,
        PathType = PathPickerVM.PathTypeOptions.File,
    };

    public ExistingProjectInitVm(
        IAvailableProjectsRetriever availableProjectsRetriever,
        IPatcherFactory patcherFactory)
    {
        SolutionPath.PathType = PathPickerVM.PathTypeOptions.File;
        SolutionPath.ExistCheckOption = PathPickerVM.CheckOptions.On;
        SolutionPath.Filters.Add(new CommonFileDialogFilter("Solution", ".sln"));

        AvailableProjects = this.WhenAnyValue(x => x.SolutionPath.TargetPath)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Select(x => availableProjectsRetriever.Get(x))
            .Select(x => x.AsObservableChangeSet())
            .Switch()
            .ObserveOnGui()
            .ToObservableCollection(this);

        InitializationCall = SelectedProjects.Connect()
            .Transform(subPath =>
            {
                if (SolutionPath.TargetPath.IsNullOrWhitespace()) return string.Empty;
                try
                {
                    return Path.Combine(Path.GetDirectoryName(SolutionPath.TargetPath)!, subPath);
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            })
            .Transform(path =>
            {
                var pathPicker = new PathPickerVM
                {
                    PathType = PathPickerVM.PathTypeOptions.File,
                    ExistCheckOption = PathPickerVM.CheckOptions.On,
                    TargetPath = path
                };
                pathPicker.Filters.Add(new CommonFileDialogFilter("Project", ".csproj"));
                return pathPicker;
            })
            .DisposeMany()
            .QueryWhenChanged(q =>
            {
                if (q.Count == 0) return GetResponse<InitializerCall>.Fail("No projects selected");
                var err = q
                    .Select(p => p.ErrorState)
                    .Where(e => e.Failed)
                    .And(ErrorResponse.Success)
                    .First();
                if (err.Failed) return err.BubbleFailure<InitializerCall>();
                return GetResponse<InitializerCall>.Succeed(async () =>
                {
                    return q.Select(i =>
                    {
                        var patcher = patcherFactory.GetSolutionPatcher(new SolutionPatcherSettings()
                        {
                            SolutionPath = SolutionPath.TargetPath,
                            ProjectSubpath =  i.TargetPath.TrimStart($"{Path.GetDirectoryName(SolutionPath.TargetPath)}\\"!)
                        });
                        return patcher;
                    });
                });
            });
    }
}