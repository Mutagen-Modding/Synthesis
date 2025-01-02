using CommandLine;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Synthesis.CLI;
using Mutagen.Bethesda.Synthesis.Internal;
using Noggog;
using Noggog.Utility;
using Synthesis.Bethesda;
using Synthesis.Bethesda.DTO;
using System.IO.Abstractions;
using System.Text.Json;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Installs.DI;
using Mutagen.Bethesda.Synthesis.Versioning;
using SynthesisBase = Synthesis.Bethesda;
using Mutagen.Bethesda.Plugins.Binary.Streams;
using Mutagen.Bethesda.Plugins.Exceptions;
using Mutagen.Bethesda.Plugins.Implicit.DI;
using Mutagen.Bethesda.Plugins.Order.DI;
using Mutagen.Bethesda.Strings.DI;
using Mutagen.Bethesda.Synthesis.Pipeline;
using Mutagen.Bethesda.Synthesis.States;
using Mutagen.Bethesda.Synthesis.States.DI;
using Synthesis.Bethesda.Commands;

namespace Mutagen.Bethesda.Synthesis;

internal record TypicalOpenParameters(GameRelease Release, ModKey ModKey, TypicalOpenExtraParameters? Extra);

/// <summary>
/// Bootstrapper API for creating a Mutagen-based patch from CLI arguments or PatcherRunSettings.<br />
/// Note that you do not have to use these systems to be Synthesis compliant.  This system serves
/// as a quick bootstrapper for some of the typical setup tasks and informational queries.
/// </summary>
public class SynthesisPipeline
{
    #region Starting Instance
    // We want to have this be a static singleton instance, as this allows us to 
    // eventually move the convenience functions out of this library, but still
    // latch on with the same API via extension functions.

    public static SynthesisPipeline Instance => new();
    #endregion

    #region Members
    record PatcherListing(Func<object, Task> Patcher, PatcherPreferences? Prefs);

    private readonly Dictionary<GameCategory, PatcherListing> _patchers = new();
    private readonly List<AsyncCheckerFunction> _runnabilityChecks = new();
    private AsyncOpenForSettingsFunction? _openForSettings;
    private AsyncOpenTypicalFunction? _openTypical;
    private readonly List<(ReflectionSettingsConfig Config, IReflectionSettingsTarget Target)> _autogeneratedSettingsTypes = new();
    internal Action<int>? _onShutdown;
    internal Action<RunStyle>? _runStyleCallback;
    private AdjustArgumentsFunction? _argumentAdjustment;
    private IFileSystem? _fileSystem;
    private TypicalOpenParameters? _typicalOpen;

    public bool HasAutogeneratedSettings => _autogeneratedSettingsTypes.Count > 0;
    #endregion

    #region AddPatch
    /// <summary>
    /// Takes in the main line command arguments, and handles PatcherRunSettings CLI inputs.
    /// </summary>
    /// <typeparam name="TMod">Setter mod interface</typeparam>
    /// <typeparam name="TModGetter">Getter only mod interface</typeparam>
    /// <param name="patcher">Patcher func that processes a load order, and returns a mod object to export.</param>
    /// <param name="userPreferences">Any custom user preferences</param>
    /// <returns>Int error code of the operation</returns>
    public SynthesisPipeline AddPatch<TMod, TModGetter>(
        AsyncPatcherFunction<TMod, TModGetter> patcher,
        PatcherPreferences? userPreferences = null)
        where TMod : class, IContextMod<TMod, TModGetter>, TModGetter
        where TModGetter : class, IContextGetterMod<TMod, TModGetter>
    {
        var cata = GameCategoryHelper.FromModType<TModGetter>();
        if (_patchers.TryGetValue(cata, out _))
        {
            throw new ArgumentException($"Cannot add two patch callbacks for the same game category: {cata}");
        }
        _patchers.Add(
            cata,
#pragma warning disable CS0618 // Type or member is obsolete
            new PatcherListing((state) => patcher((SynthesisState<TMod, TModGetter>)state), userPreferences));
#pragma warning restore CS0618 // Type or member is obsolete
        return this;
    }

