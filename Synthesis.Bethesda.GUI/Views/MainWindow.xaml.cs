using System.ComponentModel;
using System.Windows;
using Synthesis.Bethesda.GUI.Services.Startup;

namespace Synthesis.Bethesda.GUI.Views
{
    public interface IMainWindow
    {
        public Visibility Visibility { get; set; }
        public object DataContext { get; set; }
        event CancelEventHandler Closing;
        double Left { get; set; }
        double Top { get; set; }
        double Width { get; set; }
        double Height { get; set; }
    }
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : IMainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            new Inject(c => c.ForSingletonOf<IMainWindow>().Use(this));

            Inject.Container.GetInstance<IStartup>()
                .Initialize();
        }
    }
}
