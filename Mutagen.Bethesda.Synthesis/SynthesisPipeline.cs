using CommandLine;
using Mutagen.Bethesda.Synthesis.CLI;
using Mutagen.Bethesda.Synthesis.Internal;
using Noggog;
using Synthesis.Bethesda;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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

        public static readonly SynthesisPipeline Instance = new SynthesisPipeline();
        #endregion

        #region Delegates
        public delegate void DepreciatedPatcherFunction<TMod, TModGetter>(SynthesisState<TMod, TModGetter> state)
            where TMod : class, IContextMod<TMod>, TModGetter
            where TModGetter : class, IContextGetterMod<TMod>;

        public delegate Task DepreciatedAsyncPatcherFunction<TMod, TModGetter>(SynthesisState<TMod, TModGetter> state)
            where TMod : class, IContextMod<TMod>, TModGetter
            where TModGetter : class, IContextGetterMod<TMod>;

        public delegate void PatcherFunction<TMod, TModGetter>(IPatcherState<TMod, TModGetter> state)
            where TMod : class, IContextMod<TMod>, TModGetter
            where TModGetter : class, IContextGetterMod<TMod>;

        public delegate Task AsyncPatcherFunction<TMod, TModGetter>(IPatcherState<TMod, TModGetter> state)
            where TMod : class, IContextMod<TMod>, TModGetter
            where TModGetter : class, IContextGetterMod<TMod>;

        public delegate void CheckerFunction(IRunnabilityState state);

        public delegate Task AsyncCheckerFunction(IRunnabilityState state);
        #endregion

        #region Members
        record PatcherListing(Func<object, Task> Patcher, PatcherPreferences? Prefs);

        private readonly Dictionary<GameCategory, PatcherListing> _patchers = new Dictionary<GameCategory, PatcherListing>();
        private List<AsyncCheckerFunction> _runnabilityChecks = new List<AsyncCheckerFunction>();
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
            where TMod : class, IContextMod<TMod>, TModGetter
            where TModGetter : class, IContextGetterMod<TMod>
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
            where TMod : class, IContextMod<TMod>, TModGetter
            where TModGetter : class, IContextGetterMod<TMod>
        {
            return AddPatch<TMod, TModGetter>(async (s) => patcher(s), userPreferences);
        }
        #endregion

        #region Runnability Checks
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
                release: args.GameRelease,
                loadOrderFilePath: args.LoadOrderFilePath,
                dataFolderPath: args.DataFolderPath,
                patcher?.Prefs)
                .ToList();
            var state = new RunnabilityState(args, loadOrder);
            await Task.WhenAll(_runnabilityChecks.Select(check =>
            {
                return check(state);
            }));
            return 0;
        }
        #endregion

        #region Capstone Run
        public async Task<int> Run(
            string[] args,
            RunPreferences? preferences = null)
        {
            if (args.Length == 0)
            {
                if (preferences?.ActionsForEmptyArgs == null) return -1;
                var category = preferences.ActionsForEmptyArgs.TargetRelease.ToCategory();
                if (!_patchers.TryGetValue(category, out var patchers)) return -1;

                try
                {
                    await Run(
                        GetDefaultRun(preferences.ActionsForEmptyArgs.TargetRelease, preferences.ActionsForEmptyArgs.IdentifyingModKey),
                        preferences.ActionsForEmptyArgs.IdentifyingModKey,
                        preferences);
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
                if (preferences.ActionsForEmptyArgs.BlockAutomaticExit)
                {
                    System.Console.Error.WriteLine("Press enter to exit");
                    System.Console.ReadLine();
                }
                return 0;
            }
            var parser = new Parser((s) =>
            {
                s.IgnoreUnknownArguments = true;
            });
            return await parser.ParseArguments(args, typeof(RunSynthesisMutagenPatcher), typeof(CheckRunnability))
                .MapResult(
                    async (RunSynthesisMutagenPatcher settings) =>
                    {
                        try
                        {
                            await Run(settings, preferences);
                        }
                        catch (Exception ex)
                        {
                            System.Console.Error.WriteLine(ex);
                            return -1;
                        }
                        return 0;
                    },
                    (CheckRunnability checkRunnabiity) => CheckRunnability(checkRunnabiity),
                    async _ =>
                    {
                        return -1;
                    });
        }

        public Task Run(
            RunSynthesisMutagenPatcher args,
            RunPreferences? preferences = null)
        {
            return Run(args, SynthesisBase.Constants.SynthesisModKey, preferences);
        }

        private async Task Run(
            RunSynthesisMutagenPatcher args,
            ModKey exportKey,
            RunPreferences? preferences)
        {
            try
            {
                Console.WriteLine($"Mutagen version: {Versions.MutagenVersion}");
                Console.WriteLine($"Mutagen sha: {Versions.MutagenSha}");
                Console.WriteLine($"Synthesis version: {Versions.SynthesisVersion}");
                Console.WriteLine($"Synthesis sha: {Versions.SynthesisSha}");
                System.Console.WriteLine(args.ToString());
                var cat = args.GameRelease.ToCategory();
                if (!_patchers.TryGetValue(cat, out var patcher))
                {
                    throw new ArgumentException($"No applicable patchers for {cat}");
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
            catch (Exception ex)
            when (Environment.GetCommandLineArgs().Length == 0
                && (preferences?.ActionsForEmptyArgs?.BlockAutomaticExit ?? false))
            {
                System.Console.Error.WriteLine(ex);
                System.Console.Error.WriteLine("Error occurred.  Press enter to exit");
                System.Console.ReadLine();
            }
        }
        #endregion

        #region Depreciated Patch Finisher
        private SynthesisState<TMod, TModGetter> ToDepreciatedState<TMod, TModGetter>(IPatcherState<TMod, TModGetter> state)
            where TMod : class, IContextMod<TMod>, TModGetter
            where TModGetter : class, IContextGetterMod<TMod>
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
            where TMod : class, IContextMod<TMod>, TModGetter
            where TModGetter : class, IContextGetterMod<TMod>
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
            where TMod : class, IContextMod<TMod>, TModGetter
            where TModGetter : class, IContextGetterMod<TMod>
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
            where TMod : class, IContextMod<TMod>, TModGetter
            where TModGetter : class, IContextGetterMod<TMod>
        {
            await AddPatch<TMod, TModGetter>(state => patcher(ToDepreciatedState(state)), userPreferences?.ToPatcherPrefs())
                .Run(settings, userPreferences?.ToRunPrefs());
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
            where TMod : class, IContextMod<TMod>, TModGetter
            where TModGetter : class, IContextGetterMod<TMod>
        {
            AddPatch<TMod, TModGetter>(state => patcher(ToDepreciatedState(state)), userPreferences?.ToPatcherPrefs())
                .Run(settings, userPreferences?.ToRunPrefs()).Wait();
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

            var dataPath = Path.Combine(Wabbajack.Common.GameExtensions.MetaData(release.ToWjGame()).GameLocation().ToString(), "Data");
            if (!PluginListings.TryGetListingsFile(release, out var path))
            {
                throw new FileNotFoundException("Could not locate load order automatically.");
            }
            return new RunSynthesisMutagenPatcher()
            {
                DataFolderPath = dataPath,
                SourcePath = null,
                OutputPath = Path.Combine(dataPath, targetModKey.FileName),
                GameRelease = release,
                LoadOrderFilePath = path.Path,
                ExtraDataFolder = Path.GetFullPath("./Data")
            };
        }
    }
}
