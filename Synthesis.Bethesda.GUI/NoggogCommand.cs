using Noggog;
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
        public static ReactiveCommand<Unit, Unit> CreateFromJob<TJob>(
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

        public static ReactiveCommand<Unit, Unit> CreateFromJob<TInput, TJob>(
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
                    canExecute ?? Observable.Return(true),
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

        public static ReactiveCommand<Unit, Unit> CreateFromObject<TObject>(
            IObservable<TObject> objectSource,
            Func<TObject, bool> canExecute,
            Action<TObject> execute,
            CompositeDisposable disposable)
        {
            TObject latest = default!;
            objectSource
                .Subscribe(l => latest = l)
                .DisposeWith(disposable);
            return ReactiveCommand.Create(
                canExecute: objectSource.Select(canExecute),
                execute: () =>
                {
                    execute(latest);
                });
        }

        public static ReactiveCommand<Unit, Unit> CreateFromObject<TObject>(
            IObservable<TObject> objectSource,
            Func<TObject, bool> canExecute,
            Func<TObject, Task> execute,
            CompositeDisposable disposable)
        {
            TObject latest = default!;
            objectSource
                .Subscribe(l => latest = l)
                .DisposeWith(disposable);
            return ReactiveCommand.CreateFromTask(
                canExecute: objectSource.Select(canExecute),
                execute: async () =>
                {
                    await execute(latest);
                });
        }

        public static ReactiveCommand<Unit, Unit> CreateFromObject<TObject>(
            IObservable<TObject> objectSource,
            Func<TObject, bool> canExecute,
            IObservable<bool> extraCanExecute,
            Action<TObject> execute,
            CompositeDisposable disposable)
        {
            TObject latest = default!;
            objectSource
                .Subscribe(l => latest = l)
                .DisposeWith(disposable);
            return ReactiveCommand.Create(
                canExecute: Observable.CombineLatest(
                    objectSource.Select(canExecute),
                    extraCanExecute,
                    (obj, ex) => obj && ex),
                execute: () =>
                {
                    execute(latest);
                });
        }

        public static ReactiveCommand<Unit, Unit> CreateFromObject<TObject>(
            IObservable<TObject> objectSource,
            Func<TObject, bool> canExecute,
            IObservable<bool> extraCanExecute,
            Func<TObject, Task> execute,
            CompositeDisposable disposable)
        {
            TObject latest = default!;
            objectSource
                .Subscribe(l => latest = l)
                .DisposeWith(disposable);
            return ReactiveCommand.CreateFromTask(
                canExecute: Observable.CombineLatest(
                    objectSource.Select(canExecute),
                    extraCanExecute,
                    (obj, ex) => obj && ex),
                execute: async() => 
                {
                    await execute(latest);
                });
        }

        public static ReactiveCommand<Unit, Unit> CreateFromObject<TObject>(
            IObservable<TObject> objectSource,
            Func<IObservable<TObject>, IObservable<bool>> canExecute,
            Action<TObject> execute,
            CompositeDisposable disposable)
        {
            TObject latest = default!;
            objectSource
                .Subscribe(l => latest = l)
                .DisposeWith(disposable);
            return ReactiveCommand.Create(
                canExecute: canExecute(objectSource.ObserveOnGui()),
                execute: () =>
                {
                    execute(latest);
                });
        }

        public static ReactiveCommand<Unit, Unit> CreateFromObject<TObject>(
            IObservable<TObject> objectSource,
            Func<IObservable<TObject>, IObservable<bool>> canExecute,
            IObservable<bool> extraCanExecute,
            Action<TObject> execute,
            CompositeDisposable disposable)
        {
            TObject latest = default!;
            objectSource
                .Subscribe(l => latest = l)
                .DisposeWith(disposable);
            return ReactiveCommand.Create(
                canExecute: Observable.CombineLatest(
                    canExecute(objectSource.ObserveOnGui()),
                    extraCanExecute,
                    (obj, extra) => obj && extra),
                execute: () =>
                {
                    execute(latest);
                });
        }
    }
}
