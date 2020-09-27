using Synthesis.Bethesda.Execution.Settings;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Reflection;
using Microsoft.CodeAnalysis.Emit;
using System.Reactive;
using System.Threading;
using Synthesis.Bethesda.Execution.Patchers;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Controls;
using System.Linq;

namespace Synthesis.Bethesda.GUI
{
    public class CodeSnippetPatcherVM : PatcherVM
    {
        private readonly ObservableAsPropertyHelper<string> _DisplayName;
        public override string DisplayName => _DisplayName.Value;

        public string ID { get; private set; } = string.Empty;

        [Reactive]
        public string Code { get; set; } = string.Empty;

        private readonly ObservableAsPropertyHelper<ConfigurationStateVM> _State;
        public override ConfigurationStateVM State => _State.Value;

        private readonly ObservableAsPropertyHelper<string> _CompilationText;
        public string CompilationText => _CompilationText.Value;

        private readonly ObservableAsPropertyHelper<CodeCompilationStatus> _CompilationStatus;
        public CodeCompilationStatus CompilationStatus => _CompilationStatus.Value;

        private readonly ObservableAsPropertyHelper<Assembly?> _ActiveAssembly;
        public Assembly? ActiveAssembly => _ActiveAssembly.Value;

        public CodeSnippetPatcherVM(ProfileVM parent, CodeSnippetPatcherSettings? settings = null)
            : base(parent, settings)
        {
            CopyInSettings(settings);
            _DisplayName = this.WhenAnyValue(x => x.Nickname)
                .Select(x =>
                {
                    if (string.IsNullOrWhiteSpace(x))
                    {
                        return "<No Name>";
                    }
                    else
                    {
                        return x;
                    }
                })
                .ToGuiProperty<string>(this, nameof(DisplayName));

            IObservable<(MemoryStream? AssemblyStream, EmitResult? CompileResults, Exception? Exception)> compileResults =
                Observable.Merge(
                    // Anytime code changes, wipe and mark as "compiling"
                    this.WhenAnyValue(x => x.Code)
                        .Select(_ => (default(MemoryStream?), default(EmitResult?), default(Exception?))),
                    // Start actual compiling task,
                    this.WhenAnyValue(x => x.Code)
                        // Throttle input
                        .Throttle(TimeSpan.FromMilliseconds(350), RxApp.MainThreadScheduler)
                        // Stick on background thread
                        .ObserveOn(RxApp.TaskpoolScheduler)
                        .Select(code =>
                        {
                            CancellationTokenSource cancel = new CancellationTokenSource();
                            try
                            {
                                var emit = CodeSnippetPatcherRun.Compile(
                                    parent.Release,
                                    ID,
                                    code,
                                    cancel.Token,
                                    out var assembly);
                                return (emit.Success ? assembly : default, emit, default(Exception?));
                            }
                            catch (TaskCanceledException)
                            {
                                return (default(MemoryStream?), default(EmitResult?), default(Exception?));
                            }
                            catch (Exception ex)
                            {
                                return (default(MemoryStream?), default(EmitResult?), ex);
                            }
                        }))
                    .ObserveOnGui()
                .Replay(1)
                .RefCount();

            IObservable<ConfigurationStateVM> compileState = compileResults
                .Select(results => ToState(results))
                .Replay(1)
                .RefCount();

            _CompilationText = compileState
                .Select(err => err.RunnableState.Reason)
                .ToGuiProperty<string>(this, nameof(CompilationText));

            _CompilationStatus = compileResults
                .Select(results =>
                {
                    if (results.Exception != null || (!results.CompileResults?.Success ?? false))
                    {
                        return CodeCompilationStatus.Error;
                    }
                    if (results.AssemblyStream == null || results.CompileResults == null)
                    {
                        return CodeCompilationStatus.Compiling;
                    }
                    return CodeCompilationStatus.Successful;
                })
                .ToGuiProperty(this, nameof(CompilationStatus));

            var stateAndAssembly = compileResults
                .Select(results =>
                {
                    var state = ToState(results);
                    if (results.AssemblyStream == null) return Observable.Return((state, default(Assembly?)));
                    return Observable.Return(results.AssemblyStream)
                        .ObserveOn(RxApp.TaskpoolScheduler)
                        .Select(x =>
                        {
                            return (state, (Assembly?)Assembly.Load(x.ToArray()));
                        });
                })
                .Switch()
                .Replay(1)
                .RefCount();

            _ActiveAssembly = stateAndAssembly
                .Select(results => results.Item2)
                .ToGuiProperty(this, nameof(ActiveAssembly));

            _State = stateAndAssembly
                .Select(results =>
                {
                    if (!results.state.RunnableState.Succeeded) return results.state;
                    if (results.Item2 == null)
                    {
                        return new ConfigurationStateVM()
                        {
                            RunnableState = ErrorResponse.Fail("Assembly was unexpectedly null"),
                            IsHaltingError = true,
                        };
                    }
                    return ConfigurationStateVM.Success;
                })
                .ToGuiProperty<ConfigurationStateVM>(this, nameof(State), ConfigurationStateVM.Success);
        }

        public override PatcherSettings Save()
        {
            var ret = new CodeSnippetPatcherSettings();
            CopyOverSave(ret);
            ret.Code = this.Code;
            ret.ID = this.ID;
            return ret;
        }

        private void CopyInSettings(CodeSnippetPatcherSettings? settings)
        {
            if (settings == null)
            {
                this.ID = Guid.NewGuid().ToString();
                return;
            }
            this.Code = settings.Code;
            this.ID = string.IsNullOrWhiteSpace(settings.ID) ? Guid.NewGuid().ToString() : settings.ID;
        }

        public override PatcherRunVM ToRunner(PatchersRunVM parent)
        {
            if (ActiveAssembly == null)
            {
                throw new ArgumentNullException("Assembly was null when trying to run");
            }
            return new PatcherRunVM(
                parent,
                this,
                new CodeSnippetPatcherRun(
                    Nickname,
                    ActiveAssembly));
        }

        private ConfigurationStateVM ToState((MemoryStream? AssemblyStream, EmitResult? CompileResults, Exception? Exception) results)
        {
            if (results.CompileResults == null)
            {
                return new ConfigurationStateVM()
                {
                    RunnableState = ErrorResponse.Fail("Compiling")
                };
            }
            if (results.Exception != null)
            {
                return new ConfigurationStateVM()
                {
                    RunnableState = ErrorResponse.Fail(results.Exception),
                    IsHaltingError = true
                };
            }
            if (results.CompileResults.Success)
            {
                return new ConfigurationStateVM()
                {
                    RunnableState = ErrorResponse.Succeed("Compilation successful")
                };
            }
            var errDiag = results.CompileResults.Diagnostics
                .Where(e => e.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .FirstOrDefault();
            if (errDiag == null)
            {
                return new ConfigurationStateVM()
                {
                    RunnableState = ErrorResponse.Fail("Unknown error"),
                    IsHaltingError = true,
                };
            }
            return new ConfigurationStateVM()
            {
                RunnableState = ErrorResponse.Fail(errDiag.ToString()),
                IsHaltingError = true,
            };
        }
    }

    public enum CodeCompilationStatus
    {
        Successful,
        Compiling,
        Error
    }
}
