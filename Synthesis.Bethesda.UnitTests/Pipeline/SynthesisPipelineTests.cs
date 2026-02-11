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
        args.LoadOrderFilePath = gameEnvironment.LoadOrderFilePath!.Value;
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
                state.LoadOrderFilePath.ShouldBe(gameEnvironment.LoadOrderFilePath!.Value);
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
        args.LoadOrderFilePath = gameEnvironment.LoadOrderFilePath!.Value;
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
                state.LoadOrderFilePath.ShouldBe(gameEnvironment.LoadOrderFilePath!.Value);
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
                state.LoadOrderFilePath.ShouldBe(gameEnvironment.LoadOrderFilePath!.Value);
                state.ExtraSettingsDataPath!.Value.Path.ShouldBe(run.ExtraDataFolder);
                return 1753;
            })
            .Run(run, fileSystem);
    }

    #endregion

    #region AutoSplit

    [Theory]
    [SynthAutoData]
    public async Task AutoSplitCreatesMultipleFiles(
        IFileSystem fileSystem,
        DirectoryPath dataFolder,
        DirectoryPath outputPath,
        ModKey outputModKey)
    {
        // Create 256 master mods with NPCs to exceed the master limit
        const int masterCount = 256;
        var masterModKeys = new List<ModKey>();
        var npcFormKeys = new List<FormKey>();

        fileSystem.Directory.CreateDirectory(dataFolder);

        for (int i = 0; i < masterCount; i++)
        {
            var masterKey = new ModKey($"Master{i:D3}", ModType.Plugin);
            masterModKeys.Add(masterKey);

            var masterMod = new SkyrimMod(masterKey, SkyrimRelease.SkyrimSE);
            var npc = masterMod.Npcs.AddNew();
            npcFormKeys.Add(npc.FormKey);

            var masterPath = Path.Combine(dataFolder, masterKey.FileName);
            masterMod.BeginWrite
                .ToPath(masterPath)
                .WithNoLoadOrder()
                .WithFileSystem(fileSystem)
                .NoModKeySync()
                .Write();
        }

        // Create plugins.txt with all masters
        var pluginPath = Path.Combine(dataFolder, "Plugins.txt");
        var pluginLines = masterModKeys.Select(k => $"*{k.FileName}").ToList();
        fileSystem.File.WriteAllLines(pluginPath, pluginLines);

        // Create source mod
        var sourcePath = Path.Combine(dataFolder, "Source.esp");
        var sourceMod = new SkyrimMod(ModKey.FromFileName("Source.esp"), SkyrimRelease.SkyrimSE);
        sourceMod.BeginWrite
            .ToPath(sourcePath)
            .WithNoLoadOrder()
            .WithFileSystem(fileSystem)
            .NoModKeySync()
            .Write();

        // Setup run arguments
        var runArgs = new RunSynthesisMutagenPatcher
        {
            ModKey = outputModKey.FileName,
            DataFolderPath = dataFolder,
            LoadOrderFilePath = pluginPath,
            GameRelease = GameRelease.SkyrimSE,
            SourcePath = sourcePath,
            OutputPath = Path.Combine(outputPath, outputModKey.FileName),
            LoadOrderIncludesCreationClub = true,
            SplitIfMaxMastersExceeded = true, // Enable auto-split
        };

        fileSystem.Directory.CreateDirectory(outputPath);

        // Track expected FormLists for verification
        var expectedFormLists = new List<(string EditorID, List<FormKey> Items)>();

        // Run pipeline with patcher that creates multiple FormLists
        // Each FormList references a subset of NPCs so the splitter can distribute them
        await SynthesisPipeline.Instance
            .AddPatch<ISkyrimMod, ISkyrimModGetter>((state) =>
            {
                // Create FormLists with ~128 masters each (under the 254 limit per record)
                // This allows the splitter to distribute them across multiple output files
                const int mastersPerList = 128;
                int formListIndex = 0;

                for (int i = 0; i < npcFormKeys.Count; i += mastersPerList)
                {
                    var formList = state.PatchMod.FormLists.AddNew();
                    var editorId = $"NpcFormList_{formListIndex++}";
                    formList.EditorID = editorId;

                    var items = new List<FormKey>();
                    var endIndex = Math.Min(i + mastersPerList, npcFormKeys.Count);
                    for (int j = i; j < endIndex; j++)
                    {
                        formList.Items.Add(npcFormKeys[j]);
                        items.Add(npcFormKeys[j]);
                    }

                    expectedFormLists.Add((editorId, items));
                }
            })
            .Run(runArgs, fileSystem);

        // Verify multiple output files were created
        // First split has no suffix (base name), second split is _2
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(outputModKey.FileName);
        var extension = Path.GetExtension(outputModKey.FileName);

        var splitFile1 = Path.Combine(outputPath, $"{fileNameWithoutExtension}{extension}");
        var splitFile2 = Path.Combine(outputPath, $"{fileNameWithoutExtension}_2{extension}");

        fileSystem.File.Exists(splitFile1).ShouldBeTrue($"Expected split file 1 to exist: {splitFile1}");
        fileSystem.File.Exists(splitFile2).ShouldBeTrue($"Expected split file 2 to exist: {splitFile2}");

        // Read back the split files and verify content
        using var splitMod1 = SkyrimMod.Create(SkyrimRelease.SkyrimSE)
            .FromPath(splitFile1)
            .WithFileSystem(fileSystem)
            .Construct();
        using var splitMod2 = SkyrimMod.Create(SkyrimRelease.SkyrimSE)
            .FromPath(splitFile2)
            .WithFileSystem(fileSystem)
            .Construct();

        // Collect all FormLists from both split files
        var reimportedFormLists = new List<Mutagen.Bethesda.Skyrim.IFormListGetter>();
        reimportedFormLists.AddRange(splitMod1.FormLists);
        reimportedFormLists.AddRange(splitMod2.FormLists);

        // Verify all expected FormLists are present
        reimportedFormLists.Count.ShouldBe(expectedFormLists.Count,
            $"Expected {expectedFormLists.Count} FormLists but found {reimportedFormLists.Count}");

        // Verify each FormList has correct items
        foreach (var expected in expectedFormLists)
        {
            var reimported = reimportedFormLists.FirstOrDefault(f => f.EditorID == expected.EditorID);
            reimported.ShouldNotBeNull($"Could not find FormList with EditorID: {expected.EditorID}");
            reimported!.Items.Count.ShouldBe(expected.Items.Count,
                $"FormList {expected.EditorID} has wrong item count");

            for (int i = 0; i < expected.Items.Count; i++)
            {
                reimported.Items[i].FormKey.ShouldBe(expected.Items[i],
                    $"FormList {expected.EditorID} item {i} mismatch");
            }
        }
    }

    [Theory]
    [SynthAutoData]
    public async Task ImportsSplitSourceFiles(
        IFileSystem fileSystem,
        DirectoryPath dataFolder,
        DirectoryPath outputPath)
    {
        // Create 256 master mods with NPCs to exceed the master limit
        const int masterCount = 256;
        var masterModKeys = new List<ModKey>();
        var npcFormKeys = new List<FormKey>();

        fileSystem.Directory.CreateDirectory(dataFolder);

        for (int i = 0; i < masterCount; i++)
        {
            var masterKey = new ModKey($"Master{i:D3}", ModType.Plugin);
            masterModKeys.Add(masterKey);

            var masterMod = new SkyrimMod(masterKey, SkyrimRelease.SkyrimSE);
            var npc = masterMod.Npcs.AddNew();
            npcFormKeys.Add(npc.FormKey);

            var masterPath = Path.Combine(dataFolder, masterKey.FileName);
            masterMod.BeginWrite
                .ToPath(masterPath)
                .WithNoLoadOrder()
                .WithFileSystem(fileSystem)
                .NoModKeySync()
                .Write();
        }

        // Create split source files simulating a previous patcher's output
        // Note: When Mutagen splits a mod, records keep FormKeys pointing to the merged mod key
        var sourceModKey = ModKey.FromFileName("PreviousPatch.esp");
        var expectedFormLists = new List<(string EditorID, List<FormKey> Items)>();

        // Create first split file with first 128 masters
        // Use merged mod key for the mod, then write with NoModKeySync to preserve FormKeys
        var splitMod1 = new SkyrimMod(sourceModKey, SkyrimRelease.SkyrimSE);
        var formList1 = splitMod1.FormLists.AddNew();
        formList1.EditorID = "NpcFormList_0";
        var items1 = new List<FormKey>();
        for (int i = 0; i < 128; i++)
        {
            formList1.Items.Add(npcFormKeys[i]);
            items1.Add(npcFormKeys[i]);
        }
        expectedFormLists.Add((formList1.EditorID, items1));

        // First split file uses the base name (no suffix)
        var splitFile1Path = Path.Combine(dataFolder, sourceModKey.FileName);
        splitMod1.BeginWrite
            .ToPath(splitFile1Path)
            .WithNoLoadOrder()
            .WithFileSystem(fileSystem)
            .NoModKeySync()
            .Write();

        // Create second split file with remaining masters
        var splitMod2 = new SkyrimMod(sourceModKey, SkyrimRelease.SkyrimSE);
        // Use RecordWith to create a record with a specific FormKey to avoid conflicts
        var formList2 = new Mutagen.Bethesda.Skyrim.FormList(new FormKey(sourceModKey, 0x900), SkyrimRelease.SkyrimSE);
        formList2.EditorID = "NpcFormList_1";
        splitMod2.FormLists.RecordCache.Set(formList2);
        var items2 = new List<FormKey>();
        for (int i = 128; i < masterCount; i++)
        {
            formList2.Items.Add(npcFormKeys[i]);
            items2.Add(npcFormKeys[i]);
        }
        expectedFormLists.Add((formList2.EditorID, items2));

        // Second split file uses _2 suffix
        var splitFile2Path = Path.Combine(dataFolder, $"{sourceModKey.Name}_2{Path.GetExtension(sourceModKey.FileName)}");
        splitMod2.BeginWrite
            .ToPath(splitFile2Path)
            .WithNoLoadOrder()
            .WithFileSystem(fileSystem)
            .NoModKeySync()
            .Write();

        // Create plugins.txt with all masters AND the split files
        var pluginPath = Path.Combine(dataFolder, "Plugins.txt");
        var pluginLines = masterModKeys.Select(k => $"*{k.FileName}").ToList();
        pluginLines.Add($"*{sourceModKey.FileName}");
        pluginLines.Add($"*{sourceModKey.Name}_2{Path.GetExtension(sourceModKey.FileName)}");
        fileSystem.File.WriteAllLines(pluginPath, pluginLines);

        // Setup run arguments - SourcePath points to base name that doesn't exist
        var outputModKey = ModKey.FromFileName("Output.esp");
        var runArgs = new RunSynthesisMutagenPatcher
        {
            ModKey = outputModKey.FileName,
            DataFolderPath = dataFolder,
            LoadOrderFilePath = pluginPath,
            GameRelease = GameRelease.SkyrimSE,
            SourcePath = Path.Combine(dataFolder, sourceModKey.FileName), // Base name (first split file)
            OutputPath = Path.Combine(outputPath, outputModKey.FileName),
            LoadOrderIncludesCreationClub = true,
            SplitIfMaxMastersExceeded = true // Enable split file detection
        };

        fileSystem.Directory.CreateDirectory(outputPath);

        // Track state for verification
        ILoadOrder<IModListing<ISkyrimModGetter>>? capturedLoadOrder = null;
        ISkyrimMod? capturedPatchMod = null;

        // Run pipeline - the patcher should receive merged content from split files
        await SynthesisPipeline.Instance
            .AddPatch<ISkyrimMod, ISkyrimModGetter>((state) =>
            {
                capturedLoadOrder = state.LoadOrder;
                capturedPatchMod = state.PatchMod;

                // Verify PatchMod contains all FormLists from both split files
                var patchFormLists = state.PatchMod.FormLists.ToList();
                patchFormLists.Count.ShouldBe(expectedFormLists.Count,
                    $"Expected {expectedFormLists.Count} FormLists in PatchMod but found {patchFormLists.Count}");

                foreach (var expected in expectedFormLists)
                {
                    var found = patchFormLists.FirstOrDefault(f => f.EditorID == expected.EditorID);
                    found.ShouldNotBeNull($"Could not find FormList with EditorID: {expected.EditorID}");
                    found!.Items.Count.ShouldBe(expected.Items.Count,
                        $"FormList {expected.EditorID} has wrong item count");

                    for (int i = 0; i < expected.Items.Count; i++)
                    {
                        found.Items[i].FormKey.ShouldBe(expected.Items[i],
                            $"FormList {expected.EditorID} item {i} mismatch");
                    }
                }
            })
            .Run(runArgs, fileSystem);

        // Verify load order was modified correctly
        capturedLoadOrder.ShouldNotBeNull();

        // The load order should contain the output mod key (patchMod), not the split files
        var loadOrderKeys = capturedLoadOrder!.ListedOrder.Select(x => x.ModKey).ToList();

        // Should contain the output mod key (the patchMod)
        loadOrderKeys.ShouldContain(outputModKey,
            $"Load order should contain output ModKey {outputModKey}");

        // Should NOT contain the split sibling file key
        var splitKey2 = new ModKey($"{sourceModKey.Name}_2", sourceModKey.Type);
        loadOrderKeys.ShouldNotContain(splitKey2,
            $"Load order should not contain split file {splitKey2}");
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
        listings
            .Select(x => new LoadOrderListing(x.ModKey, x.Enabled, x.GhostSuffix))
            .OfType<ILoadOrderListingGetter>()
            .ShouldEqualEnumerable(new ILoadOrderListingGetter[]
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