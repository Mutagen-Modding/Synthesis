using CommandLine;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Synthesis.CLI;
using Mutagen.Bethesda.Synthesis.Internal;
using Noggog;
using Noggog.Utility;
using Synthesis.Bethesda;
using Synthesis.Bethesda.DTO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Mutagen.Bethesda.Installs;
using Mutagen.Bethesda.Synthesis.Versioning;
using SynthesisBase = Synthesis.Bethesda;

namespace Mutagen.Bethesda.Synthesis
{
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

        public static readonly SynthesisPipeline Instance = new();
        #endregion

        #region Members
        record PatcherListing(Func<object, Task> Patcher, PatcherPreferences? Prefs);

        private readonly Dictionary<GameCategory, PatcherListing> _patchers = new();
        private readonly List<AsyncCheckerFunction> _runnabilityChecks = new();
        private AsyncOpenFunction? _openForSettings;
        private AsyncOpenFunction? _openTypical;
        private readonly List<(ReflectionSettingsConfig Config, IReflectionSettingsTarget Target)> _autogeneratedSettingsTypes = new();
        internal Action<int>? _onShutdown;
        private AdjustArgumentsFunction? _argumentAdjustment;

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
            if (_patchers.TryGetValue(cata, out var _))
            {
                throw new ArgumentException($"Cannot add two patch callbacks for the same game category: {cata}");
            }
            _patchers.Add(
                cata,
                new PatcherListing((state) => patcher((SynthesisState<TMod, TModGetter>)state), userPreferences));
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

        private async Task<int> CheckRunnability(CheckRunnability args)
        {
            var patcher = _patchers.GetOrDefault(args.GameRelease.ToCategory());
            var loadOrder = Utility.GetLoadOrder(
                fileSystem: IFileSystemExt.DefaultFilesystem,
                release: args.GameRelease,
                loadOrderFilePath: args.LoadOrderFilePath,
                dataFolderPath: args.DataFolderPath,
                patcher?.Prefs)
                .ToLoadOrder();
            var state = new RunnabilityState(args, loadOrder);
            try
            {
                await Task.WhenAll(_runnabilityChecks.Select(check =>
                {
                    return check(state);
                }));
            }
            catch (Exception ex)
            {
                System.Console.Error.Write(ex);
                return (int)Codes.NotRunnable;
            }
            return 0;
        }
        #endregion

        #region Settings

        public delegate int OpenFunction(Rectangle rectangle);

        public delegate Task<int> AsyncOpenFunction(Rectangle rectangle);

        public SynthesisPipeline SetOpenForSettings(OpenFunction action)
        {
            SetOpenForSettings(async (r) => action(r));
            return this;
        }

        public SynthesisPipeline SetOpenForSettings(AsyncOpenFunction action)
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

        private async Task<int> OpenForSettings(OpenForSettings args)
        {
            if (_openForSettings == null)
            {
                throw new ArgumentException("Patcher cannot open for settings.");
            }
            await _openForSettings(
                new Rectangle(
                    x: args.Left,
                    y: args.Top,
                    width: args.Width,
                    height: args.Height));
            return 0;
        }

        private async Task<int> QuerySettings(SettingsQuery args)
        {
            if (_openForSettings != null) return (int)Codes.OpensForSettings;
            if (_autogeneratedSettingsTypes.Count > 0)
            {
                var configs = new ReflectionSettingsConfigs(_autogeneratedSettingsTypes.Select(i => i.Config).ToArray());
                System.Console.WriteLine(JsonSerializer.Serialize(configs));
                return (int)Codes.AutogeneratedSettingsClass;
            }
            return (int)Codes.Unsupported;
        }

        private void SetReflectionSettingsAnchorPaths(string? path)
        {
            foreach (var setting in _autogeneratedSettingsTypes)
            {
                setting.Target.AnchorPath = path;
            }
        }
        #endregion

        #region Typical Open
        public SynthesisPipeline SetTypicalOpen(OpenFunction action)
        {
            SetTypicalOpen(async (r) => action(r));
            return this;
        }

