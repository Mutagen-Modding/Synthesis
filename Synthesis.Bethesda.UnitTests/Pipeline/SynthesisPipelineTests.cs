using System.IO.Abstractions;
using FluentAssertions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Fallout4;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Synthesis.CLI;
using Newtonsoft.Json;
using Noggog;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Pipeline;

public class SynthesisPipelineTests
{
    #region Run Patch
    
    private void SetupRun(
        IFileSystem fileSystem,
        RunSynthesisMutagenPatcher args,
        ModKey modKey,
        FilePath sourcePath,
        DirectoryPath outputPath,
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> gameEnvironment)
    {
        args.ModKey = modKey.FileName;
        args.DataFolderPath = gameEnvironment.DataFolderPath;
        args.LoadOrderFilePath = gameEnvironment.LoadOrderFilePath;
        args.GameRelease = gameEnvironment.GameRelease;
        args.SourcePath = sourcePath;
        args.OutputPath = outputPath;

        var mod = new SkyrimMod(modKey, SkyrimRelease.SkyrimSE);
        mod.WriteToBinary(sourcePath, fileSystem: fileSystem, param: new BinaryWriteParameters()
        {
            ModKey = ModKeyOption.NoCheck
        });
    }
    
    [Theory]
    [SynthAutoData]
    public async Task AddPatch(
        IFileSystem fileSystem,
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> gameEnvironment,
        RunSynthesisMutagenPatcher runSynthesisMutagenPatcher,
        FilePath sourcePath,
        DirectoryPath outputPath,
        DirectoryPath extraSettingsFolder,
        ModKey outputModKey)
    {
        SetupRun(fileSystem, runSynthesisMutagenPatcher, outputModKey, sourcePath, outputPath, gameEnvironment);
        runSynthesisMutagenPatcher.ExtraDataFolder = extraSettingsFolder;
        
        int count = 0;
        await SynthesisPipeline.Instance
            .AddPatch<ISkyrimMod, ISkyrimModGetter>((state) =>
            {
                count++;
            })
            .Run(runSynthesisMutagenPatcher, fileSystem);
        count.Should().Be(1);
    }
    
    [Theory]
    [SynthAutoData]
    public async Task PatchStateBasicProperties(
        IFileSystem fileSystem,
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> gameEnvironment,
        RunSynthesisMutagenPatcher runSynthesisMutagenPatcher,
        FilePath sourcePath,
        DirectoryPath outputPath,
        DirectoryPath extraSettingsFolder,
        ModKey outputModKey)
    {
        SetupRun(fileSystem, runSynthesisMutagenPatcher, outputModKey, sourcePath, outputPath, gameEnvironment);
        runSynthesisMutagenPatcher.ExtraDataFolder = extraSettingsFolder;
        
        await SynthesisPipeline.Instance
            .AddPatch<ISkyrimMod, ISkyrimModGetter>((state) =>
            {
                state.GameRelease.Should().Be(gameEnvironment.GameRelease);
                state.OutputPath.Should().Be(runSynthesisMutagenPatcher.OutputPath);
                state.SourcePath.Should().Be(sourcePath);
                state.DataFolderPath.Should().Be(gameEnvironment.DataFolderPath);
                state.LoadOrderFilePath.Should().Be(gameEnvironment.LoadOrderFilePath);
                state.ExtraSettingsDataPath.Should().Be(runSynthesisMutagenPatcher.ExtraDataFolder);
                state.PatchMod.Should().NotBeNull();
                state.PatchMod.ModKey.Should().Be(outputModKey);
            })
            .Run(runSynthesisMutagenPatcher, fileSystem);
    }
    
    [Theory]
    [SynthAutoData]
    public async Task PatchStateLoadOrder(
        IFileSystem fileSystem,
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> gameEnvironment,
        RunSynthesisMutagenPatcher runSynthesisMutagenPatcher,
        FilePath sourcePath,
        DirectoryPath outputPath,
        DirectoryPath extraSettingsFolder,
        ModKey outputModKey)
    {
        SetupRun(fileSystem, runSynthesisMutagenPatcher, outputModKey, sourcePath, outputPath, gameEnvironment);
        runSynthesisMutagenPatcher.ExtraDataFolder = extraSettingsFolder;
        
        await SynthesisPipeline.Instance
            .AddPatch<ISkyrimMod, ISkyrimModGetter>((state) =>
            {
                state.LoadOrder.ListedOrder.Select(x => x.ModKey).Should().Equal(
                    gameEnvironment.LoadOrder.ListedOrder
                        .Where(x => x.Enabled)
                        .Select(x => x.ModKey)
                        .And(outputModKey));
                state.LoadOrder.ListedOrder.Select(x => x.Mod).Should().AllSatisfy(x => x.Should().NotBeNull());
                state.LinkCache.ListedOrder.Select(x => x.ModKey).Should().Equal(
                    gameEnvironment.LoadOrder.ListedOrder
                        .Where(x => x.Enabled)
                        .Select(x => x.ModKey)
                        .And(outputModKey));
            })
            .Run(runSynthesisMutagenPatcher, fileSystem);
    }
    
