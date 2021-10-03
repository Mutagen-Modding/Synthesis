using System;
using System.IO;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.WindowsAPICodePack.Dialogs;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Synthesis.Projects;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.PatcherInstantiation;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Solution
{
    public class ExistingSolutionInitVm : ASolutionInitializer
    {
        public override IObservable<GetResponse<InitializerCall>> InitializationCall { get; }

        public PathPickerVM SolutionPath { get; } = new PathPickerVM();

        [Reactive]
        public string ProjectName { get; set; } = string.Empty;

        private readonly ObservableAsPropertyHelper<ErrorResponse> _ProjectError;
        public ErrorResponse ProjectError => _ProjectError.Value;

        public ExistingSolutionInitVm(
            IGameCategoryContext gameCategoryContext,
            IPatcherFactory patcherFactory,
            IValidateProjectPath validateProjectPath,
            ICreateProject createProject,
            IAddProjectToSolution addProjectToSolution)
        {
            SolutionPath.PathType = PathPickerVM.PathTypeOptions.File;
            SolutionPath.ExistCheckOption = PathPickerVM.CheckOptions.On;
            SolutionPath.Filters.Add(new CommonFileDialogFilter("Solution", ".sln"));

            var validation = Observable.CombineLatest(
                    SolutionPath.PathState(),
                    this.WhenAnyValue(x => x.ProjectName),
                    (sln, proj) => (sln, proj, validation: validateProjectPath.Validate(proj, sln)))
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
                    return GetResponse<InitializerCall>.Succeed(async () =>
                    {
                        createProject.Create(gameCategoryContext.Category, i.validation.Value);
                        addProjectToSolution.Add(i.sln.Value, i.validation.Value);
                        var patcher = patcherFactory.GetSolutionPatcher(new SolutionPatcherSettings()
                        {
                            SolutionPath = i.sln.Value,
                            ProjectSubpath = Path.Combine(i.proj, $"{i.proj}.csproj"),
                        });
                        return patcher.AsEnumerable();
                    });
                });
        }
    }
}