    /// <summary>
    /// Takes in the main line command arguments, and handles PatcherRunSettings CLI inputs.
    /// </summary>
    /// <typeparam name="TMod">Setter mod interface</typeparam>
    /// <typeparam name="TModGetter">Getter only mod interface</typeparam>
    /// <param name="patcher">Patcher func that processes a load order, and returns a mod object to export.</param>
    /// <param name="userPreferences">Any custom user preferences</param>
    /// <returns>Int error code of the operation</returns>
    public SynthesisPipeline AddPatch<TMod, TModGetter>(
        PatcherFunction<TMod, TModGetter> patcher,
        PatcherPreferences? userPreferences = null)
        where TMod : class, IContextMod<TMod, TModGetter>, TModGetter
        where TModGetter : class, IContextGetterMod<TMod, TModGetter>
    {
        return AddPatch<TMod, TModGetter>(async (s) => patcher(s), userPreferences);
    }
    #endregion

    #region Load Order Construction

    private GetStateLoadOrder.LoadOrderReturn GetLoadOrderFromArgs(
        IBaseRunArgs args,
        PatcherPreferences? prefs,
        IFileSystem fileSystem)
    {
        var gameReleaseInjection = new GameReleaseInjection(args.GameRelease);
        var categoryContext = new GameCategoryContext(gameReleaseInjection);
        var dataDir = new DataDirectoryInjection(args.DataFolderPath);
        var gameLoc = new GameLocatorLookupCache();
        return new GetStateLoadOrder(
                new ImplicitListingsProvider(
                    fileSystem,
                    dataDir,
                    new ImplicitListingModKeyProvider(
                        gameReleaseInjection)),
                new OrderListings(),
                new CreationClubListingsProvider(
                    fileSystem,
                    dataDir,
                    new CreationClubListingsPathProvider(
                        categoryContext,
                        new CreationClubEnabledProvider(categoryContext),
                        new GameDirectoryProvider(
                            gameReleaseInjection,
                            gameLoc)),
                    new CreationClubRawListingsReader()),
                new StatePluginsListingProvider(
                    args.LoadOrderFilePath,
                    new PluginRawListingsReader(
                        fileSystem,
                        new PluginListingsParser(
                            new PluginListingCommentTrimmer(),
                            new LoadOrderListingParser(
                                new HasEnabledMarkersProvider(
                                    gameReleaseInjection))))),
                new EnableImplicitMastersFactory(fileSystem))
            .GetFinalLoadOrder(
                gameRelease: args.GameRelease,
                exportKey: ModKey.TryFromFileName(args.ModKey),
                dataFolderPath: args.DataFolderPath,
                addCcMods: !args.LoadOrderIncludesCreationClub,
                prefs);
    }

    #endregion

    #region Runnability Checks

    public delegate void CheckerFunction(IRunnabilityState state);

    public delegate Task AsyncCheckerFunction(IRunnabilityState state);

    public SynthesisPipeline AddRunnabilityCheck(CheckerFunction action)
    {
        _runnabilityChecks.Add(async (c) => action(c));
        return this;
    }

    public SynthesisPipeline AddRunnabilityCheck(AsyncCheckerFunction action)
    {
        _runnabilityChecks.Add(action);
        return this;
    }

    private async Task<Codes> CheckRunnability(CheckRunnability args, IFileSystem fileSystem)
    {
        _runStyleCallback?.Invoke(RunStyle.CheckRunnability);
        if (_runnabilityChecks.Count == 0) return Codes.NotNeeded;
        var patcher = _patchers.GetOrDefault(args.GameRelease.ToCategory());
        if (patcher == null) return Codes.NotRunnable;
        SetReflectionSettingsAnchorPaths(args.ExtraDataFolder, fileSystem);
        var loadOrder = GetLoadOrderFromArgs(args, patcher.Prefs, fileSystem);
        var state = new RunnabilityState(args, loadOrder.ProcessedLoadOrder.ToLoadOrder());
        try
        {
            await Task.WhenAll(_runnabilityChecks.Select(check =>
            {
                return check(state);
            })).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Console.Error.Write(ex);
            return Codes.NotRunnable;
        }
        return 0;
    }
    #endregion

    #region Settings

    public delegate int OpenForSettingsFunction(IOpenForSettingsState state);

    public delegate Task<int> AsyncOpenForSettingsFunction(IOpenForSettingsState state);

