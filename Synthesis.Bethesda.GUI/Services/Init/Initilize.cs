using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using Noggog;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.GUI.Settings;

namespace Synthesis.Bethesda.GUI.Services
{
    public interface IInitilize
    {
        public bool Initialized { get; }
        void Initialize(Window window);
    }

    public class Initilize : IInitilize
    {
        private readonly ILogger _Logger;
        private readonly IClearLoading _Loading;
        private readonly Lazy<ISettingsSingleton> _Settings;
        
        public bool Initialized { get; set; }

        public Initilize(
            ILogger logger,
            IClearLoading loading,
            Lazy<ISettingsSingleton> settings)
        {
            _Logger = logger;
            _Loading = loading;
            _Settings = settings;
        }
        
        public async void Initialize(Window window)
        {
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
                        var mainVM = Inject.Container.GetInstance<MainVM>();
                        mainVM.Load();

                        window.DataContext = mainVM;
                        mainVM.Init();
                    });
                Initialized = true;
            }
            catch (Exception e)
            {
                _Logger.Error(e, "Error initializing app");
                Application.Current.Shutdown();
            }
        }
    }
}