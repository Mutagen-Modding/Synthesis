using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using Mutagen.Bethesda.Synthesis.Versioning;
using Noggog;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.GUI.Logging;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.Settings;
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
        private readonly IClearLoading _loading;
        private readonly Lazy<ISettingsSingleton> _settings;
        private readonly Lazy<MainVm> _mainVm;
        private readonly IMainWindow _window;
        private readonly IStartupTracker _tracker;
        private readonly LogCleaner _logCleaner;
        private readonly IShutdown _shutdown;

        public Startup(
            ILogger logger,
            IClearLoading loading,
            Lazy<ISettingsSingleton> settings,
            Lazy<MainVm> mainVm,
            IMainWindow window,
            IStartupTracker tracker,
            LogCleaner logCleaner,
            IShutdown shutdown)
        {
            _logger = logger;
            _loading = loading;
            _settings = settings;
            _mainVm = mainVm;
            _window = window;
            _tracker = tracker;
            _logCleaner = logCleaner;
            _shutdown = shutdown;
        }
        
        public async void Initialize()
        {
            AppDomain.CurrentDomain.UnhandledException += (o, e) =>
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
                await Observable.Return(Unit.Default)
                    .SelectTask(async (_) =>
                    {
                        await Task.WhenAll(
                            Task.Run(() =>
                            {
                                _loading.Do();
                            }),
                            Task.Run(() =>
                            {
                                _settings.Value.GetType();
                            }),
                            Task.Run(() =>
                            {
                                _logCleaner.Clean();
                            })).ConfigureAwait(false);
                    })
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Do(_ =>
                    {
                        var mainVM = _mainVm.Value;
                        mainVM.Load();

                        _window.DataContext = mainVM;
                        mainVM.Init();
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