    public SynthesisPipeline SetOpenForSettings(OpenForSettingsFunction action)
    {
        SetOpenForSettings(async (r) => action(r));
        return this;
    }

    public SynthesisPipeline SetOpenForSettings(AsyncOpenForSettingsFunction action)
    {
        if (_openForSettings != null
            || _autogeneratedSettingsTypes.Count > 0)
        {
            throw new ArgumentException("Cannot add more than one callback type for settings");
        }
        _openForSettings = action;
        return this;
    }

    public SynthesisPipeline SetAutogeneratedSettings<TSetting>(string nickname, string path, out Lazy<TSetting> setting, bool throwIfSettingsMissing = false)
        where TSetting : class, new()
    {
        if (_openForSettings != null)
        {
            throw new ArgumentException("Cannot add more than one callback type for settings");
        }
        var target = new ReflectionSettingsTarget<TSetting>(
            path,
            throwIfSettingsMissing);
        setting = target.Value;
        _autogeneratedSettingsTypes.Add(
            (new ReflectionSettingsConfig(
                    TypeName: typeof(TSetting).ToString(),
                    Nickname: nickname,
                    Path: path),
                target));
        return this;
    }

    private async Task<int> OpenForSettings(OpenForSettings args, IFileSystem fileSystem)
    {
        _runStyleCallback?.Invoke(RunStyle.OpenForSettings);
        if (_openForSettings == null)
        {
            throw new ArgumentException("Patcher cannot open for settings.");
        }

        OpenForSettingsSetForTypicalOpen(args, fileSystem);

        SetReflectionSettingsAnchorPaths(args.ExtraDataFolder, fileSystem);
        var loadOrder = GetLoadOrderFromArgs(args, null, fileSystem);
        var state = new OpenForSettingsState(args, loadOrder.ProcessedLoadOrder.ToLoadOrder());
        return await _openForSettings(state).ConfigureAwait(false);
    }

    private void OpenForSettingsSetForTypicalOpen(OpenForSettings args, IFileSystem fileSystem)
    {
        args.GameRelease ??= _typicalOpen?.Release;
        args.ModKey ??= _typicalOpen?.ModKey.ToString();

        if (_typicalOpen != null &&
            (args.DataFolderPath == null || args.LoadOrderFilePath == null))
        {
            var defaultRun = GetDefaultRun(
                _typicalOpen.Release, _typicalOpen.ModKey,
                fileSystem, _typicalOpen.Extra);
            args.DataFolderPath = defaultRun.DataFolderPath;
            args.LoadOrderFilePath = defaultRun.LoadOrderFilePath;
        }
    }

    private async Task<int> QuerySettings(SettingsQuery args)
    {
        _runStyleCallback?.Invoke(RunStyle.QueryForSettings);
        if (_openForSettings != null) return (int)Codes.OpensForSettings;
        if (_autogeneratedSettingsTypes.Count > 0)
        {
            var configs = new ReflectionSettingsConfigs(_autogeneratedSettingsTypes.Select(i => i.Config).ToArray());
            System.Console.WriteLine(JsonSerializer.Serialize(configs));
            return (int)Codes.AutogeneratedSettingsClass;
        }
        return (int)Codes.Unsupported;
    }

    private void SetReflectionSettingsAnchorPaths(string? path, IFileSystem fileSystem)
    {
        foreach (var setting in _autogeneratedSettingsTypes)
        {
            setting.Target.AnchorPath = path;
            setting.Target.FileSystem = fileSystem;
        }
    }
    #endregion

    #region Typical Open
    public delegate int OpenTypicalFunction();

    public delegate Task<int> AsyncOpenTypicalFunction();
    
    public SynthesisPipeline SetTypicalOpen(OpenTypicalFunction action)
    {
        SetTypicalOpen(async () => action());
        return this;
    }

    public SynthesisPipeline SetTypicalOpen(AsyncOpenTypicalFunction action)
    {
        if (_openTypical != null)
        {
            throw new ArgumentException("Cannot add more than one callback for opening typically");
        }
        _openTypical = action;
        return this;
    }

