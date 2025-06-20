using System.IO.Abstractions;
using Mutagen.Bethesda;
using Shouldly;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Fallout4;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Synthesis.CLI;
using Newtonsoft.Json;
using Noggog;
using Noggog.Testing.Extensions;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.UnitTests.AutoData;
using Synthesis.Bethesda.UnitTests.Common;

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
        mod.BeginWrite
            .ToPath(sourcePath)
            .WithNoLoadOrder()
            .WithFileSystem(fileSystem)
            .NoModKeySync()
            .Write();
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
        count.ShouldBe(1);
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
                state.GameRelease.ShouldBe(gameEnvironment.GameRelease);
                state.OutputPath.Path.ShouldBe(runSynthesisMutagenPatcher.OutputPath);
                state.SourcePath.ShouldBe(sourcePath);
                state.DataFolderPath.ShouldBe(gameEnvironment.DataFolderPath);
                state.LoadOrderFilePath.ShouldBe(gameEnvironment.LoadOrderFilePath);
                state.ExtraSettingsDataPath!.Value.Path.ShouldBe(runSynthesisMutagenPatcher.ExtraDataFolder);
                state.PatchMod.ShouldNotBeNull();
                state.PatchMod.ModKey.ShouldBe(outputModKey);
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
                state.LoadOrder.ListedOrder.Select(x => x.ModKey).ShouldEqualEnumerable(
                    gameEnvironment.LoadOrder.ListedOrder
                        .Where(x => x.Enabled)
                        .Select(x => x.ModKey)
                        .And(outputModKey));
                state.LoadOrder.ListedOrder.Select(x => x.Mod).ShouldSatisfyAllConditions(x => x.ShouldNotBeNull());
                state.LinkCache.ListedOrder.Select(x => x.ModKey).ShouldEqualEnumerable(
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
        skyrimCount.ShouldBe(1);
        fo4Count.ShouldBe(0);
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
        callCount.ShouldBe(1);
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
        callCount.ShouldBe(1);
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
        callCount.ShouldBe(1);
        code.ShouldBe(0);
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
        callCount.ShouldBe(0);
        code.ShouldBe(Codes.NotRunnable);
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
                state.GameRelease.ShouldBe(gameEnvironment.GameRelease);
                state.DataFolderPath.ShouldBe(gameEnvironment.DataFolderPath);
                state.LoadOrderFilePath.ShouldBe(gameEnvironment.LoadOrderFilePath);
                state.ExtraSettingsDataPath.ShouldBe<DirectoryPath?>(checkRunnability.ExtraDataFolder);
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
                state.LoadOrder.ListedOrder.Select(x => x.ModKey).ShouldEqual(
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
                env.GameRelease.ShouldBe(gameEnvironment.GameRelease);
                env.DataFolderPath.ShouldBe(gameEnvironment.DataFolderPath);
                env.LoadOrderFilePath.ShouldBe(gameEnvironment.LoadOrderFilePath);
                env.CreationClubListingsFilePath.ShouldBe(gameEnvironment.CreationClubListingsFilePath);
                env.LoadOrder.ListedOrder.Select(x => x.ModKey).ShouldEqual(
                    gameEnvironment.LoadOrder.ListedOrder
                        .Where(x => x.Enabled)
                        .Select(x => x.ModKey)
                        .And(outputModKey));
                env.LoadOrder.ListedOrder.Select(x => x.Mod).ShouldSatisfyAllConditions(x => x.ShouldNotBeNull());
                env.LinkCache.ListedOrder.Select(x => x.ModKey).ShouldEqual(
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
        settings.Value.MyInt.ShouldBe(SettingDto.Set);
    }

    private void AssertSettingsMissing(Lazy<SettingDto> settings)
    {
        settings.Value.MyInt.ShouldBe(SettingDto.Default);
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
    
    // Needs more test work to be able to mimic a game environment when none exists
    
    // [Theory]
    // [SynthAutoData]
    // public async Task TypicalOpen(
    //     IFileSystem fileSystem,
    //     IGameEnvironment<ISkyrimMod, ISkyrimModGetter> environment)
    // {
    //     int count = 0;
    //     var ret = await SynthesisPipeline.Instance
    //         .SetTypicalOpen(() =>
    //         {
    //             count++;
    //             return 1753;
    //         })
    //         .Run(Array.Empty<string>(), fileSystem);
    //     ret.ShouldBe(1753);
    //     count.ShouldBe(1);
    // }
    //
    // [Theory]
    // [SynthAutoData]
    // public async Task PatchWithTypicalOpenButNoPatches(
    //     IFileSystem fileSystem,
    //     IGameEnvironment<ISkyrimMod, ISkyrimModGetter> environment,
    //     ModKey modKey)
    // {
    //     await Assert.ThrowsAsync<ArgumentException>(async () =>
    //     {
    //         await SynthesisPipeline.Instance
    //             .SetTypicalOpen(GameRelease.SkyrimSE, modKey)
    //             .Run(Array.Empty<string>(), fileSystem: fileSystem);
    //     });
    // }
    //
    // [Theory]
    // [SynthAutoData]
    // public async Task PatchWithTypicalOpen(
    //     IFileSystem fileSystem,
    //     IGameEnvironment<ISkyrimMod, ISkyrimModGetter> environment,
    //     ModKey modKey)
    // {
    //     int count = 0;
    //     await SynthesisPipeline.Instance
    //         .AddPatch<ISkyrimMod, ISkyrimModGetter>((state) =>
    //         {
    //             count++;
    //         })
    //         .SetTypicalOpen(GameRelease.SkyrimSE, modKey)
    //         .Run(Array.Empty<string>(), fileSystem: fileSystem);
    //     count.ShouldBe(1);
    // }

    #endregion

    #region OpenForSettings
    
    private void SetupRun(
        IFileSystem fileSystem,
        OpenForSettings args,
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
    public async Task OpenForSettings(
        IFileSystem fileSystem,
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> gameEnvironment,
        ModKey outputModKey,
        OpenForSettings run)
    {
        SetupRun(fileSystem, run, outputModKey, gameEnvironment);
        
        int count = 0;
        var ret = await SynthesisPipeline.Instance
            .SetOpenForSettings(state =>
            {
                count++;
                return 1753;
            })
            .Run(run, fileSystem);
        ret.ShouldBe(1753);
        count.ShouldBe(1);
    }
    
    [Theory]
    [SynthAutoData]
    public async Task OpenForSettingsBasicProperties(
        IFileSystem fileSystem,
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> gameEnvironment,
        ModKey outputModKey,
        DirectoryPath extraSettingsFolder,
        OpenForSettings run)
    {
        SetupRun(fileSystem, run, outputModKey, gameEnvironment);
        run.ExtraDataFolder = extraSettingsFolder;
        
        var ret = await SynthesisPipeline.Instance
            .SetOpenForSettings(state =>
            {
                state.GameRelease.ShouldBe(gameEnvironment.GameRelease);
                state.DataFolderPath.ShouldBe(gameEnvironment.DataFolderPath);
                state.LoadOrderFilePath.ShouldBe(gameEnvironment.LoadOrderFilePath);
                state.ExtraSettingsDataPath!.Value.Path.ShouldBe(run.ExtraDataFolder);
                return 1753;
            })
            .Run(run, fileSystem);
    }

    #endregion

    #region Misc

    
    [Fact]
    public void AddsImplicitMods()
    {
        using var tmp = Utility.GetTempFolder(nameof(SynthesisPipelineTests));

        var pluginPath = Path.Combine(tmp.Dir.Path, "Plugins.txt");
        var dataFolder = Path.Combine(tmp.Dir.Path, "Data");
        Directory.CreateDirectory(dataFolder);
        File.WriteAllText(
            Path.Combine(dataFolder, Mutagen.Bethesda.Skyrim.Constants.Skyrim.FileName),
            string.Empty);
        File.WriteAllLines(pluginPath,
            new string[]
            {
                $"*{Utility.TestModKey.FileName}",
                $"{Utility.OverrideModKey.FileName}",
            });
        var testEnv = new TestEnvironment(
            IFileSystemExt.DefaultFilesystem,
            GameRelease.SkyrimSE,
            string.Empty,
            dataFolder,
            pluginPath);
        var getStateLoadOrder = testEnv.GetStateLoadOrder();
        var listings = getStateLoadOrder.GetUnfilteredLoadOrder(false).ToList();
        listings.ShouldHaveCount(3);
        listings.ShouldEqualEnumerable(new ILoadOrderListingGetter[]
        {
            new LoadOrderListing(Mutagen.Bethesda.Skyrim.Constants.Skyrim, true),
            new LoadOrderListing(Utility.TestModKey, true),
            new LoadOrderListing(Utility.OverrideModKey, false),
        });
    }

    [Fact]
    public void GetLoadOrder_NoLoadOrderPath()
    {
        var env = Utility.SetupEnvironment(GameRelease.SkyrimSE);
        env = env with {PluginPath = string.Empty};
        var getStateLoadOrder = env.GetStateLoadOrder();
        var lo = getStateLoadOrder.GetUnfilteredLoadOrder(false);
        lo.Select(l => l.ModKey).ShouldBeEmpty();
    }

    #endregion
}