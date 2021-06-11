using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Synthesis.Bethesda.GUI.Settings;
    
#if !DEBUG
using System.Diagnostics;
using Noggog.Utility;
#endif

namespace Synthesis.Bethesda.GUI.Services
{
    public interface IExecuteShutdown
    {
        bool IsShutdown { get; }
        void Closing(CancelEventArgs args);
    }

    public class ExecuteShutdown : IExecuteShutdown
    {
        private readonly IInitilize _Init;
        private readonly IRetrieveSaveSettings _Save;

        public ExecuteShutdown(
            IInitilize init,
            IRetrieveSaveSettings save)
        {
            _Init = init;
            _Save = save;
        }
        
        public bool IsShutdown { get; private set; }
        
        private async void Shutdown()
        {
            IsShutdown = true;

            await Task.Run(() =>
            {
                if (!_Init.Initialized)
                {
                    Log.Logger.Information("App was unable to start up.  Not saving settings.");
                    return;
                }

                try
                {
                    _Save.Retrieve(out var gui, out var pipe);
                    File.WriteAllText(Execution.Paths.SettingsFileName,
                        JsonConvert.SerializeObject(pipe, Formatting.Indented, Execution.Constants.JsonSettings));
                    File.WriteAllText(Paths.GuiSettingsPath,
                        JsonConvert.SerializeObject(gui, Formatting.Indented, Execution.Constants.JsonSettings));
                }
                catch (Exception e)
                {
                    Log.Logger.Error("Error saving settings", e);
                }
            });
            
            var toDo = new List<Task>();
#if !DEBUG
            toDo.Add(Task.Run(async () =>
            {
                try
                {
                    using var process = ProcessWrapper.Create(
                        new ProcessStartInfo("dotnet", $"build-server shutdown"));
                    using var output = process.Output.Subscribe(x => Log.Logger.Information(x));
                    using var error = process.Error.Subscribe(x => Log.Logger.Information(x));
                    var ret = await process.Run();
                }
                catch (Exception e)
                {
                    Log.Logger.Error("Error shutting down build server", e);
                }
            }));
#endif

            toDo.Add(Task.Run(() =>
            {
                try
                {
                    Log.Logger.Information("Disposing injection");
                    Inject.Container.Dispose();
                    Log.Logger.Information("Disposed injection");
                }
                catch (Exception e)
                {
                    Log.Logger.Error("Error shutting down injector actions", e);
                }
            }));
            await Task.WhenAll(toDo);
            Application.Current.Shutdown();
        }

        public void Closing(CancelEventArgs args)
        {
            if (IsShutdown) return;
            args.Cancel = true;
            Shutdown();
        }
    }
}