    public SynthesisPipeline SetTypicalOpen(
        GameRelease targetRelease,
        ModKey identifyingModKey,
        TypicalOpenExtraParameters? extraParameters = null)
    {
        _typicalOpen = new TypicalOpenParameters(targetRelease, identifyingModKey, extraParameters);
        
        SetTypicalOpen(async () =>
        {
            var category = targetRelease.ToCategory();
            if (!_patchers.TryGetValue(category, out var patchers))
            {
                throw new ArgumentException($"No applicable patchers for {category}");
            }

            await Run(
                GetDefaultRun(targetRelease, identifyingModKey, _fileSystem.GetOrDefault(), extraParameters),
                identifyingModKey).ConfigureAwait(false);
            return 0;
        });
        return this;
    }

    private async Task<int> OpenTypical()
    {
        _runStyleCallback?.Invoke(RunStyle.Standalone);
        if (_openTypical == null)
        {
            throw new ArgumentException("Patcher cannot open normally.");
        }
        return await _openTypical().ConfigureAwait(false);
    }
    #endregion

    #region Argument Adjustment
    public delegate string[] AdjustArgumentsFunction(string[] args);

    public SynthesisPipeline AdjustArguments(AdjustArgumentsFunction adjustment)
    {
        if (_argumentAdjustment != null)
        {
            throw new ArgumentException("Cannot add more than one callback for adjusting arguments");
        }
        _argumentAdjustment = adjustment;
        return this;
    }
    #endregion

    #region Capstone Run
    public delegate void PatcherFunction<TMod, TModGetter>(IPatcherState<TMod, TModGetter> state)
        where TMod : class, IContextMod<TMod, TModGetter>, TModGetter
        where TModGetter : class, IContextGetterMod<TMod, TModGetter>;

    public delegate Task AsyncPatcherFunction<TMod, TModGetter>(IPatcherState<TMod, TModGetter> state)
        where TMod : class, IContextMod<TMod, TModGetter>, TModGetter
        where TModGetter : class, IContextGetterMod<TMod, TModGetter>;

    [Obsolete("Using SetTypicalOpen is the new preferred API for supplying RunDefaultPatcher preferences")]
    public async Task<int> Run(
        string[] args,
        RunPreferences? preferences)
    {
        return await HandleOnShutdown(() => InternalRun(args, preferences));
    }

    public async Task<int> Run(
        string[] args,
        IFileSystem? fileSystem = null)
    {
        return await HandleOnShutdown(() => InternalRun(args, null, fileSystem: fileSystem));
    }

