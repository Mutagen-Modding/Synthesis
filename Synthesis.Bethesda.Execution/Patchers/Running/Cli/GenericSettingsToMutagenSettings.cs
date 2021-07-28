using Mutagen.Bethesda.Synthesis.CLI;
using Synthesis.Bethesda.Execution.Patchers.TopLevel;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Cli
{
    public interface IGenericSettingsToMutagenSettings
    {
        RunSynthesisMutagenPatcher Convert(RunSynthesisPatcher settings);
    }

    public class GenericSettingsToMutagenSettings : IGenericSettingsToMutagenSettings
    {
        public IPatcherExtraDataPathProvider ExtraDataPathProvider { get; }

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
                ExtraDataFolder = ExtraDataPathProvider.Path
            };
        }
    }
}