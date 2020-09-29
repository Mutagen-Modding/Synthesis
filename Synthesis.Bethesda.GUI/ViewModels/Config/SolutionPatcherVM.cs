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
using System.Threading;
using Serilog.Context;
using Serilog;
using Serilog.Events;
using Newtonsoft.Json;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Synthesis.DTO;

namespace Synthesis.Bethesda.GUI
{
    public class SolutionPatcherVM : PatcherVM
    {
        public PathPickerVM SolutionPath { get; } = new PathPickerVM()
        {
            ExistCheckOption = PathPickerVM.CheckOptions.On,
            PathType = PathPickerVM.PathTypeOptions.File,
        };

        public IObservableCollection<string> AvailableProjects { get; }

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

        [Reactive]
        public string Description { get; set; } = string.Empty;

        [Reactive]
        public bool HiddenByDefault { get; set; }

        public SolutionPatcherVM(ProfileVM parent, SolutionPatcherSettings? settings = null)
            : base(parent, settings)
        {
            CopyInSettings(settings);
            SolutionPath.Filters.Add(new CommonFileDialogFilter("Solution", ".sln"));
            SelectedProjectPath.Filters.Add(new CommonFileDialogFilter("Project", ".csproj"));

            _DisplayName = Observable.CombineLatest(
                this.WhenAnyValue(x => x.Nickname),
                this.WhenAnyValue(x => x.SelectedProjectPath.TargetPath)
                    .StartWith(settings?.ProjectSubpath ?? string.Empty),
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
                .ToProperty(this, nameof(DisplayName), Nickname);

            AvailableProjects = SolutionPatcherConfigLogic.AvailableProject(
                this.WhenAnyValue(x => x.SolutionPath.TargetPath))
                .ObserveOnGui()
                .ToObservableCollection(this);

            var projPath = SolutionPatcherConfigLogic.ProjectPath(
                solutionPath: this.WhenAnyValue(x => x.SolutionPath.TargetPath),
                projectSubpath: this.WhenAnyValue(x => x.ProjectSubpath));
            projPath
                .Subscribe(p => SelectedProjectPath.TargetPath = p)
                .DisposeWith(this);

            var pathToExe = projPath
                .ObserveOn(RxApp.TaskpoolScheduler)
                .SelectReplaceWithIntermediate(
                    new ConfigurationStateVM<string>(default!)
                    {
                        IsHaltingError = false,
                        RunnableState = ErrorResponse.Fail("Locating exe to run.")
                    },
                    async (projectPath, cancel) =>
                    {
                        GetResponse<string> exe;
                        using (Log.Logger.Time($"locate path to exe from {projectPath}"))
                        {
                            exe = await SolutionPatcherConfigLogic.PathToExe(projectPath, cancel);
                            if (exe.Failed) return new ConfigurationStateVM<string>(exe.BubbleFailure<string>());
                        }

                        using (Logger.Time($"building {projectPath}"))
                        {
                            // Now we want to build, just to prep for run
                            var build = await SolutionPatcherRun.CompileWithDotnet(projectPath, cancel).ConfigureAwait(false);
                            if (build.Failed) return new ConfigurationStateVM<string>(build.BubbleFailure<string>());
                        }

                        return new ConfigurationStateVM<string>(exe);
                    })
                .Replay(1)
                .RefCount();

            _PathToExe = pathToExe
                .Select(r => r.Item ?? string.Empty)
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
                            .Select(i => i.ToUnit())),
                    (sln, proj, exe) =>
                    {
                        if (sln.Failed) return new ConfigurationStateVM(sln);
                        if (exe.RunnableState.Failed) return exe;
                        return new ConfigurationStateVM(proj);
                    })
                .ToGuiProperty<ConfigurationStateVM>(this, nameof(State), ConfigurationStateVM.Success);

            OpenSolutionCommand = ReactiveCommand.Create(
                canExecute: this.WhenAnyValue(x => x.SolutionPath.InError)
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

            var metaPath = this.WhenAnyValue(x => x.SelectedProjectPath.TargetPath)
                .Select(projPath =>
                {
                    try
                    {
                        return Path.Combine(Path.GetDirectoryName(projPath)!, "SynthesisMeta.json");
                    }
                    catch (Exception)
                    {
                        return string.Empty;
                    }
                })
                .Replay(1)
                .RefCount();

            // Set up meta file sync
            metaPath
                .Select(path =>
                {
                    return Noggog.ObservableExt.WatchFile(path)
                        .StartWith(Unit.Default)
                        .Select(_ =>
                        {
                            try
                            {
                                return JsonConvert.DeserializeObject<PatcherInfo>(File.ReadAllText(path));
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex, "Error reading in meta");
                            }
                            return default(PatcherInfo?);
                        });
                })
                .Switch()
                .DistinctUntilChanged()
                .ObserveOnGui()
                .Subscribe(info =>
                {
                    if (info == null) return;
                    if (info.Nickname != null)
                    {
                        this.Nickname = info.Nickname;
                    }
                    this.Description = info.Description ?? string.Empty;
                    this.HiddenByDefault = info.HideByDefault;
                })
                .DisposeWith(this);

            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.DisplayName),
                    this.WhenAnyValue(x => x.Description),
                    this.WhenAnyValue(x => x.HiddenByDefault),
                    metaPath,
                    (nickname, desc, hidden, meta) => (nickname, desc, hidden, meta))
                .DistinctUntilChanged()
                .Throttle(TimeSpan.FromMilliseconds(200), RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(x.meta)) return;
                        File.WriteAllText(x.meta,
                            JsonConvert.SerializeObject(
                                new PatcherInfo()
                                {
                                    Description = x.desc,
                                    HideByDefault = x.hidden,
                                    Nickname = x.nickname
                                },
                                Formatting.Indented));
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Error writing out meta");
                    }
                })
                .DisposeWith(this);
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

        public class SolutionPatcherConfigLogic
        {
            public static IObservable<IChangeSet<string>> AvailableProject(IObservable<string> solutionPath)
            {
                return solutionPath
                    .ObserveOn(RxApp.TaskpoolScheduler)
                    .Select(AvailableProject)
                    .Select(x => x.AsObservableChangeSet())
                    .Switch()
                    .RefCount();
            }

            public static IEnumerable<string> AvailableProject(string solutionPath)
            {
                if (!File.Exists(solutionPath)) return Enumerable.Empty<string>();
                try
                {
                    var manager = new AnalyzerManager(solutionPath);
                    return manager.Projects.Keys.Select(projPath => projPath.TrimStart($"{Path.GetDirectoryName(solutionPath)}\\"!));
                }
                catch (Exception)
                {
                    return Enumerable.Empty<string>();
                }
            }

            public static IObservable<string> ProjectPath(IObservable<string> solutionPath, IObservable<string> projectSubpath)
            {
                return projectSubpath
                    // Need to throttle, as bindings flip to null quickly, which we want to skip
                    .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                    .DistinctUntilChanged()
                    .CombineLatest(solutionPath.DistinctUntilChanged(),
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
            }

            public static async Task<GetResponse<string>> PathToExe(string projectPath, CancellationToken cancel)
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

                    return GetResponse<string>.Succeed(cmd);
                }
                catch (Exception ex)
                {
                    return GetResponse<string>.Fail(ex);
                }
            }
        }
    }
}