    private async Task<int> InternalRun(
        string[] args,
        RunPreferences? preferences = null,
        IFileSystem? fileSystem = null)
    {
        _fileSystem = fileSystem;
        await using var throttler = new ConsoleThrottler().ConfigureAwait(false);
        if (_argumentAdjustment != null)
        {
            args = _argumentAdjustment(args);
        }

        if (args.Length == 0)
        {
            if (preferences?.ActionsForEmptyArgs != null)
            {
                try
                {
                    SetTypicalOpen(
                        preferences.ActionsForEmptyArgs.TargetRelease,
                        preferences.ActionsForEmptyArgs.IdentifyingModKey);
                }
                catch (Exception ex)
                {
                    System.Console.Error.WriteLine(ex);
                    if (preferences.ActionsForEmptyArgs.BlockAutomaticExit)
                    {
                        System.Console.Error.WriteLine("Error occurred.  Press enter to exit");
                        System.Console.ReadLine();
                    }
                    return -1;
                }
            }
            try
            {
                if (_openTypical != null)
                {
                    return await OpenTypical().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine(ex);
                throw;
            }
            return -1;
        }
        var parser = new Parser((s) =>
        {
            s.IgnoreUnknownArguments = true;
        });
        fileSystem = fileSystem.GetOrDefault();
        return await parser.ParseArguments(
                args,
                typeof(RunSynthesisMutagenPatcher),
                typeof(CheckRunnability),
                typeof(OpenForSettings),
                typeof(SettingsQuery))
            .MapResult(
                async (RunSynthesisMutagenPatcher settings) =>
                {
                    try
                    {
                        _runStyleCallback?.Invoke(RunStyle.RunPatcher);
                        await Run(settings, fileSystem: fileSystem).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        System.Console.Error.WriteLine(ex);
                        if (preferences?.ActionsForEmptyArgs?.BlockAutomaticExit ?? false)
                        {
                            System.Console.Error.WriteLine("Error occurred.  Press enter to exit");
                            System.Console.ReadLine();
                        }
                        return -1;
                    }
                    return 0;
                },
                async (CheckRunnability checkRunnability) => (int)await CheckRunnability(checkRunnability, fileSystem),
                (OpenForSettings openForSettings) => OpenForSettings(openForSettings, fileSystem),
                (SettingsQuery settingsQuery) => QuerySettings(settingsQuery),
                async _ =>
                {
                    Console.Error.WriteLine($"Could not parse arguments into an executable command: {string.Join(' ', args)}");
                    return -1;
                });
    }

    public async Task Run(
        RunSynthesisMutagenPatcher args,
        IFileSystem? fileSystem = null)
    {
        await HandleOnShutdown(async () =>
        {
            ModKey modKey;
            if (args.ModKey.IsNullOrWhitespace())
            {
                modKey = SynthesisBase.Constants.SynthesisModKey;
            }
            else
            {
                modKey = ModKey.FromNameAndExtension(args.ModKey);
            }

            await Run(
                args,
                modKey,
                fileSystem).ConfigureAwait(false);
            return 0;
        });
    }

    public async Task<int> Run(
        CheckRunnability args,
        IFileSystem? fileSystem = null)
    {
        return await HandleOnShutdown(async () =>
        {
            return (int)await CheckRunnability(args, fileSystem.GetOrDefault());
        });
    }

    public async Task<int> Run(
        OpenForSettings args,
        IFileSystem? fileSystem = null)
    {
        return await HandleOnShutdown(async () =>
        {
            return await OpenForSettings(args, fileSystem.GetOrDefault());
        });
    }

    [Obsolete("Using SetTypicalOpen is the new preferred API for supplying RunDefaultPatcher preferences")]
    public async Task Run(
        RunSynthesisMutagenPatcher args,
        RunPreferences? preferences)
    {
        await HandleOnShutdown(async () =>
        {
            try
            {
                await Run(args).ConfigureAwait(false);
            }
            catch (Exception ex)
                when (Environment.GetCommandLineArgs().Length == 0
                      && (preferences?.ActionsForEmptyArgs?.BlockAutomaticExit ?? false))
            {
                System.Console.Error.WriteLine(ex);
                System.Console.Error.WriteLine("Error occurred.  Press enter to exit");
                System.Console.ReadLine();
            }
            return 0;
        });
    }

    private async Task Run(
        RunSynthesisMutagenPatcher args,
        ModKey? exportKey,
        IFileSystem? fileSystem = null)
    {
        fileSystem = fileSystem.GetOrDefault();
        var versions = new ProvideCurrentVersions();
        foreach (var printout in versions.GetVersionPrintouts())
        {
            Console.WriteLine(printout);
        }
        System.Console.WriteLine(Parser.Default.FormatCommandLine(args));
        SetReflectionSettingsAnchorPaths(args.ExtraDataFolder, fileSystem);
            
        var cat = args.GameRelease.ToCategory();
        if (!_patchers.TryGetValue(cat, out var patcher))
        {
            throw new ArgumentException($"No applicable patchers for {cat}");
        }
            
        if (_runnabilityChecks.Count > 0)
        {
            System.Console.WriteLine("Checking runnability");
            var runnabilityResult = await CheckRunnability(
                new CheckRunnability()
                {
                    DataFolderPath = args.DataFolderPath,
                    GameRelease = args.GameRelease,
                    LoadOrderFilePath = args.LoadOrderFilePath,
                    ExtraDataFolder = args.ExtraDataFolder,
                    InternalDataFolder = args.InternalDataFolder,
                    DefaultDataFolderPath = args.DefaultDataFolderPath,
                    ModKey = args.ModKey,
                    LoadOrderIncludesCreationClub = args.LoadOrderIncludesCreationClub
                },
                fileSystem: fileSystem).ConfigureAwait(false);
            if (runnabilityResult == Codes.NotRunnable)
            {
                throw new ArgumentException("Patcher responded that it was not runnable.");
            }
            System.Console.WriteLine("Checking runnability complete");
        }
            
        System.Console.WriteLine("Prepping state.");
        var prefs = patcher.Prefs ?? new PatcherPreferences();
        var gameReleaseInjection = new GameReleaseInjection(args.GameRelease);
        var categoryContext = new GameCategoryContext(gameReleaseInjection);
        var dataDir = new DataDirectoryInjection(args.DataFolderPath);
        var gameLoc = new GameLocatorLookupCache();
        var stateFactory = new PatcherStateFactory(
            fileSystem,
            new LoadOrderImporterFactory(
                fileSystem,
                new MasterFlagsLookupProvider(
                    gameReleaseInjection,
                    fileSystem,
                    dataDir)),
            new GetStateLoadOrder(
                new ImplicitListingsProvider(
                    fileSystem,
                    dataDir,
                    new ImplicitListingModKeyProvider(
                        gameReleaseInjection)),
                new OrderListings(),
                new CreationClubListingsProvider(
                    fileSystem,
                    dataDir,
                    new CreationClubListingsPathProvider(
                        categoryContext,
                        new CreationClubEnabledProvider(categoryContext),
                        new GameDirectoryProvider(
                            gameReleaseInjection,
                            gameLoc)),
                    new CreationClubRawListingsReader()),
                new StatePluginsListingProvider(
                    args.LoadOrderFilePath,
                    new PluginRawListingsReader(
                        fileSystem,
                        new PluginListingsParser(
                            new PluginListingCommentTrimmer(),
                            new LoadOrderListingParser(
                                new HasEnabledMarkersProvider(
                                    gameReleaseInjection))))),
                new EnableImplicitMastersFactory(fileSystem)));
            
        exportKey = exportKey == null || exportKey.Value.IsNull ? SynthesisBase.Constants.SynthesisModKey : exportKey.Value;
        using var state = stateFactory.ToState(cat, args, prefs, exportKey.Value);
            
        System.Console.WriteLine("Running patch.");
        await patcher.Patcher(state).ConfigureAwait(false);
        System.Console.WriteLine("Finished patch.");
            
        if (prefs.NoPatch) return;
            
        System.Console.WriteLine($"Writing to output: {args.OutputPath}");
        Directory.CreateDirectory(Path.GetDirectoryName(args.OutputPath)!);

        try
        {
            await state.PatchMod.BeginWrite
                .ToPath(args.OutputPath)
                .WithLoadOrder(state.LoadOrderForPipeline)
                .WithFileSystem(fileSystem)
                .NoModKeySync()
                .WithTargetLanguage(args.TargetLanguage)
                .WithEmbeddedEncodings(
                    args.UseUtf8ForEmbeddedStrings 
                        ? new EncodingBundle(NonTranslated: MutagenEncoding._1252, NonLocalized: MutagenEncoding._utf8)
                        : null)
                .WithForcedLowerFormIdRangeUsage(args.FormIDRangeMode.ToForceBool())
                .WriteAsync();
        }
        catch (TooManyMastersException tooMany)
        {
            System.Console.WriteLine(tooMany.Message);
            System.Console.WriteLine("Masters:");
            foreach (var master in tooMany.Masters)
            {
                Console.WriteLine($"  {master}");
            }
            throw;
        }
    }
    #endregion

    #region Depreciated Patch Finisher

#pragma warning disable CS0618
    public delegate void DepreciatedPatcherFunction<TMod, TModGetter>(SynthesisState<TMod, TModGetter> state)
        where TMod : class, IContextMod<TMod, TModGetter>, TModGetter
        where TModGetter : class, IContextGetterMod<TMod, TModGetter>;

    public delegate Task DepreciatedAsyncPatcherFunction<TMod, TModGetter>(SynthesisState<TMod, TModGetter> state)
        where TMod : class, IContextMod<TMod, TModGetter>, TModGetter
        where TModGetter : class, IContextGetterMod<TMod, TModGetter>;
    
    private SynthesisState<TMod, TModGetter> ToDepreciatedState<TMod, TModGetter>(IPatcherState<TMod, TModGetter> state)
        where TMod : class, IContextMod<TMod, TModGetter>, TModGetter
        where TModGetter : class, IContextGetterMod<TMod, TModGetter>
    {
        if (state is SynthesisState<TMod, TModGetter> depreciatedState)
        {
            return depreciatedState;
        }
        throw new ArgumentException("Using the depreciated \'Patch\' call is causing problems.  Upgrade to the newest API");
    }
#pragma warning restore CS0618

    /// <summary>
    /// Takes in the main line command arguments, and handles PatcherRunSettings CLI inputs.
    /// </summary>
    /// <typeparam name="TMod">Setter mod interface</typeparam>
    /// <typeparam name="TModGetter">Getter only mod interface</typeparam>
    /// <param name="args">Main command line args</param>
    /// <param name="patcher">Patcher func that processes a load order, and returns a mod object to export.</param>
    /// <param name="userPreferences">Any custom user preferences</param>
    /// <returns>Int error code of the operation</returns>
    [Obsolete("Using the AddPatch().Run() combination chain is the new preferred API")]
    public async Task<int> Patch<TMod, TModGetter>(
        string[] args,
        DepreciatedAsyncPatcherFunction<TMod, TModGetter> patcher,
        UserPreferences? userPreferences = null)
        where TMod : class, IContextMod<TMod, TModGetter>, TModGetter
        where TModGetter : class, IContextGetterMod<TMod, TModGetter>
    {
        return await AddPatch<TMod, TModGetter>(state => patcher(ToDepreciatedState(state)), userPreferences?.ToPatcherPrefs())
            .Run(args, userPreferences?.ToRunPrefs());
    }

    /// <summary>
    /// Takes in the main line command arguments, and handles PatcherRunSettings CLI inputs.
    /// </summary>
    /// <typeparam name="TMod">Setter mod interface</typeparam>
    /// <typeparam name="TModGetter">Getter only mod interface</typeparam>
    /// <param name="args">Main command line args</param>
    /// <param name="patcher">Patcher func that processes a load order, and returns a mod object to export.</param>
    /// <param name="userPreferences">Any custom user preferences</param>
    /// <returns>Int error code of the operation</returns>
    [Obsolete("Using the AddPatch().Run() combination chain is the new preferred API")]
    public int Patch<TMod, TModGetter>(
        string[] args,
        DepreciatedPatcherFunction<TMod, TModGetter> patcher,
        UserPreferences? userPreferences = null)
        where TMod : class, IContextMod<TMod, TModGetter>, TModGetter
        where TModGetter : class, IContextGetterMod<TMod, TModGetter>
    {
        return AddPatch<TMod, TModGetter>(state => patcher(ToDepreciatedState(state)), userPreferences?.ToPatcherPrefs())
            .Run(args, userPreferences?.ToRunPrefs()).Result;
    }

    /// <summary>
    /// Takes in the main line command arguments, and handles PatcherRunSettings CLI inputs.
    /// </summary>
    /// <typeparam name="TMod">Setter mod interface</typeparam>
    /// <typeparam name="TModGetter">Getter only mod interface</typeparam>
    /// <param name="settings">Patcher run settings</param>
    /// <param name="patcher">Patcher func that processes a load order, and returns a mod object to export.</param>
    /// <param name="userPreferences">Any custom user preferences</param>
    /// <param name="fileSystem">File system to run on</param>
    [Obsolete("Using the AddPatch().Run() combination chain is the new preferred API")]
    public async Task Patch<TMod, TModGetter>(
        RunSynthesisMutagenPatcher settings,
        DepreciatedAsyncPatcherFunction<TMod, TModGetter> patcher,
        UserPreferences? userPreferences = null,
        IFileSystem? fileSystem = null)
        where TMod : class, IContextMod<TMod, TModGetter>, TModGetter
        where TModGetter : class, IContextGetterMod<TMod, TModGetter>
    {
        try
        {
            await AddPatch<TMod, TModGetter>(state => patcher(ToDepreciatedState(state)), userPreferences?.ToPatcherPrefs())
                .Run(settings, fileSystem);
        }
        catch (Exception ex)
            when (Environment.GetCommandLineArgs().Length == 0
                  && (userPreferences?.ToRunPrefs().ActionsForEmptyArgs?.BlockAutomaticExit ?? false))
        {
            System.Console.Error.WriteLine(ex);
            System.Console.Error.WriteLine("Error occurred.  Press enter to exit");
            System.Console.ReadLine();
        }
    }

    /// <summary>
    /// Takes in the main line command arguments, and handles PatcherRunSettings CLI inputs.
    /// </summary>
    /// <typeparam name="TMod">Setter mod interface</typeparam>
    /// <typeparam name="TModGetter">Getter only mod interface</typeparam>
    /// <param name="settings">Patcher run settings</param>
    /// <param name="patcher">Patcher func that processes a load order, and returns a mod object to export.</param>
    /// <param name="userPreferences">Any custom user preferences</param>
    /// <param name="fileSystem">File system to run on</param>
    [Obsolete("Using the AddPatch().Run() combination chain is the new preferred API")]
    public void Patch<TMod, TModGetter>(
        RunSynthesisMutagenPatcher settings,
        DepreciatedPatcherFunction<TMod, TModGetter> patcher,
        UserPreferences? userPreferences = null,
        IFileSystem? fileSystem = null)
        where TMod : class, IContextMod<TMod, TModGetter>, TModGetter
        where TModGetter : class, IContextGetterMod<TMod, TModGetter>
    {
        try
        {
            AddPatch<TMod, TModGetter>(state => patcher(ToDepreciatedState(state)), userPreferences?.ToPatcherPrefs())
                .Run(settings, fileSystem).Wait();
        }
        catch (Exception ex)
            when (Environment.GetCommandLineArgs().Length == 0
                  && (userPreferences?.ToRunPrefs().ActionsForEmptyArgs?.BlockAutomaticExit ?? false))
        {
            System.Console.Error.WriteLine(ex);
            System.Console.Error.WriteLine("Error occurred.  Press enter to exit");
            System.Console.ReadLine();
        }
    }
    #endregion

    private static string? LocateInternalData(IFileSystem fileSystem)
    {
        var internalDataDir = new DirectoryPath("InternalData");
        if (internalDataDir.CheckExists(fileSystem))
        {
            return internalDataDir.Path;
        }
        
        internalDataDir = new DirectoryPath(Path.Combine("..", "..", "InternalData"));
        if (internalDataDir.CheckExists(fileSystem))
        {
            return internalDataDir.Path;
        }
        
        internalDataDir = new DirectoryPath(Path.Combine("..", "..", "..", "InternalData"));
        if (internalDataDir.CheckExists(fileSystem))
        {
            return internalDataDir.Path;
        }

        return null;
    }

    private static RunSynthesisMutagenPatcher GetDefaultRun(
        GameRelease release,
        ModKey targetModKey,
        IFileSystem fileSystem,
        TypicalOpenExtraParameters? extraParameters = null)
    {
        extraParameters ??= new();

        IDataDirectoryLookup dataDir = new GameLocatorLookupCache();
        
        if (!dataDir.TryGet(release, out var dataFolder))
        {
            throw new DirectoryNotFoundException("Could not locate game folder automatically.");
        }

        if (!PluginListings.TryGetListingsFile(release, out var loadOrderPath))
        {
            throw new FileNotFoundException("Could not locate load order automatically.");
        }

        return new RunSynthesisMutagenPatcher()
        {
            DataFolderPath = dataFolder,
            SourcePath = null,
            OutputPath = Path.Combine(dataFolder, targetModKey.FileName),
            GameRelease = release,
            LoadOrderFilePath = loadOrderPath,
            ExtraDataFolder = Path.GetFullPath("./Data"),
            DefaultDataFolderPath = null,
            LoadOrderIncludesCreationClub = false,
            PatcherName = targetModKey.Name,
            PersistencePath = "Persistence",
            InternalDataFolder = LocateInternalData(fileSystem),
            TargetLanguage = extraParameters.TargetLanguage,
            Localize = extraParameters.Localize,
            UseUtf8ForEmbeddedStrings = extraParameters.UseUtf8ForEmbeddedStrings,
            FormIDRangeMode = extraParameters.FormIDRangeMode,
            HeaderVersionOverride = extraParameters.HeaderVersionOverride,
        };
    }

    internal SynthesisPipeline SetOnShutdown(Action<int> callback)
    {
        _onShutdown = callback;
        return this;
    }
    
    private int HandleOnShutdown(int result)
    {
        _onShutdown?.Invoke(result);
        _onShutdown = null;
        return result;
    }
    
    private async Task<int> HandleOnShutdown(Func<Task<int>> a)
    {
        int code = -1;
        try
        {
            code = await a().ConfigureAwait(false);
        }
        finally
        {
            code = HandleOnShutdown(code);
        }

        return code;
    }
}