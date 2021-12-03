using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using Mutagen.Bethesda.Synthesis.Versioning;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.GUI.ViewModels.Top;
using Synthesis.Bethesda.GUI.Views;

namespace Synthesis.Bethesda.GUI.Services.Startup
{
    public interface IStartup
    {
        void Initialize();
    }

    public class Startup : IStartup
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<IStartupTask> _startupTasks;
        private readonly Lazy<MainVm> _mainVm;
        private readonly IMainWindow _window;
        private readonly IStartupTracker _tracker;
        private readonly IShutdown _shutdown;

        public Startup(
            ILogger logger,
            IEnumerable<IStartupTask> startupTasks,
            Lazy<MainVm> mainVm,
            IMainWindow window,
            IStartupTracker tracker,
            IShutdown shutdown)
        {
            _logger = logger;
            _startupTasks = startupTasks;
            _mainVm = mainVm;
            _window = window;
            _tracker = tracker;
            _shutdown = shutdown;
        }
        
        public async void Initialize()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                _logger.Error(e.ExceptionObject as Exception, "Crashing");
            };

            var versionLine = $"============== Opening Synthesis v{Versions.SynthesisVersion} ==============";
            var bars = new string('=', versionLine.Length);
            _logger.Information(bars);
            _logger.Information(versionLine);
            _logger.Information(bars);
            _logger.Information(DateTime.Now.ToString());
            
            try
            {
                await Observable.FromAsync(() =>
                        Task.WhenAll(_startupTasks
                            .Select(x => Task.Run(x.Start))))
                    .Select(x => _mainVm.Value)
                    .Do(mainVM =>
                    {
                        _logger.Information("Loading settings");
                        mainVM.Load();
                        _logger.Information("Loaded settings");
                    })
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Do(mainVM =>
                    {
                        _logger.Information("Setting Main VM");
                        _window.DataContext = mainVM;
                        _logger.Information("Set Main VM");
                    })
                    .ObserveOn(RxApp.TaskpoolScheduler)
                    .Do(mainVM =>
                    {
                        _logger.Information("Initializing Main VM");
                        mainVM.Init();
                        _logger.Information("Initialized Main VM");
                    });
                _tracker.Initialized = true;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error initializing app");
                Application.Current.Shutdown();
            }
            
            _shutdown.Prepare();
        }
    }
}