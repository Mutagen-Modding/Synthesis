using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public static class NoggogCommand
    {
        public static ReactiveCommandBase<Unit, Unit> CreateFromJob<TJob>(
            Func<(TJob Job, IObservable<Unit> CompletionSignal)> jobCreator,
            out IObservable<TJob> createdJobs,
            IObservable<bool>? canExecute = null,
            IScheduler? outputScheduler = null)
        {
            var jobs = new Subject<TJob>();
            createdJobs = jobs;
            return ReactiveCommand.CreateFromTask(
                canExecute: canExecute,
                outputScheduler: outputScheduler,
                execute: async () =>
                {
                    var j = jobCreator();
                    jobs.OnNext(j.Job);
                    await j.CompletionSignal;
                });
        }

        public static ReactiveCommandBase<Unit, Unit> CreateFromJob<TInput, TJob>(
            Func<TInput, (TJob? Job, IObservable<Unit> CompletionSignal)> jobCreator,
            IObservable<TInput> extraInput,
            out IObservable<TJob?> createdJobs,
            IObservable<bool>? canExecute = null,
            IScheduler? outputScheduler = null)
            where TJob : class
        {
            var jobs = new Subject<TJob?>();
            var gotInput = new BehaviorSubject<bool>(false);
            createdJobs = jobs;
            TInput lastInput = default!;
            // ToDo
            // Find a way to dispose, or research if it needs to be?
            var dispose = extraInput.Subscribe(i =>
            {
                lastInput = i;
                if (gotInput.Value) return;
                gotInput.OnNext(true);
            });
            return ReactiveCommand.CreateFromTask(
                canExecute: Observable.CombineLatest(
                    canExecute,
                    gotInput.ObserveOnGui(),
                    (canExecute, gotInput) => canExecute && gotInput),
                outputScheduler: outputScheduler,
                execute: async () =>
                {
                    var j = jobCreator(lastInput);
                    jobs.OnNext(j.Job);
                    await j.CompletionSignal;
                });
        }
    }
}
