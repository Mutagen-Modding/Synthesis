using System;
using System.IO;
using System.Reactive.Linq;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Synthesis.Projects;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Solution
{
    public class NewSolutionInitVm : ASolutionInitializer
    {
        public PathPickerVM ParentDirPath { get; } = new PathPickerVM();

        [Reactive]
        public string SolutionName { get; set; } = string.Empty;

        [Reactive]
        public string ProjectName { get; set; } = string.Empty;

        private readonly ObservableAsPropertyHelper<GetResponse<string>> _SolutionPath;
        public GetResponse<string> SolutionPath => _SolutionPath.Value;

        public override IObservable<GetResponse<InitializerCall>> InitializationCall { get; }

        private readonly ObservableAsPropertyHelper<ErrorResponse> _ProjectError;
        public ErrorResponse ProjectError => _ProjectError.Value;

        private readonly ObservableAsPropertyHelper<string> _ProjectNameWatermark;
        public string ProjectNameWatermark => _ProjectNameWatermark.Value;

        public NewSolutionInitVm(
            IGameCategoryContext gameCategoryContext,
            Func<SolutionPatcherVm> patcherFactory,
            IValidateProjectPath validateProjectPath,
            ICreateSolutionFile createSolutionFile,
            ICreateProject createProject,
            IAddProjectToSolution addProjectToSolution,
            IGenerateGitIgnore gitIgnore)
        {
            ParentDirPath.PathType = PathPickerVM.PathTypeOptions.Folder;
            ParentDirPath.ExistCheckOption = PathPickerVM.CheckOptions.On;

            _SolutionPath = Observable.CombineLatest(
                this.ParentDirPath.PathState(),
                this.WhenAnyValue(x => x.SolutionName),
                (parentDir, slnName) =>
                {
                    if (string.IsNullOrWhiteSpace(slnName)) return GetResponse<string>.Fail(val: slnName, reason: "Solution needs a name.");

                    // Will reevaluate once parent dir is fixed
                    if (parentDir.Failed) return GetResponse<string>.Succeed(value: slnName);
                    try
                    {
                        var slnPath = Path.Combine(parentDir.Value, slnName);
                        if (File.Exists(slnPath))
                        {
                            return GetResponse<string>.Fail(val: slnName, reason: $"Target solution folder cannot already exist as a file: {slnPath}");
                        }
                        if (Directory.Exists(slnPath)
                            && (Directory.EnumerateFiles(slnPath).Any()
                            || Directory.EnumerateDirectories(slnPath).Any()))
                        {
                            return GetResponse<string>.Fail(val: slnName, reason: $"Target solution folder must be empty: {slnPath}");
                        }
                        return GetResponse<string>.Succeed(Path.Combine(slnPath, $"{slnName}.sln"));
                    }
                    catch (ArgumentException)
                    {
                        return GetResponse<string>.Fail(val: slnName, reason: "Improper solution name. Go simpler.");
                    }
                })
                .ToGuiProperty(this, nameof(SolutionPath));

            var validation = Observable.CombineLatest(
                    this.ParentDirPath.PathState(),
                    this.WhenAnyValue(x => x.SolutionName),
                    this.WhenAnyValue(x => x.SolutionPath),
                    this.WhenAnyValue(x => x.ProjectName),
                    (parentDir, slnName, sln, proj) =>
                    {
                        // Use solution name if proj empty.
                        if (string.IsNullOrWhiteSpace(proj))
                        {
                            proj = SolutionNameProcessor(slnName);
                        }
                        return (parentDir, sln, proj, validation: validateProjectPath.Validate(proj, sln));
                    })
                .Replay(1)
                .RefCount();

            _ProjectError = validation
                .Select(i => (ErrorResponse)i.validation)
                .ToGuiProperty<ErrorResponse>(this, nameof(ProjectError), ErrorResponse.Success);

            InitializationCall = validation
                .Select((i) =>
                {
                    if (i.parentDir.Failed) return i.parentDir.BubbleFailure<InitializerCall>();
                    if (i.sln.Failed) return i.sln.BubbleFailure<InitializerCall>();
                    if (i.validation.Failed) return i.validation.BubbleFailure<InitializerCall>();
                    return GetResponse<InitializerCall>.Succeed(async () =>
                    {
                        var patcher = patcherFactory();
                        createSolutionFile.Create(i.sln.Value);
                        createProject.Create(gameCategoryContext.Category, i.validation.Value);
                        addProjectToSolution.Add(i.sln.Value, i.validation.Value);
                        gitIgnore.Generate(Path.GetDirectoryName(i.sln.Value)!);
                        patcher.SolutionPathInput.Picker.TargetPath = i.sln.Value;
                        var projName = Path.GetFileNameWithoutExtension(i.validation.Value);
                        patcher.SelectedProjectInput.ProjectSubpath = Path.Combine(projName, $"{projName}.csproj");
                        return patcher.AsEnumerable();
                    });
                });

            _ProjectNameWatermark = this.WhenAnyValue(x => x.SolutionName)
                .Select(x => string.IsNullOrWhiteSpace(x) ? "The name of the patcher" : SolutionNameProcessor(x))
                .ToGuiProperty<string>(this, nameof(ProjectNameWatermark), string.Empty);
        }

        private string SolutionNameProcessor(string slnName) => slnName.Replace(" ", string.Empty);
    }
}
