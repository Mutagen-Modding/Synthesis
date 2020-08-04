using MahApps.Metro.Controls;
using Synthesis.Bethesda.Execution.Settings;
using Newtonsoft.Json;
using Noggog.WPF;
using System.IO;

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
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto
            };
            var mvm = this.WireMainVM<MainVM>(
                $"Settings.json",
                load: (s, vm) => vm.Load(JsonConvert.DeserializeObject<SynthesisSettings>(File.ReadAllText(s), settings)!),
                save: (s, vm) => File.WriteAllText(s, JsonConvert.SerializeObject(vm.Save(), Formatting.Indented, settings)));
            mvm.Init();
        }
    }
}
