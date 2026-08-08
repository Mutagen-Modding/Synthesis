using System.Diagnostics.CodeAnalysis;
using Mutagen.Bethesda.Strings;
using Mutagen.Bethesda.Synthesis.CLI;
using Synthesis.Bethesda.Commands;

namespace Synthesis.Bethesda.Execution.Patchers.Common;

public interface IConstructBaseRunArgs
{
    RunSynthesisMutagenPatcher Construct(RunSynthesisPatcher settings);
}

public class ConstructBaseRunArgs : IConstructBaseRunArgs
{
    public IPatcherExtraDataPathProvider ExtraDataPathProvider { get; }

    [ExcludeFromCodeCoverage]
    public ConstructBaseRunArgs(
        IPatcherExtraDataPathProvider extraDataPathProvider)
    {
        ExtraDataPathProvider = extraDataPathProvider;
    }

    public RunSynthesisMutagenPatcher Construct(RunSynthesisPatcher settings)
    {
        return new RunSynthesisMutagenPatcher()
        {
            DataFolderPath = settings.DataFolderPath,
            GameRelease = settings.GameRelease,
            LoadOrderFilePath = settings.LoadOrderFilePath,
            LoadOrderIncludesCreationClub = settings.LoadOrderIncludesCreationClub,
            OutputPath = settings.OutputPath,
            SourcePath = settings.SourcePath,
            PersistencePath = settings.PersistencePath,
            PatcherName = settings.PatcherName,
            ExtraDataFolder = ExtraDataPathProvider.Path,
            Localize = settings.Localize,
            TargetLanguage = Enum.Parse<Language>(settings.TargetLanguage),
            ModKey = settings.ModKey,
            UseUtf8ForEmbeddedStrings = settings.UseUtf8ForEmbeddedStrings,
            HeaderVersionOverride = settings.HeaderVersionOverride,
            FormIDRangeMode = settings.FormIDRangeMode,
            SplitIfMaxMastersExceeded = settings.SplitIfMaxMastersExceeded,
        };
    }
}
