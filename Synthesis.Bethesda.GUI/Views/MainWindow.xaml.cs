using System.ComponentModel;
using System.Windows;
using Synthesis.Bethesda.Execution.Placement;
using Synthesis.Bethesda.GUI.Services.Startup;

namespace Synthesis.Bethesda.GUI.Views
{
    public interface IMainWindow
    {
        public Visibility Visibility { get; set; }
        public object DataContext { get; set; }
        event CancelEventHandler Closing;
    }
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : IMainWindow, IWindowPlacement
    {
        public MainWindow()
        {
            InitializeComponent();

            new Inject(c =>
            {
                c.ForSingletonOf<IWindowPlacement>().Use(this);
                c.ForSingletonOf<IMainWindow>().Use(this);
            });

            Inject.Container.GetInstance<IStartup>()
                .Initialize();
        }
    }
}
