using MahApps.Metro.Controls;
using Synthesis.Bethesda.Execution.Settings;
using Newtonsoft.Json;
using System.IO;
using System;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Synthesis.Versioning;
using Noggog;
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
            SynthesisGuiSettings? guiSettings = null;
            PipelineSettings? pipeSettings = null;
            Task.WhenAll(
                Task.Run(async () =>
                {
                    if (File.Exists(Paths.GuiSettingsPath))
                    {
                        guiSettings = JsonConvert.DeserializeObject<SynthesisGuiSettings>(await File.ReadAllTextAsync(Paths.GuiSettingsPath), Execution.Constants.JsonSettings)!;
                    }
                }),
                Task.Run(async () =>
                {
                    if (File.Exists(Execution.Paths.SettingsFileName))
                    {
                        pipeSettings = JsonConvert.DeserializeObject<PipelineSettings>(await File.ReadAllTextAsync(Execution.Paths.SettingsFileName), Execution.Constants.JsonSettings)!;
                    }
                })
            ).Wait();

            Inject.Container.RegisterInstance<Window>(this);

            var init = Inject.Scope.GetInstance<IInitilize>();
            init.Initialize().Wait();
            
            var mainVM = Inject.Scope.GetInstance<MainVM>();
            mainVM.Load(guiSettings, pipeSettings);
            Closing += (a, b) =>
            {
                if (mainVM.IsShutdown) return;
                b.Cancel = true;
                this.Visibility = Visibility.Collapsed;
                mainVM.Shutdown();
            };

            DataContext = mainVM;
            mainVM.Init();
        }
    }
}
