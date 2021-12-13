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
                foreach (var startupTask in _startupTasks)
                {
                    startupTask.Start();;
                }
                _logger.Information("Loading settings");
                _mainVm.Value.Load();
                _logger.Information("Loaded settings");
                _logger.Information("Setting Main VM");
                _window.DataContext = _mainVm.Value;
                _logger.Information("Set Main VM");
                _logger.Information("Initializing Main VM");
                _mainVm.Value.Init();
                _logger.Information("Initialized Main VM");
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