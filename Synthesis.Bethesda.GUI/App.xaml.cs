using System;
using System.IO;
using System.Windows;
using Autofac;
using Microsoft.Build.Logging.StructuredLogger;
using Noggog.IO;
using Serilog;
using Serilog.Core;
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
            singleApp.ForwardArgs(e.Args);
            Application.Current.Shutdown();
            return;
        }
        else
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length == 0)
            {
                Logging.Log.Logger.Warning("Expected args with start directory as first argument");
            }
            else
            {
                var parentDir = Path.GetDirectoryName(args[0]);
                if (parentDir == null)
                {
                    Logging.Log.Logger.Warning("Could not find parent directory of exe");
                }
                else if (!parentDir.Equals(Environment.CurrentDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    Environment.CurrentDirectory = parentDir;
                    Logging.Log.Logger.Information($"Changed current directory to: {parentDir}");
                }
            }
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
            builder.RegisterInstance(new ArgumentReceiver(singleApp, e.Args))
                .AsImplementedInterfaces();
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