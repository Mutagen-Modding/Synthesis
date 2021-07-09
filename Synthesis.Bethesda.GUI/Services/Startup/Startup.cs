using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using Mutagen.Bethesda.Synthesis.Versioning;
using Noggog;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.GUI.Views;

namespace Synthesis.Bethesda.GUI.Services.Startup
{
    public interface IStartup
    {
        void Initialize();
    }

    public class Startup : IStartup
    {
        private readonly ILogger _Logger;
        private readonly IClearLoading _Loading;
        private readonly Lazy<ISettingsSingleton> _Settings;
        private readonly Lazy<MainVm> _MainVm;
        private readonly IMainWindow _Window;
        private readonly IStartupTracker _Tracker;
        private readonly IShutdown _Shutdown;

        public Startup(
            ILogger logger,
            IClearLoading loading,
            Lazy<ISettingsSingleton> settings,
            Lazy<MainVm> mainVm,
            IMainWindow window,
            IStartupTracker tracker,
            IShutdown shutdown)
        {
            _Logger = logger;
            _Loading = loading;
            _Settings = settings;
            _MainVm = mainVm;
            _Window = window;
            _Tracker = tracker;
            _Shutdown = shutdown;
        }
        
        public async void Initialize()
        {
            AppDomain.CurrentDomain.UnhandledException += (o, e) =>
            {
                Log.Logger.Error(e.ExceptionObject as Exception, "Crashing");
            };

            var versionLine = $"============== Opening Synthesis v{Versions.SynthesisVersion} ==============";
            var bars = new string('=', versionLine.Length);
            Log.Logger.Information(bars);
            Log.Logger.Information(versionLine);
            Log.Logger.Information(bars);
            Log.Logger.Information(DateTime.Now.ToString());
            
            try
            {
                await Observable.Return(Unit.Default)
                    .SelectTask(async (_) =>
                    {
                        await Task.WhenAll(
                            Task.Run(() =>
                            {
                                _Loading.Do();
                            }),
                            Task.Run(() =>
                            {
                                _Settings.Value.GetType();
                            })).ConfigureAwait(false);
                    })
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Do(_ =>
                    {
                        var mainVM = _MainVm.Value;
                        mainVM.Load();

                        _Window.DataContext = mainVM;
                        mainVM.Init();
                    });
                _Tracker.Initialized = true;
            }
            catch (Exception e)
            {
                _Logger.Error(e, "Error initializing app");
                Application.Current.Shutdown();
            }
            
            _Shutdown.Prepare();
        }
    }
}