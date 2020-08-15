using DynamicData;
using Microsoft.WindowsAPICodePack.Dialogs;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public class ExistingProjectInitVM : ASolutionInitializer
    {
        public override IObservable<GetResponse<Func<SolutionPatcherVM, Task>>> InitializationCall { get; }

        public PathPickerVM SolutionPath { get; } = new PathPickerVM();

        public PathPickerVM ProjectPath { get; } = new PathPickerVM();

        public ExistingProjectInitVM()
        {
            SolutionPath.PathType = PathPickerVM.PathTypeOptions.File;
            SolutionPath.ExistCheckOption = PathPickerVM.CheckOptions.On;
            SolutionPath.Filters.Add(new CommonFileDialogFilter("Solution", ".sln"));

            ProjectPath.PathType = PathPickerVM.PathTypeOptions.File;
            ProjectPath.ExistCheckOption = PathPickerVM.CheckOptions.On;
            ProjectPath.Filters.Add(new CommonFileDialogFilter("Project", ".csproj"));

            InitializationCall = Observable.CombineLatest(
                    SolutionPath.PathState(),
                    ProjectPath.PathState(),
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
