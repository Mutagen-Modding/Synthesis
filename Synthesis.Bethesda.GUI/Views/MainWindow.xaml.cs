using MahApps.Metro.Controls;
using System;
using System.Windows;
using Mutagen.Bethesda.Synthesis.Versioning;
using Synthesis.Bethesda.GUI.Services;

namespace Synthesis.Bethesda.GUI.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();

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

            Inject.Container.RegisterInstance<Window>(this);

            var init = Inject.Scope.GetInstance<IInitilize>();
            init.Initialize().Wait();
            
            var mainVM = Inject.Scope.GetInstance<MainVM>();
            mainVM.Load();
            
            var shutdown = Inject.Scope.GetInstance<IExecuteShutdown>();
            Closing += (_, b) =>
            {
                Visibility = Visibility.Collapsed;
                shutdown.Closing(b);
            };

            DataContext = mainVM;
            mainVM.Init();
        }
    }
}
