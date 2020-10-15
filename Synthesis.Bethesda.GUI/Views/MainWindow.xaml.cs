using MahApps.Metro.Controls;
using Synthesis.Bethesda.Execution.Settings;
using Newtonsoft.Json;
using Noggog.WPF;
using System.IO;
using System;
using Newtonsoft.Json.Converters;
using Noggog;
using Newtonsoft.Json.Linq;

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
            const string GuiSettingsPath = "GuiSettings.json";
            var mainVM = new MainVM();
            SynthesisGuiSettings? guiSettings = null;
            if (File.Exists(GuiSettingsPath))
            {
                guiSettings = JsonConvert.DeserializeObject<SynthesisGuiSettings>(File.ReadAllText(GuiSettingsPath), Execution.Constants.JsonSettings)!;
            }
            PipelineSettings? pipeSettings = null;
            if (File.Exists(Execution.Constants.SettingsFileName))
            {
                pipeSettings = JsonConvert.DeserializeObject<PipelineSettings>(File.ReadAllText(Execution.Constants.SettingsFileName), Execution.Constants.JsonSettings)!;
            }

            // Backwards compatibility
            const string OldGuiSettingsPath = "Settings.json";
            if (guiSettings == null && pipeSettings == null
                && File.Exists(OldGuiSettingsPath))
            {
                var rawText = File.ReadAllText(OldGuiSettingsPath);
                guiSettings = JsonConvert.DeserializeObject<SynthesisGuiSettings>(rawText, Execution.Constants.JsonSettings)!;
                JObject rawObj = JObject.Parse(rawText);
                var execSettings = rawObj["ExecutableSettings"];
                if (execSettings != null)
                {
                    guiSettings.SelectedProfile = execSettings["SelectedProfile"]?.ToString() ?? string.Empty;
                    pipeSettings = JsonConvert.DeserializeObject<PipelineSettings>(execSettings.ToString(), Execution.Constants.JsonSettings)!;
                }
            }
            mainVM.Load(guiSettings, pipeSettings);
            Closed += (a, b) =>
            {
                mainVM.Save(out var gui, out var pipe);
                File.WriteAllText(Execution.Constants.SettingsFileName, JsonConvert.SerializeObject(pipe, Formatting.Indented, Execution.Constants.JsonSettings));
                File.WriteAllText(GuiSettingsPath, JsonConvert.SerializeObject(gui, Formatting.Indented, Execution.Constants.JsonSettings));
                mainVM.Dispose();
            };

            DataContext = mainVM;
            mainVM.Init();
        }
    }
}
