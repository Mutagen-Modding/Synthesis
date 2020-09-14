using Buildalyzer;
using DynamicData;
using DynamicData.Binding;
using Microsoft.WindowsAPICodePack.Dialogs;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public class ExistingProjectInitVM : ASolutionInitializer
    {
        public override IObservable<GetResponse<Func<SolutionPatcherVM, Task>>> InitializationCall { get; }

        public PathPickerVM SolutionPath { get; } = new PathPickerVM();

        public IObservableCollection<string> ProjectsDisplay { get; }

        [Reactive]
        public string ProjectSubpath { get; set; } = string.Empty;

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

            SelectedProjectPath.PathType = PathPickerVM.PathTypeOptions.File;
            SelectedProjectPath.ExistCheckOption = PathPickerVM.CheckOptions.On;
            SelectedProjectPath.Filters.Add(new CommonFileDialogFilter("Project", ".csproj"));

            ProjectsDisplay = this.WhenAnyValue(x => x.SolutionPath.TargetPath)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Select(x =>
                {
                    if (!File.Exists(x)) return Enumerable.Empty<string>();
                    try
                    {
                        var manager = new AnalyzerManager(x);
                        return manager.Projects.Keys.Select(projPath => projPath.TrimStart($"{Path.GetDirectoryName(x)}\\"!));
                    }
                    catch (Exception)
                    {
                        return Enumerable.Empty<string>();
                    }
                })
                .Select(x => x.AsObservableChangeSet())
                .Switch()
                .ObserveOnGui()
                .ToObservableCollection(this);

            this.WhenAnyValue(x => x.ProjectSubpath)
                .DistinctUntilChanged()
                .CombineLatest(this.WhenAnyValue(x => x.SolutionPath.TargetPath)
                        .DistinctUntilChanged(),
                    (subPath, slnPath) =>
                    {
                        if (subPath == null || slnPath == null) return string.Empty;
                        try
                        {
                            return Path.Combine(Path.GetDirectoryName(slnPath)!, subPath);
                        }
                        catch (Exception)
                        {
                            return string.Empty;
                        }
                    })
                .Subscribe(p => SelectedProjectPath.TargetPath = p)
                .DisposeWith(this);

            InitializationCall = Observable.CombineLatest(
                    SolutionPath.PathState(),
                    SelectedProjectPath.PathState(),
                    (sln, proj) => (sln, proj))
                .Select(i =>
                {
                    if (!i.sln.Succeeded) return i.sln.BubbleFailure<Func<SolutionPatcherVM, Task>>();
                    if (!i.proj.Succeeded) return i.proj.BubbleFailure<Func<SolutionPatcherVM, Task>>();
                    return GetResponse<Func<SolutionPatcherVM, Task>>.Succeed(async (patcher) =>
                    {
                        patcher.SolutionPath.TargetPath = i.sln.Value;
                        // Little delay, just to make sure things populated properly.  Might not be needed
                        await Task.Delay(300);
                        patcher.ProjectSubpath = i.proj.Value.TrimStart($"{Path.GetDirectoryName(i.sln.Value)}\\"!);
                    });
                });
        }
    }
}
