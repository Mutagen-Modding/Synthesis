using MahApps.Metro.Controls;
using Synthesis.Bethesda.Execution.Settings;
using Newtonsoft.Json;
using System.IO;
using System;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Noggog;

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

            Log.Logger.Information("===============================================");
            Log.Logger.Information("============== Opening Synthesis ==============");
            Log.Logger.Information("===============================================");
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
                }),
                Task.Run(() =>
                {
                    try
                    {
                        var loadingDir = new DirectoryInfo(Execution.Paths.LoadingFolder);
                        if (!loadingDir.Exists) return;
                        Log.Logger.Information("Clearing Loading folder");
                        loadingDir.DeleteEntireFolder(deleteFolderItself: false);
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex, "Error clearing Loading folder");
                    }
                })
            ).Wait();

            var mainVM = new MainVM(this);
            mainVM.Load(guiSettings, pipeSettings);
            Closed += (a, b) =>
            {
                mainVM.Save(out var gui, out var pipe);
                File.WriteAllText(Execution.Paths.SettingsFileName, JsonConvert.SerializeObject(pipe, Formatting.Indented, Execution.Constants.JsonSettings));
                File.WriteAllText(Paths.GuiSettingsPath, JsonConvert.SerializeObject(gui, Formatting.Indented, Execution.Constants.JsonSettings));
                mainVM.Dispose();
            };

            DataContext = mainVM;
            mainVM.Init();
        }
    }
}
