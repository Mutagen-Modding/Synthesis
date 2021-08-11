using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Autofac;
using Newtonsoft.Json;
using Serilog;
using Synthesis.Bethesda.Execution.Json;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.GUI.Json;
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
        private readonly ILifetimeScope _scope;
        private readonly ILogger _logger;
        private readonly IStartupTracker _init;
        private readonly IPipelineSettingsPath _pipelineSettingsPath;
        private readonly IGuiSettingsPath _guiPaths;
        private readonly IRetrieveSaveSettings _save;
        private readonly IGuiSettingsExporter _guiSettingsExporter;
        private readonly IPipelineSettingsExporter _pipelineSettingsExporter;
        private readonly IMainWindow _window;
        
        public bool IsShutdown { get; private set; }

        public Shutdown(
            ILifetimeScope scope,
            ILogger logger,
            IStartupTracker init,
            IPipelineSettingsPath paths,
            IGuiSettingsPath guiPaths,
            IRetrieveSaveSettings save,
            IGuiSettingsExporter guiSettingsExporter,
            IPipelineSettingsExporter pipelineSettingsExporter,
            IMainWindow window)
        {
            _scope = scope;
            _logger = logger;
            _init = init;
            _pipelineSettingsPath = paths;
            _guiPaths = guiPaths;
            _save = save;
            _guiSettingsExporter = guiSettingsExporter;
            _pipelineSettingsExporter = pipelineSettingsExporter;
            _window = window;
        }

        public void Prepare()
        {
            _window.Closing += (_, b) =>
            {
                _window.Visibility = Visibility.Collapsed;
                Closing(b);
            };
        }
        
        private async void ExecuteShutdown()
        {
            IsShutdown = true;

            await Task.Run(() =>
            {
                if (!_init.Initialized)
                {
                    _logger.Information("App was unable to start up.  Not saving settings.");
                    return;
                }

                try
                {
                    _save.Retrieve(out var gui, out var pipe);
                    _pipelineSettingsExporter.Write(_pipelineSettingsPath.Path, pipe);
                    _guiSettingsExporter.Write(_guiPaths.Path, gui);
                }
                catch (Exception e)
                {
                    _logger.Error("Error saving settings", e);
                }
            });
            
            var toDo = new List<Task>();
#if !DEBUG
            toDo.Add(Task.Run(async () =>
            {
                try
                {
                    using var process = _ProcessFactory.Create(
                        new ProcessStartInfo(_dotNetCommandPathProvider.Path, $"build-server shutdown"));
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
                    _logger.Information("Disposing container");
                    _scope.Dispose();
                }
                catch (Exception e)
                {
                    _logger.Error("Error shutting down container actions", e);
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