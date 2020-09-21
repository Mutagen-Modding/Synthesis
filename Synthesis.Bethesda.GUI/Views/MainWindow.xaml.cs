using MahApps.Metro.Controls;
using Synthesis.Bethesda.Execution.Settings;
using Newtonsoft.Json;
using Noggog.WPF;
using System.IO;
using System;

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
            Log.Logger.Information("Starting");
            JsonSerializerSettings jsonSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto
            };
            var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Synthesis", "Settings.json");
            var mvm = this.WireMainVM<MainVM>(
                settingsPath,
                load: (s, vm) => vm.Load(JsonConvert.DeserializeObject<SynthesisGuiSettings>(File.ReadAllText(s), jsonSettings)!),
                save: (s, vm) => File.WriteAllText(s, JsonConvert.SerializeObject(vm.Save(), Formatting.Indented, jsonSettings)));
            mvm.Init();
        }
    }
}
