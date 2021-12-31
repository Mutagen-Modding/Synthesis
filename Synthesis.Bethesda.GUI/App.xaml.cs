using System;
using System.Windows;
using Autofac;
using Noggog.IO;
using Serilog;
using Synthesis.Bethesda.Execution.Placement;
using Synthesis.Bethesda.GUI.Services.Startup;
using Synthesis.Bethesda.GUI.Views;

namespace Synthesis.Bethesda.GUI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
            
        var singleApp = new SingletonApplicationEnforcer("Synthesis");

        if (!singleApp.IsFirstApplication)
        {
            Application.Current.Shutdown();
            return;
        }
            
        var window = new MainWindow();
            
        try
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<Synthesis.Bethesda.GUI.Modules.MainModule>();
            builder.RegisterInstance(window)
                .AsSelf()
                .As<IWindowPlacement>()
                .As<IMainWindow>();
            builder.RegisterInstance(singleApp);
            var container = builder.Build();

            container.Resolve<IStartup>()
                .Initialize();
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error constructing container");
            throw;
        }
            
        window.Show();
    }
}