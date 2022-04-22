using System.Reactive.Linq;
using System.Text;
using ICSharpCode.AvalonEdit.Document;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Running;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

public class PatcherRunVm : ViewModel, IRunItem
{
    public Guid InternalID { get; }
    public IPatcherRun Run { get; }
    public ViewModel SourceVm { get; }

    [Reactive]
    public GetResponse<RunState> State { get; set; } = GetResponse<RunState>.Succeed(RunState.NotStarted);

    public TextDocument OutputDisplay { get; } = new();

    private readonly ObservableAsPropertyHelper<TimeSpan> _runTime;
    public TimeSpan RunTime => _runTime.Value;

    private readonly ObservableAsPropertyHelper<string> _runTimeString;
    public string RunTimeString => _runTimeString.Value;

    private readonly ObservableAsPropertyHelper<bool> _isRunning;
    public bool IsRunning => _isRunning.Value;

    private readonly ObservableAsPropertyHelper<bool> _isErrored;
    public bool IsErrored => _isErrored.Value;

    [Reactive]
    public bool IsSelected { get; set; }

    [Reactive]
    public bool AutoScrolling { get; set; }
        
    public string Name { get; }

    public delegate PatcherRunVm Factory(PatcherVm sourcePatcherVm);

    public PatcherRunVm(PatcherVm sourcePatcherVm, IPatcherRun run, IReporterLoggerWrapper loggerWrapper)
    {
        Name = sourcePatcherVm.NameVm.Name;
        InternalID = sourcePatcherVm.InternalID;
        Run = run;
        SourceVm = sourcePatcherVm;

        Observable.Merge(
                loggerWrapper.Events
                    .Select(e => e.RenderMessage()),
                this.WhenAnyValue(x => x.State)
                    .Where(x => x.Value == RunState.Error)
                    .Select(x => x.Reason))
            .Buffer(TimeSpan.FromMilliseconds(250), count: 1000, RxApp.TaskpoolScheduler)
            .Where(b => b.Count > 0)
            .ObserveOnGui()
            .Subscribe(output =>
            {
                StringBuilder sb = new();
                foreach (var line in output)
                {
                    sb.AppendLine(line);
                }
                OutputDisplay.Insert(OutputDisplay.TextLength, sb.ToString());
            })
            .DisposeWith(this);

        _isRunning = this.WhenAnyValue(x => x.State)
            .Select(x => x.Value == RunState.Started)
            .ToGuiProperty(this, nameof(IsRunning), deferSubscription: true);

        _isErrored = this.WhenAnyValue(x => x.State)
            .Select(x => x.Value == RunState.Error)
            .ToGuiProperty(this, nameof(IsErrored), deferSubscription: true);

        var runTime = Noggog.ObservableExt.TimePassed(TimeSpan.FromMilliseconds(100), RxApp.MainThreadScheduler)
            .FlowSwitch(this.WhenAnyValue(x => x.IsRunning))
            .Publish()
            .RefCount();

        _runTime = runTime
            .ToProperty(this, nameof(RunTime));

        _runTimeString = runTime
            .Select(time =>
            {
                if (time.TotalDays > 1)
                {
                    return $"{time.TotalDays:n1}d";
                }
                if (time.TotalHours > 1)
                {
                    return $"{time.TotalHours:n1}h";
                }
                if (time.TotalMinutes > 1)
                {
                    return $"{time.TotalMinutes:n1}m";
                }
                return $"{time.TotalSeconds:n1}s";
            })
            .ToGuiProperty<string>(this, nameof(RunTimeString), string.Empty, deferSubscription: true);

        this.WhenAnyValue(x => x.State)
            .Where(x => x.Succeeded && x.Value == RunState.Finished)
            .Subscribe(_ => sourcePatcherVm.SuccessfulRunCompleted())
            .DisposeWith(this);
    }

    public override string ToString()
    {
        return Run.ToString() ?? nameof(PatcherRunVm);
    }
}

public enum RunState
{
    NotStarted,
    Started,
    Finished,
    Error,
}