using DynamicData;
using Microsoft.WindowsAPICodePack.Dialogs;
using Mutagen.Bethesda;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public class ExistingSolutionInitVM : ASolutionInitializer
    {
        public override IObservable<GetResponse<InitializerCall>> InitializationCall { get; }

        public PathPickerVM SolutionPath { get; } = new PathPickerVM();

        [Reactive]
        public string ProjectName { get; set; } = string.Empty;

        private readonly ObservableAsPropertyHelper<ErrorResponse> _ProjectError;
        public ErrorResponse ProjectError => _ProjectError.Value;

        public ExistingSolutionInitVM()
        {
            SolutionPath.PathType = PathPickerVM.PathTypeOptions.File;
            SolutionPath.ExistCheckOption = PathPickerVM.CheckOptions.On;
            SolutionPath.Filters.Add(new CommonFileDialogFilter("Solution", ".sln"));

            var validation = Observable.CombineLatest(
                    SolutionPath.PathState(),
                    this.WhenAnyValue(x => x.ProjectName),
                    (sln, proj) => (sln, proj, validation: ValidateProjectPath(proj, sln)))
                .Replay(1)
                .RefCount();

            _ProjectError = validation
                .Select(i => (ErrorResponse)i.validation)
                .ToGuiProperty<ErrorResponse>(this, nameof(ProjectError), ErrorResponse.Success);

            InitializationCall = validation
                .Select((i) =>
                {
                    if (!i.sln.Succeeded) return i.sln.BubbleFailure<InitializerCall>();
                    if (!i.validation.Succeeded) return i.validation.BubbleFailure<InitializerCall>();
                    return GetResponse<InitializerCall>.Succeed(async (profile) =>
                    {
                        var patcher = new SolutionPatcherVM(profile);
                        CreateProject(i.validation.Value, patcher.Profile.Release.ToCategory());
                        AddProjectToSolution(i.sln.Value, i.validation.Value);
                        patcher.SolutionPath.TargetPath = i.sln.Value;
                        patcher.ProjectSubpath = Path.Combine(i.proj, $"{i.proj}.csproj");
                        return patcher.AsEnumerable();
                    });
                });
        }
    }
}
