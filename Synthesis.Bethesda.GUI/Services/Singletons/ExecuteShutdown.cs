﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Noggog.Utility;
using Serilog;
using Synthesis.Bethesda.Execution;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.GUI.Settings;
    
#if !DEBUG
using System.Diagnostics;
using Noggog.Utility;
#endif

namespace Synthesis.Bethesda.GUI.Services.Singletons
{
    public interface IExecuteShutdown
    {
        bool IsShutdown { get; }
        void Closing(CancelEventArgs args);
    }

    public class ExecuteShutdown : IExecuteShutdown
    {
        private readonly ILogger _Logger;
        private readonly IInitilize _Init;
        private readonly IPipelineSettingsPath _PipelineSettingsPath;
        private readonly IGuiSettingsPath _GuiPaths;
        private readonly IProcessFactory _ProcessFactory;
        private readonly IRetrieveSaveSettings _Save;

        public ExecuteShutdown(
            ILogger logger,
            IInitilize init,
            IPipelineSettingsPath paths,
            IGuiSettingsPath guiPaths,
            IProcessFactory processFactory,
            IRetrieveSaveSettings save)
        {
            _Logger = logger;
            _Init = init;
            _PipelineSettingsPath = paths;
            _GuiPaths = guiPaths;
            _ProcessFactory = processFactory;
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
                    _Logger.Information("Disposing injection");
                    Inject.Container.Dispose();
                    _Logger.Information("Disposed injection");
                }
                catch (Exception e)
                {
                    _Logger.Error("Error shutting down injector actions", e);
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