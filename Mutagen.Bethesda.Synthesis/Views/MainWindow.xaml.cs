using MahApps.Metro.Controls;
using Mutagen.Bethesda.Synthesis.Core.Settings;
using Newtonsoft.Json;
using Noggog.WPF;
using System.IO;

namespace Mutagen.Bethesda.Synthesis.Views
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
            this.WireMainVM<MainVM>(
                $"Settings.json",
                load: (s, vm) => vm.Load(JsonConvert.DeserializeObject<SynthesisSettings>(File.ReadAllText(s), settings)!),
                save: (s, vm) => File.WriteAllText(s, JsonConvert.SerializeObject(vm.Save(), Formatting.Indented, settings)));
        }
    }
}
