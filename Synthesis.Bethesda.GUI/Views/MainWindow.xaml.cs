using System.ComponentModel;
using System.Windows;
using Autofac;
using Synthesis.Bethesda.Execution.Placement;
using Synthesis.Bethesda.GUI.Modules;
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

            var builder = new ContainerBuilder();
            builder.RegisterModule<MainModule>();
            builder.RegisterInstance(this)
                .AsSelf()
                .As<IWindowPlacement>()
                .As<IMainWindow>();
            var container = builder.Build();

            container.Resolve<IStartup>()
                .Initialize();
        }
    }
}
