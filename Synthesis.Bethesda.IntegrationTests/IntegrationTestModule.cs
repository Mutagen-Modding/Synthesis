using System.IO.Abstractions;
using Autofac;
using Noggog.Autofac;
using Noggog.IO;
using Serilog;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.IntegrationTests.Components;
using Synthesis.Bethesda.IntegrationTests.Infrastructure;

namespace Synthesis.Bethesda.IntegrationTests;

public class IntegrationTestModule : Module
{
    private readonly IntegrationTest _testBase;
    private readonly PipelineMode _pipelineMode;

    public IntegrationTestModule(IntegrationTest testBase, PipelineMode pipelineMode)
    {
        _testBase = testBase;
        _pipelineMode = pipelineMode;
    }

    protected override void Load(ContainerBuilder builder)
    {
        // Register the appropriate module based on pipeline mode
        switch (_pipelineMode)
        {
            case PipelineMode.UI:
                builder.RegisterModule<Synthesis.Bethesda.GUI.Modules.MainModule>();
                break;
            case PipelineMode.CLI:
                var command = new RunPatcherPipelineCommand
                {
                    OutputDirectory = _testBase.DataFolder,
                    DataFolderPath = _testBase.DataFolder,
                    LoadOrderFilePath = _testBase.PluginsPath,
                    PipelineSettingsPath = Path.Combine(_testBase.TestFolder, "PipelineSettings.json"),
                    ExtraDataFolder = _testBase.TestFolder
                };
                builder.RegisterModule(new RunPipelineCliModule(new FileSystem(), command));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_pipelineMode), _pipelineMode, "Unknown pipeline mode");
        }

        // Override the logger registration from MainModule to write to test output
        // and capture log events for assertion
        var logConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.TestOutput(_testBase.Output)
            .WriteTo.Sink(_testBase.LogSink);

        var testLogger = logConfig.CreateLogger();

        builder.RegisterInstance(testLogger)
            .As<ILogger>()
            .SingleInstance();
        builder.RegisterAssemblyTypes(typeof(WatchSingleAppArgumentsStub).Assembly)
            .InNamespacesOf(
                typeof(WatchSingleAppArgumentsStub))
            .TypicalRegistrations()
            .AsImplementedInterfaces()
            .SingleInstance();

        // Override IEnvironmentTemporaryDirectoryProvider to use test temp folder
        var testTempDir = Path.Combine(_testBase.TestFolder, "Temp");
        Directory.CreateDirectory(testTempDir);
        builder.RegisterInstance(new EnvironmentTemporaryDirectoryInjection
                { Path = testTempDir })
            .AsImplementedInterfaces()
            .SingleInstance();

        // Override ICurrentDirectoryProvider to point to test folder
        builder
            .RegisterInstance(new CurrentDirectoryInjection(_testBase.TestFolder))
            .AsImplementedInterfaces()
            .SingleInstance();

        // Override IPluginListingsPathContext to point to test plugins.txt
        builder
            .RegisterInstance(new Components.PluginListingsPathInjection(_testBase.PluginsPath))
            .AsImplementedInterfaces()
            .SingleInstance();

        // Override ICreationClubListingsPathProvider with stub (no creation club content in tests)
        builder
            .RegisterInstance(new CreationClubListingsPathStub())
            .AsImplementedInterfaces()
            .SingleInstance();

        // Override IExecutionParameters to disable shared Roslyn compilation server,
        // preventing file locking issues between test runs on CI
        builder.RegisterType<TestExecutionParameters>()
            .As<Synthesis.Bethesda.Execution.DotNet.IExecutionParameters>()
            .SingleInstance();
    }
}