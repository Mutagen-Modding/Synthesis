using DynamicData;
using DynamicData.Binding;
using Microsoft.WindowsAPICodePack.Dialogs;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Patchers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Synthesis.Bethesda.Execution.DotNet;

namespace Synthesis.Bethesda.GUI
{
    public class ExistingProjectInitVM : ASolutionInitializer
    {
        public override IObservable<GetResponse<InitializerCall>> InitializationCall { get; }

        public PathPickerVM SolutionPath { get; } = new PathPickerVM();

        public IObservableCollection<string> AvailableProjects { get; }

        public SourceList<string> SelectedProjects { get; } = new SourceList<string>();

        public PathPickerVM SelectedProjectPath { get; } = new PathPickerVM()
        {
            ExistCheckOption = PathPickerVM.CheckOptions.On,
            PathType = PathPickerVM.PathTypeOptions.File,
        };

        public ExistingProjectInitVM()
        {
            SolutionPath.PathType = PathPickerVM.PathTypeOptions.File;
            SolutionPath.ExistCheckOption = PathPickerVM.CheckOptions.On;
            SolutionPath.Filters.Add(new CommonFileDialogFilter("Solution", ".sln"));

            AvailableProjects = this.WhenAnyValue(x => x.SolutionPath.TargetPath)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Select(x => SolutionPatcherRun.AvailableProjectSubpaths(x))
                .Select(x => x.AsObservableChangeSet())
                .Switch()
                .ObserveOnGui()
                .ToObservableCollection(this);

            InitializationCall = SelectedProjects.Connect()
                .Transform(subPath =>
                {
                    if (subPath == null || this.SolutionPath.TargetPath == null) return string.Empty;
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
                    return GetResponse<InitializerCall>.Succeed(async (profile) =>
                    {
                        return q.Select(i =>
                        {
                            var patcher = new SolutionPatcherVM(profile,
                                Inject.Scope.GetInstance<IProvideInstalledSdk>(),
                                Inject.Scope.GetInstance<IProfileDisplayControllerVm>(),
                                Inject.Scope.GetInstance<IConfirmationPanelControllerVm>());
                            patcher.SolutionPath.TargetPath = SolutionPath.TargetPath;
                            patcher.ProjectSubpath = i.TargetPath.TrimStart($"{Path.GetDirectoryName(SolutionPath.TargetPath)}\\"!);
                            return patcher;
                        });
                    });
                });
        }
    }
}
