using System;
using System.Diagnostics.CodeAnalysis;
using Mutagen.Bethesda.Strings;
using Mutagen.Bethesda.Synthesis.CLI;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Patchers.Common;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Cli
{
    public interface IGenericSettingsToMutagenSettings
    {
        RunSynthesisMutagenPatcher Convert(RunSynthesisPatcher settings);
    }

    public class GenericSettingsToMutagenSettings : IGenericSettingsToMutagenSettings
    {
        public IPatcherExtraDataPathProvider ExtraDataPathProvider { get; }

        [ExcludeFromCodeCoverage]
        public GenericSettingsToMutagenSettings(
            IPatcherExtraDataPathProvider extraDataPathProvider)
        {
            ExtraDataPathProvider = extraDataPathProvider;
        }
        
        public RunSynthesisMutagenPatcher Convert(RunSynthesisPatcher settings)
        {
            return new RunSynthesisMutagenPatcher()
            {
                DataFolderPath = settings.DataFolderPath,
                GameRelease = settings.GameRelease,
                LoadOrderFilePath = settings.LoadOrderFilePath,
                OutputPath = settings.OutputPath,
                SourcePath = settings.SourcePath,
                PersistencePath = settings.PersistencePath,
                PatcherName = settings.PatcherName,
                ExtraDataFolder = ExtraDataPathProvider.Path,
                Localize = settings.Localize,
                TargetLanguage = Enum.Parse<Language>(settings.TargetLanguage),
                ModKey = settings.ModKey,
            };
        }
    }
}