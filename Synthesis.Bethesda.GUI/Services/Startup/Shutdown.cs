using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Autofac;
using Newtonsoft.Json;
using Noggog.Utility;
using Serilog;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.GUI.DI;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.GUI.Views;

#if !DEBUG
using System.Diagnostics;
using Noggog.Utility;
#endif

namespace Synthesis.Bethesda.GUI.Services.Startup
{
    public interface IShutdown
    {
        bool IsShutdown { get; }

        void Prepare();
    }

    public class Shutdown : IShutdown
    {
        private readonly ILifetimeScope _Scope;
        private readonly ILogger _Logger;
        private readonly IStartupTracker _Init;
        private readonly IPipelineSettingsPath _PipelineSettingsPath;
        private readonly IGuiSettingsPath _GuiPaths;
        private readonly IRetrieveSaveSettings _Save;
        private readonly IMainWindow _Window;
        
        public bool IsShutdown { get; private set; }

        public Shutdown(
            ILifetimeScope scope,
            ILogger logger,
            IStartupTracker init,
            IPipelineSettingsPath paths,
            IGuiSettingsPath guiPaths,
            IRetrieveSaveSettings save,
            IMainWindow window)
        {
            _Scope = scope;
            _Logger = logger;
            _Init = init;
            _PipelineSettingsPath = paths;
            _GuiPaths = guiPaths;
            _Save = save;
            _Window = window;
        }

        public void Prepare()
        {
            _Window.Closing += (_, b) =>
            {
                _Window.Visibility = Visibility.Collapsed;
                Closing(b);
            };
        }
        
        private async void ExecuteShutdown()
        {
            IsShutdown = true;

            await Task.Run(() =>
            {
                if (!_Init.Initialized)
                {
                    _Logger.Information("App was unable to start up.  Not saving settings.");
                    return;
                }

                try
                {
                    _Save.Retrieve(out var gui, out var pipe);
                    File.WriteAllText(_PipelineSettingsPath.Path,
                        JsonConvert.SerializeObject(pipe, Formatting.Indented, Execution.Constants.JsonSettings));
                    File.WriteAllText(_GuiPaths.Path,
                        JsonConvert.SerializeObject(gui, Formatting.Indented, Execution.Constants.JsonSettings));
                }
                catch (Exception e)
                {
                    _Logger.Error("Error saving settings", e);
                }
            });
            
            var toDo = new List<Task>();
#if !DEBUG
            toDo.Add(Task.Run(async () =>
            {
                try
                {
                    using var process = _ProcessFactory.Create(
                        new ProcessStartInfo("dotnet", $"build-server shutdown"));
                    using var output = process.Output.Subscribe(x => _Logger.Information(x));
                    using var error = process.Error.Subscribe(x => _Logger.Information(x));
                    var ret = await process.Run();
                }
                catch (Exception e)
                {
                    _Logger.Error("Error shutting down build server", e);
                }
            }));
#endif

            toDo.Add(Task.Run(() =>
            {
                try
                {
                    _Logger.Information("Disposing container");
                    _Scope.Dispose();
                    _Logger.Information("Disposed container");
                }
                catch (Exception e)
                {
                    _Logger.Error("Error shutting down container actions", e);
                }
            }));
            await Task.WhenAll(toDo);
            Application.Current.Shutdown();
        }

        private void Closing(CancelEventArgs args)
        {
            if (IsShutdown) return;
            args.Cancel = true;
            ExecuteShutdown();
        }
    }
}