        public SynthesisPipeline SetTypicalOpen(AsyncOpenFunction action)
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
            ModKey identifyingModKey)
        {
            SetTypicalOpen(async (r) =>
            {
                var category = targetRelease.ToCategory();
                if (!_patchers.TryGetValue(category, out var patchers)) return -1;

                await Run(
                    GetDefaultRun(targetRelease, identifyingModKey),
                    identifyingModKey);
                return 0;
            });
            return this;
        }

        private async Task<int> OpenTypical(OpenForSettings args)
        {
            if (_openTypical == null)
            {
                throw new ArgumentException("Patcher cannot open normally.");
            }
            await _openTypical(
                new Rectangle(
                    x: args.Left,
                    y: args.Top,
                    width: args.Width,
                    height: args.Height));
            return 0;
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
            return HandleOnShutdown(await InternalRun(args, preferences));
        }

        public async Task<int> Run(string[] args)
        {
            return HandleOnShutdown(await InternalRun(args, null));
        }

        private async Task<int> InternalRun(
            string[] args,
            RunPreferences? preferences = null)
        {
            await using var throttler = new ConsoleThrottler();
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
                var openSettings = new OpenForSettings()
                {
                    Height = 15,
                    Width = 15,
                    Left = 15,
                    Top = 15
                };
                try
                {
                    if (_openTypical != null)
                    {
                        return await OpenTypical(openSettings);
                    }
                    else if (_openForSettings != null)
                    {
                        return await OpenForSettings(openSettings);
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
                            await Run(settings);
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
                    (CheckRunnability checkRunnability) => CheckRunnability(checkRunnability),
                    (OpenForSettings openForSettings) => OpenForSettings(openForSettings),
                    (SettingsQuery settingsQuery) => QuerySettings(settingsQuery),
                    async _ =>
                    {
                        Console.Error.WriteLine($"Could not parse arguments into an executable command: {string.Join(' ', args)}");
                        return -1;
                    });
        }

        public async Task Run(RunSynthesisMutagenPatcher args)
        {
            await Run(args, SynthesisBase.Constants.SynthesisModKey);
        }

        [Obsolete("Using SetTypicalOpen is the new preferred API for supplying RunDefaultPatcher preferences")]
        public async Task Run(
            RunSynthesisMutagenPatcher args,
            RunPreferences? preferences)
        {
            try
            {
                await Run(args);
            }
            catch (Exception ex)
            when (Environment.GetCommandLineArgs().Length == 0
                && (preferences?.ActionsForEmptyArgs?.BlockAutomaticExit ?? false))
            {
                System.Console.Error.WriteLine(ex);
                System.Console.Error.WriteLine("Error occurred.  Press enter to exit");
                System.Console.ReadLine();
            }
        }

        private async Task Run(
            RunSynthesisMutagenPatcher args,
            ModKey exportKey)
        {
            Console.WriteLine($"Mutagen version: {Versions.MutagenVersion}");
            Console.WriteLine($"Mutagen sha: {Versions.MutagenSha}");
            Console.WriteLine($"Synthesis version: {Versions.SynthesisVersion}");
            Console.WriteLine($"Synthesis sha: {Versions.SynthesisSha}");
            System.Console.WriteLine(Parser.Default.FormatCommandLine(args));
            SetReflectionSettingsAnchorPaths(args.ExtraDataFolder);
            var cat = args.GameRelease.ToCategory();
            if (!_patchers.TryGetValue(cat, out var patcher))
            {
                throw new ArgumentException($"No applicable patchers for {cat}");
            }
            if (_runnabilityChecks.Count > 0)
            {
                System.Console.WriteLine("Checking runnability");
                await CheckRunnability(new CheckRunnability()
                {
                    DataFolderPath = args.DataFolderPath,
                    GameRelease = args.GameRelease,
                    LoadOrderFilePath = args.LoadOrderFilePath
                });
                System.Console.WriteLine("Checking runnability complete");
            }
            WarmupAll.Init();
            System.Console.WriteLine("Prepping state.");
            var prefs = patcher.Prefs ?? new PatcherPreferences();
            using var state = Utility.ToState(cat, args, prefs, exportKey);
            await patcher.Patcher(state).ConfigureAwait(false);
            System.Console.WriteLine("Running patch.");
            if (!prefs.NoPatch)
            {
                System.Console.WriteLine($"Writing to output: {args.OutputPath}");
                state.PatchMod.WriteToBinaryParallel(path: args.OutputPath, param: GetWriteParams(state.RawLoadOrder.Select(x => x.ModKey)));
            }
        }
        #endregion

        #region Depreciated Patch Finisher
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
        [Obsolete("Using the AddPatch().Run() combination chain is the new preferred API")]
        public async Task Patch<TMod, TModGetter>(
            RunSynthesisMutagenPatcher settings,
            DepreciatedAsyncPatcherFunction<TMod, TModGetter> patcher,
            UserPreferences? userPreferences = null)
            where TMod : class, IContextMod<TMod, TModGetter>, TModGetter
            where TModGetter : class, IContextGetterMod<TMod, TModGetter>
        {
            try
            {
                await AddPatch<TMod, TModGetter>(state => patcher(ToDepreciatedState(state)), userPreferences?.ToPatcherPrefs())
                    .Run(settings);
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
        [Obsolete("Using the AddPatch().Run() combination chain is the new preferred API")]
        public void Patch<TMod, TModGetter>(
            RunSynthesisMutagenPatcher settings,
            DepreciatedPatcherFunction<TMod, TModGetter> patcher,
            UserPreferences? userPreferences = null)
            where TMod : class, IContextMod<TMod, TModGetter>, TModGetter
            where TModGetter : class, IContextGetterMod<TMod, TModGetter>
        {
            try
            {
                AddPatch<TMod, TModGetter>(state => patcher(ToDepreciatedState(state)), userPreferences?.ToPatcherPrefs())
                    .Run(settings).Wait();
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

        private BinaryWriteParameters GetWriteParams(IEnumerable<ModKey> loadOrder)
        {
            return new BinaryWriteParameters()
            {
                ModKey = BinaryWriteParameters.ModKeyOption.NoCheck,
                MastersListOrdering = new BinaryWriteParameters.MastersListOrderingByLoadOrder(loadOrder),
            };
        }

        public static RunSynthesisMutagenPatcher GetDefaultRun(GameRelease release, ModKey targetModKey)
        {
            if (!GameLocations.TryGetGameFolder(release, out var gameFolder))
            {
                throw new DirectoryNotFoundException("Could not locate game folder automatically.");
            }

            if (!PluginListings.TryGetListingsFile(release, out var path))
            {
                throw new FileNotFoundException("Could not locate load order automatically.");
            }

            var dataPath = Path.Combine(gameFolder, "Data");
            return new RunSynthesisMutagenPatcher()
            {
                DataFolderPath = dataPath,
                SourcePath = null,
                OutputPath = Path.Combine(dataPath, targetModKey.FileName),
                GameRelease = release,
                LoadOrderFilePath = path.Path,
                ExtraDataFolder = Path.GetFullPath("./Data"),
                DefaultDataFolderPath = null,
                PatcherName = targetModKey.Name,
                PersistencePath = "Persistence"
            };
        }

        private int HandleOnShutdown(int result)
        {
            _onShutdown?.Invoke(result);
            return result;
        }
    }
}
