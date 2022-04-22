using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Serilog;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Patchers.Cli;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Cli;

public interface ICliPatcherRun : IPatcherRun
{
}

public class CliPatcherRun : ICliPatcherRun
{
    private readonly CompositeDisposable _disposable = new();

    public Guid Key { get; }
    public int Index { get; }
    public string Name => _name.Name;
    public IProcessRunner ProcessRunner { get; }
    public IPathToExecutableInputProvider ExePath { get; }

    public ILogger Logger { get; }
    private readonly IPatcherNameProvider _name;
    public IGenericSettingsToMutagenSettings GenericToMutagenSettings { get; }
    public IFormatCommandLine Format { get; }

    [ExcludeFromCodeCoverage]
    public CliPatcherRun(
        ILogger logger,
        IProcessRunner processRunner,
        IPatcherIdProvider idProvider,
        IPatcherNameProvider name,
        IPathToExecutableInputProvider exePath,
        IIndexDisseminator indexDisseminator,
        IGenericSettingsToMutagenSettings genericToMutagenSettings,
        IFormatCommandLine format)
    {
        Key = idProvider.InternalId;
        ProcessRunner = processRunner;
        ExePath = exePath;
        Logger = logger;
        _name = name;
        GenericToMutagenSettings = genericToMutagenSettings;
        Format = format;
        Index = indexDisseminator.GetNext();
    }

    public void Dispose()
    {
        _disposable.Dispose();
    }

    public void Add(IDisposable disposable)
    {
        _disposable.Add(disposable);
    }

    [ExcludeFromCodeCoverage]
    public async Task Prep(CancellationToken cancel)
    {
    }

    public async Task Run(RunSynthesisPatcher settings, CancellationToken cancel)
    {
        if (cancel.IsCancellationRequested) return;

        var internalSettings = GenericToMutagenSettings.Convert(settings);
        var args = Format.Format(internalSettings);
            
        try
        {
            var result = await ProcessRunner.Run(
                new ProcessStartInfo(ExePath.Path, args)
                {
                    WorkingDirectory = ExePath.Path.Directory!
                },
                cancel).ConfigureAwait(false);
            if (result != 0)
            {
                throw new CliUnsuccessfulRunException(
                    result,
                    $"Process exited in failure: {ExePath.Path} {internalSettings}");
            }
        }
        catch (Win32Exception ex)
        {
            throw new FileNotFoundException($"Could not find target CLI file: {ExePath.Path}", ex);
        }
    }
}