    [Theory]
    [SynthAutoData]
    public async Task MultiplePatches(
        IFileSystem fileSystem,
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> gameEnvironment,
        RunSynthesisMutagenPatcher runSynthesisMutagenPatcher,
        FilePath sourcePath,
        DirectoryPath outputPath,
        ModKey outputModKey)
    {
        SetupRun(fileSystem, runSynthesisMutagenPatcher, outputModKey, sourcePath, outputPath, gameEnvironment);
        
        int skyrimCount = 0;
        int fo4Count = 0;
        await SynthesisPipeline.Instance
            .AddPatch<ISkyrimMod, ISkyrimModGetter>((state) => skyrimCount++)
            .AddPatch<IFallout4Mod, IFallout4ModGetter>((state) => fo4Count++)
            .Run(runSynthesisMutagenPatcher, fileSystem);
        skyrimCount.Should().Be(1);
        fo4Count.Should().Be(0);
    }
    
    [Theory]
    [SynthAutoData]
    public async Task NoPatches(
        IFileSystem fileSystem,
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> gameEnvironment,
        RunSynthesisMutagenPatcher runSynthesisMutagenPatcher,
        FilePath sourcePath,
        DirectoryPath outputPath,
        ModKey outputModKey)
    {
        SetupRun(fileSystem, runSynthesisMutagenPatcher, outputModKey, sourcePath, outputPath, gameEnvironment);
        
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await SynthesisPipeline.Instance
                .Run(runSynthesisMutagenPatcher, fileSystem);
        });
    }
    
    [Theory]
    [SynthAutoData]
    public async Task DuplicateAddPatchesThrows(
        IFileSystem fileSystem,
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> gameEnvironment,
        RunSynthesisMutagenPatcher runSynthesisMutagenPatcher,
        FilePath sourcePath,
        DirectoryPath outputPath,
        ModKey outputModKey)
    {
        SetupRun(fileSystem, runSynthesisMutagenPatcher, outputModKey, sourcePath, outputPath, gameEnvironment);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>((state) => { })
                .AddPatch<ISkyrimMod, ISkyrimModGetter>((state) => { })
                .Run(runSynthesisMutagenPatcher, fileSystem);
        });
    }
    
    [Theory]
    [SynthAutoData]
    public async Task RunPatchRunsShutdown(
        IFileSystem fileSystem,
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> gameEnvironment,
        RunSynthesisMutagenPatcher runSynthesisMutagenPatcher,
        FilePath sourcePath,
        DirectoryPath outputPath,
        ModKey outputModKey)
    {
        SetupRun(fileSystem, runSynthesisMutagenPatcher, outputModKey, sourcePath, outputPath, gameEnvironment);
        
        int callCount = 0;
        await SynthesisPipeline.Instance
            .AddPatch<ISkyrimMod, ISkyrimModGetter>((state) => { })
            .SetOnShutdown(i =>
            {
                callCount++;
            })
            .Run(runSynthesisMutagenPatcher, fileSystem);
        callCount.Should().Be(1);
    }
    
    [Theory]
    [SynthAutoData]
    public async Task RunPatchRunsShutdownIfThrows(
        IFileSystem fileSystem,
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> gameEnvironment,
        RunSynthesisMutagenPatcher runSynthesisMutagenPatcher,
        FilePath sourcePath,
        DirectoryPath outputPath,
        ModKey outputModKey)
    {
        SetupRun(fileSystem, runSynthesisMutagenPatcher, outputModKey, sourcePath, outputPath, gameEnvironment);
        
        int callCount = 0;
        await Assert.ThrowsAsync<NotImplementedException>(async () =>
        {
            await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>((state) =>
                {
                    throw new NotImplementedException();
                })
                .SetOnShutdown(i =>
                {
                    callCount++;
                })
                .Run(runSynthesisMutagenPatcher, fileSystem);
        });
        callCount.Should().Be(1);
    }

    #endregion

    #region Runnability
    
    private void SetupRun(
        IFileSystem fileSystem,
        CheckRunnability args,
        ModKey modKey,
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> gameEnvironment)
    {
        args.ModKey = modKey.FileName;
        args.DataFolderPath = gameEnvironment.DataFolderPath;
        args.LoadOrderFilePath = gameEnvironment.LoadOrderFilePath;
        args.GameRelease = gameEnvironment.GameRelease;
    }
    
    [Theory]
    [SynthAutoData]
    public async Task RunnabilityCheck(
        IFileSystem fileSystem,
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> gameEnvironment,
        CheckRunnability checkRunnability,
        ModKey outputModKey)
    {
        SetupRun(fileSystem, checkRunnability, outputModKey, gameEnvironment);
        
        int callCount = 0;
        var code = await SynthesisPipeline.Instance
            .AddPatch<ISkyrimMod, ISkyrimModGetter>((state) => { })
            .AddRunnabilityCheck(state =>
            {
                callCount++;
            })
            .Run(checkRunnability, fileSystem);
        callCount.Should().Be(1);
        code.Should().Be(0);
    }
    
    [Theory]
    [SynthAutoData]
    public async Task RunnabilityCheckNoPatch(
        IFileSystem fileSystem,
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> gameEnvironment,
        CheckRunnability checkRunnability,
        ModKey outputModKey)
    {
        SetupRun(fileSystem, checkRunnability, outputModKey, gameEnvironment);
        
        int callCount = 0;
        var code = (Codes)await SynthesisPipeline.Instance
            .AddRunnabilityCheck(state =>
            {
                callCount++;
            })
            .Run(checkRunnability, fileSystem);
        callCount.Should().Be(0);
        code.Should().Be(Codes.NotRunnable);
    }
    
    [Theory]
    [SynthAutoData]
    public async Task RunnabilityCheckBasicProperties(
        IFileSystem fileSystem,
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> gameEnvironment,
        CheckRunnability checkRunnability,
        ModKey outputModKey)
    {
        SetupRun(fileSystem, checkRunnability, outputModKey, gameEnvironment);
        
        await SynthesisPipeline.Instance
            .AddRunnabilityCheck((state) =>
            {
                state.GameRelease.Should().Be(gameEnvironment.GameRelease);
                state.DataFolderPath.Should().Be(gameEnvironment.DataFolderPath);
                state.LoadOrderFilePath.Should().Be(gameEnvironment.LoadOrderFilePath);
                state.ExtraSettingsDataPath.Should().Be(checkRunnability.ExtraDataFolder);
            })
            .Run(checkRunnability, fileSystem);
    }
    
    [Theory]
    [SynthAutoData]
    public async Task RunnabilityCheckLoadOrder(
        IFileSystem fileSystem,
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> gameEnvironment,
        CheckRunnability checkRunnability,
        ModKey outputModKey)
    {
        SetupRun(fileSystem, checkRunnability, outputModKey, gameEnvironment);
        
        await SynthesisPipeline.Instance
            .AddRunnabilityCheck((state) =>
            {
                state.LoadOrder.ListedOrder.Select(x => x.ModKey).Should().Equal(
                    gameEnvironment.LoadOrder.ListedOrder
                        .Where(x => x.Enabled)
                        .Select(x => x.ModKey)
                        .And(outputModKey));
            })
            .Run(checkRunnability, fileSystem);
    }
    
    [Theory]
    [SynthAutoData]
    public async Task RunnabilityGetEnvironment(
        IFileSystem fileSystem,
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> gameEnvironment,
        CheckRunnability checkRunnability,
        ModKey outputModKey)
    {
        SetupRun(fileSystem, checkRunnability, outputModKey, gameEnvironment);
        
        await SynthesisPipeline.Instance
            .AddRunnabilityCheck((state) =>
            {
                using var env = state.GetEnvironmentState<ISkyrimMod, ISkyrimModGetter>();
                env.GameRelease.Should().Be(gameEnvironment.GameRelease);
                env.DataFolderPath.Should().Be(gameEnvironment.DataFolderPath);
                env.LoadOrderFilePath.Should().Be(gameEnvironment.LoadOrderFilePath);
                env.CreationClubListingsFilePath.Should().Be(gameEnvironment.CreationClubListingsFilePath);
                env.LoadOrder.ListedOrder.Select(x => x.ModKey).Should().Equal(
                    gameEnvironment.LoadOrder.ListedOrder
                        .Where(x => x.Enabled)
                        .Select(x => x.ModKey)
                        .And(outputModKey));
                env.LoadOrder.ListedOrder.Select(x => x.Mod).Should().AllSatisfy(x => x.Should().NotBeNull());
                env.LinkCache.ListedOrder.Select(x => x.ModKey).Should().Equal(
                    gameEnvironment.LoadOrder.ListedOrder
                        .Where(x => x.Enabled)
                        .Select(x => x.ModKey)
                        .And(outputModKey));
            })
            .Run(checkRunnability, fileSystem);
    }

    #endregion
    
    #region Autogen Settings

    class SettingDto
    {
        public const int Default = 5;
        public const int Set = 6;
        
        public int MyInt { get; set; } = Default;
    }

    private void AssertSettingsPresent(Lazy<SettingDto> settings)
    {
        settings.Value.MyInt.Should().Be(SettingDto.Set);
    }

    private void AssertSettingsMissing(Lazy<SettingDto> settings)
    {
        settings.Value.MyInt.Should().Be(SettingDto.Default);
    }

    private void AssertSettingsThrows(Lazy<SettingDto> settings)
    {
        Assert.Throws<FileNotFoundException>(() =>
        {
            var setting = settings.Value;
        });
    }

    private void WriteSettings(IFileSystem fs, string extraDataFolder)
    {
        fs.File.WriteAllText(
            Path.Combine(extraDataFolder, "Settings.json"),
            JsonConvert.SerializeObject(new SettingDto() { MyInt = SettingDto.Set }));
    }
    
    [Theory]
    [SynthAutoData]
    public async Task AutogeneratedSettingPatcherRun(
        IFileSystem fileSystem,
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> gameEnvironment,
        RunSynthesisMutagenPatcher runSynthesisMutagenPatcher,
        DirectoryPath existingExtraSettingsFolder,
        FilePath sourcePath,
        DirectoryPath outputPath,
        ModKey outputModKey)
    {
        SetupRun(fileSystem, runSynthesisMutagenPatcher, outputModKey, sourcePath, outputPath, gameEnvironment);
        runSynthesisMutagenPatcher.ExtraDataFolder = existingExtraSettingsFolder;
        WriteSettings(fileSystem, existingExtraSettingsFolder);
        
        await SynthesisPipeline.Instance
            .SetAutogeneratedSettings<SettingDto>("Nickname", "Settings.json", out var setting)
            .SetAutogeneratedSettings<SettingDto>("Nickname2", "Settings2.json", out var setting2)
            .SetAutogeneratedSettings<SettingDto>("Nickname3", "Settings3.json", out var setting3, throwIfSettingsMissing: true)
            .AddPatch<ISkyrimMod, ISkyrimModGetter>(state =>
            {
                AssertSettingsPresent(setting);
                AssertSettingsMissing(setting2);
                AssertSettingsThrows(setting3);
            })
            .Run(runSynthesisMutagenPatcher, fileSystem);
    }
    
    [Theory]
    [SynthAutoData]
    public async Task AutogeneratedSettingRunnability(
        IFileSystem fileSystem,
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> gameEnvironment,
        CheckRunnability checkRunnability,
        DirectoryPath existingExtraSettingsFolder,
        ModKey outputModKey)
    {
        SetupRun(fileSystem, checkRunnability, outputModKey, gameEnvironment);
        checkRunnability.ExtraDataFolder = existingExtraSettingsFolder;
        WriteSettings(fileSystem, existingExtraSettingsFolder);
        
        await SynthesisPipeline.Instance
            .SetAutogeneratedSettings<SettingDto>("Nickname", "Settings.json", out var setting)
            .SetAutogeneratedSettings<SettingDto>("Nickname2", "Settings2.json", out var setting2)
            .SetAutogeneratedSettings<SettingDto>("Nickname3", "Settings3.json", out var setting3, throwIfSettingsMissing: true)
            .AddPatch<ISkyrimMod, ISkyrimModGetter>(state =>
            {
                AssertSettingsPresent(setting);
                AssertSettingsMissing(setting2);
                AssertSettingsThrows(setting3);
            })
            .Run(checkRunnability, fileSystem);
    }
    
    #endregion

    #region TypicalOpen
    
    [Fact]
    public async Task TypicalOpen()
    {
        int count = 0;
        var ret = await SynthesisPipeline.Instance
            .SetTypicalOpen((r) =>
            {
                count++;
                return 1753;
            })
            .Run(Array.Empty<string>());
        ret.Should().Be(1753);
        count.Should().Be(1);
    }
    
    [Theory]
    [SynthAutoData]
    public async Task PatchWithTypicalOpenButNoPatches(
        ModKey modKey)
    {
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await SynthesisPipeline.Instance
                .SetTypicalOpen(GameRelease.SkyrimSE, modKey)
                .Run(Array.Empty<string>());
        });
    }
    
    [Theory]
    [SynthAutoData]
    public async Task PatchWithTypicalOpen(
        ModKey modKey)
    {
        int count = 0;
        await SynthesisPipeline.Instance
            .AddPatch<ISkyrimMod, ISkyrimModGetter>((state) =>
            {
                count++;
            })
            .SetTypicalOpen(GameRelease.SkyrimSE, modKey)
            .Run(Array.Empty<string>());
        count.Should().Be(1);
    }

    #endregion

    #region OpenForSettings
    
    [Theory]
    [SynthAutoData]
    public async Task OpenForSettings(
        OpenForSettings run)
    {
        int count = 0;
        var ret = await SynthesisPipeline.Instance
            .SetOpenForSettings(state =>
            {
                count++;
                return 1753;
            })
            .Run(run);
        ret.Should().Be(1753);
        count.Should().Be(1);
    }

    #endregion
}