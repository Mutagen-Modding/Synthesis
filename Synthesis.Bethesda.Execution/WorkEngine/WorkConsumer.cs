using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Noggog;
using Noggog.Utility;
using Serilog;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.WorkEngine
{
    public class WorkConsumer : IDisposable, IStartupTask, IWorkConsumerSettings
    {
        private readonly Subject<int?> _numThreads = new();
        private readonly CompositeDisposable _disposable = new();
        private const int UnassignedCpuId = 0;
        private readonly ILogger _logger;
        private readonly IWorkQueue _queue;
        private readonly AsyncLock _lock = new();
        private int _desiredNumThreads;
        private readonly Dictionary<int, Task> _tasks = new();
        private int _nextCpuID = 1; // Start at 1, as 0 is "Unassigned"
        private static readonly AsyncLocal<int> _cpuId = new();
        private int CpuId => _cpuId.Value;
        private readonly CancellationTokenSource _shutdown = new();

        private readonly BehaviorSubject<(int DesiredCPUs, int CurrentCPUs)> _cpuCountSubj = new((0, 0));
        public IObservable<(int CurrentCPUs, int DesiredCPUs)> CurrentCpuCount => _cpuCountSubj;

        public WorkConsumer(
            ILogger logger,
            IWorkQueue queue)
        {
            _logger = logger;
            _queue = queue;
        }

        private async Task AddNewThreadsIfNeeded(int desired)
        {
            using (await _lock.WaitAsync())
            {
                _desiredNumThreads = desired;
                while (_desiredNumThreads > _tasks.Count)
                {
                    var cpuID = _nextCpuID++;
                    _tasks[cpuID] = Task.Run(() => ThreadBody(cpuID));
                }
                _cpuCountSubj.OnNext((_tasks.Count, _desiredNumThreads));
            }
        }

        private async Task ThreadBody(int cpuID)
        {
            _cpuId.Value = cpuID;

            try
            {
                while (true)
                {
                    if (_shutdown.IsCancellationRequested) return;

                    IToDo? toDo;
                    try
                    {
                        _queue.Reader.TryRead(out toDo);
                    }
                    catch (Exception)
                    {
                        throw new OperationCanceledException();
                    }

                    if (toDo != null)
                    {
                        if (toDo.IsAsync)
                        {
                            await toDo.DoAsync();
                        }
                        else
                        {
                            toDo.Do();
                        }
                    }
                    else
                    {
                        if (!await _queue.Reader.WaitToReadAsync())
                        {
                            return;
                        }
                    }

                    // Check if we're currently trimming threads
                    if (_desiredNumThreads >= _tasks.Count) continue;

                    // Noticed that we may need to shut down, lock and check again
                    using (await _lock.WaitAsync())
                    {
                        // Check if another thread shut down before this one and got us back to the desired amount already
                        if (_desiredNumThreads >= _tasks.Count) continue;

                        // Shutdown
                        if (!_tasks.Remove(cpuID))
                        {
                            _logger.Error("Could not remove thread from workpool with CPU ID {CpuId}", cpuID);
                        }
                        _cpuCountSubj.OnNext((_tasks.Count, _desiredNumThreads));
                        return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in WorkQueue thread");
            }
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }

        public void Start()
        {
            _numThreads
                .Select(x => x ?? 0)
                .Select(x => x == 0 ? Environment.ProcessorCount : x)
                .DistinctUntilChanged()
                .Do(x => _logger.Information("Number of cores to use: {NumCores}", x))
                .Subscribe(AddNewThreadsIfNeeded)
                .DisposeWithComposite(_disposable);
        }

        public void SetNumThreads(byte? threads)
        {
            _numThreads.OnNext(threads);
        }
    }
}