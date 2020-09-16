using DynamicData;
using Microsoft.WindowsAPICodePack.Dialogs;
using Synthesis.Bethesda.Execution.Settings;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using Synthesis.Bethesda.Execution.Patchers;
using Buildalyzer;
using System.Linq;
using DynamicData.Binding;
using Synthesis.Bethesda.Execution;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Diagnostics;
using Buildalyzer.Environment;
using System.Reactive;
using Microsoft.Build.Evaluation;

namespace Synthesis.Bethesda.GUI
{
    public class SolutionPatcherVM : PatcherVM
    {
        public PathPickerVM SolutionPath { get; } = new PathPickerVM()
        {
            ExistCheckOption = PathPickerVM.CheckOptions.On,
            PathType = PathPickerVM.PathTypeOptions.File,
        };

        public IObservableCollection<string> ProjectsDisplay { get; }

        [Reactive]
        public string ProjectSubpath { get; set; } = string.Empty;

        private readonly ObservableAsPropertyHelper<string> _PathToExe;
        public string PathToExe => _PathToExe.Value;

        public PathPickerVM SelectedProjectPath { get; } = new PathPickerVM()
        {
            ExistCheckOption = PathPickerVM.CheckOptions.On,
            PathType = PathPickerVM.PathTypeOptions.File,
        };

        private readonly ObservableAsPropertyHelper<string> _DisplayName;
        public override string DisplayName => _DisplayName.Value;

        private readonly ObservableAsPropertyHelper<ConfigurationStateVM> _State;
        public override ConfigurationStateVM State => _State.Value;

        public ICommand OpenSolutionCommand { get; }

        public SolutionPatcherVM(ProfileVM parent, SolutionPatcherSettings? settings = null)
            : base(parent, settings)
        {
            CopyInSettings(settings);
            SolutionPath.Filters.Add(new CommonFileDialogFilter("Solution", ".sln"));
            SelectedProjectPath.Filters.Add(new CommonFileDialogFilter("Project", ".csproj"));

            _DisplayName = this.WhenAnyValue(
                x => x.Nickname,
                x => x.SelectedProjectPath.TargetPath,
                (nickname, path) =>
                {
                    if (!string.IsNullOrWhiteSpace(nickname)) return nickname;
                    try
                    {
                        var name = Path.GetFileName(Path.GetDirectoryName(path));
                        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
                        return name;
                    }
                    catch (Exception)
                    {
                        return string.Empty;
                    }
                })
                .ToGuiProperty<string>(this, nameof(DisplayName));

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

            var projPath = this.WhenAnyValue(x => x.ProjectSubpath)
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
                .Replay(1)
                .RefCount();

            projPath
                .Subscribe(p => SelectedProjectPath.TargetPath = p)
                .DisposeWith(this);

            var pathToExe = projPath
                .ObserveOn(RxApp.TaskpoolScheduler)
                .SelectReplace(async (projectPath, cancel) =>
                {
                    try
                    {
                        cancel.ThrowIfCancellationRequested();
                        if (!File.Exists(projectPath))
                        {
                            return GetResponse<string>.Fail("Project path does not exist.");
                        }
                        // Right now this is slow as it cleans the build results unnecessarily.  Need to look into that
                        var manager = new AnalyzerManager();
                        cancel.ThrowIfCancellationRequested();
                        var proj = manager.GetProject(projectPath);
                        cancel.ThrowIfCancellationRequested();
                        var opt = new EnvironmentOptions();
                        opt.TargetsToBuild.SetTo("Build");
                        var build = proj.Build();
                        cancel.ThrowIfCancellationRequested();
                        var results = build.Results.ToArray();
                        if (results.Length != 1)
                        {
                            return GetResponse<string>.Fail("Unsupported number of build results.");
                        }
                        var result = results[0];
                        if (!result.Properties.TryGetValue("RunCommand", out var cmd))
                        {
                            return GetResponse<string>.Fail("Could not find executable to be run");
                        }

                        // Now we want to build, just to prep for run
                        var resp = await SolutionPatcherRun.CompileWithDotnet(projectPath, cancel).ConfigureAwait(false);
                        if (resp.Failed) return resp.BubbleFailure<string>();

                        return GetResponse<string>.Succeed(cmd);
                    }
                    catch (Exception ex)
                    {
                        return GetResponse<string>.Fail(ex);
                    }
                })
                .Replay(1)
                .RefCount();

            _PathToExe = pathToExe
                .Select(r => r.Value)
                .ToGuiProperty<string>(this, nameof(PathToExe));

            _State = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.SolutionPath.ErrorState),
                    this.WhenAnyValue(x => x.SelectedProjectPath.ErrorState),
                    Observable.Merge(
                        projPath
                            .Select(_ => new ConfigurationStateVM()
                            {
                                IsHaltingError = false,
                                RunnableState = ErrorResponse.Fail("Building")
                            }),
                        pathToExe
                            .Select(p => (ConfigurationStateVM)(ErrorResponse)p)),
                    (sln, proj, exe) =>
                    {
                        if (sln.Failed) return sln;
                        if (exe.RunnableState.Failed) return exe;
                        return proj;
                    })
                .ToGuiProperty<ConfigurationStateVM>(this, nameof(State), ConfigurationStateVM.Success);

            OpenSolutionCommand = ReactiveCommand.Create(
                canExecute: this.WhenAnyValue(x => x.State.IsHaltingError)
                    .Select(x => !x),
                execute: () =>
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(SolutionPath.TargetPath)
                        {
                            UseShellExecute = true,
                        });
                    }
                    catch (Exception)
                    {
                        // ToDo
                        // Log
                    }
                });
        }

        public override PatcherSettings Save()
        {
            var ret = new SolutionPatcherSettings();
            CopyOverSave(ret);
            ret.SolutionPath = this.SolutionPath.TargetPath;
            ret.ProjectSubpath = this.ProjectSubpath;
            return ret;
        }

        private void CopyInSettings(SolutionPatcherSettings? settings)
        {
            if (settings == null) return;
            this.SolutionPath.TargetPath = settings.SolutionPath;
            this.ProjectSubpath = settings.ProjectSubpath;
        }

        public override PatcherRunVM ToRunner(PatchersRunVM parent)
        {
            return new PatcherRunVM(
                parent,
                this,
                new SolutionPatcherRun(
                    nickname: DisplayName,
                    pathToSln: SolutionPath.TargetPath,
                    pathToExe: PathToExe,
                    pathToProj: SelectedProjectPath.TargetPath));
        }

        public override PatcherInitVM? CreateInitializer()
        {
            return new SolutionPatcherInitVM(Profile.Config.MainVM, this);
        }
    }
}
