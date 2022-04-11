using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.GUI.ViewModels.Groups;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

public class GroupRunVm : ViewModel, IRunItem
{
    public IGroupRun Run { get; }
    public RunDisplayControllerVm RunDisplayControllerVm { get; }
    public IEnumerable<PatcherRunVm> Patchers { get; }

    [Reactive]
    public bool IsSelected { get; set; }
        
    [Reactive]
    public bool AutoScrolling { get; set; }
        
    public ViewModel SourceVm { get; }
        
    public GroupVm GroupVm { get; }

    private readonly ObservableAsPropertyHelper<bool> _hasStarted;
    public bool HasStarted => _hasStarted.Value;

    private readonly ObservableAsPropertyHelper<string> _runTimeString;
    public string RunTimeString => _runTimeString.Value;

    public GroupRunVm(
        GroupVm groupVm,
        IGroupRun run,
        RunDisplayControllerVm runDisplayControllerVm,
        IEnumerable<PatcherRunVm> runVms)
    {
        SourceVm = groupVm;
        GroupVm = groupVm;
        Run = run;
        RunDisplayControllerVm = runDisplayControllerVm;

        Patchers = runVms;

        _hasStarted = runVms.AsObservableChangeSet()
            .FilterOnObservable(x => x.WhenAnyValue(x => x.State.Value)
                .Select(x => x != RunState.NotStarted))
            .QueryWhenChanged(q => q.Count > 0)
            .ToGuiProperty(this, nameof(HasStarted), deferSubscription: true);

        _runTimeString = runVms.AsObservableChangeSet()
            .AutoRefresh(x => x.RunTime)
            .Transform(x => x.RunTime, transformOnRefresh: true)
            .QueryWhenChanged(q =>
            {
                TimeSpan span = new();
                foreach (var s in q)
                {
                    span += s;
                }
                return span;
            })
            .Select(time =>
            {
                if (time == new TimeSpan()) return string.Empty;
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
    }

    public override string ToString()
    {
        return GroupVm.Name;
    }
}