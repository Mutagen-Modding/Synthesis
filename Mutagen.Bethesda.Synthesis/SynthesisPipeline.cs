using CommandLine;
using Mutagen.Bethesda.Synthesis.CLI;
using Mutagen.Bethesda.Synthesis.Internal;
using Noggog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Wabbajack.Common;

namespace Mutagen.Bethesda.Synthesis
{
    /// <summary>
    /// Bootstrapper API for creating a Mutagen-based patch from CLI arguments or PatcherRunSettings.<br />
    /// Note that you do not have to use these systems to be Synthesis compliant.  This system serves
    /// as a quick bootstrapper for some of the typical setup tasks and informational queries.
    /// </summary>
    public class SynthesisPipeline
    {
        // We want to have this be a static singleton instance, as this allows us to 
        // eventually move the convenience functions out of this library, but still
        // latch on with the same API via extension functions.

        public static readonly SynthesisPipeline Instance = new SynthesisPipeline();

        public delegate void PatcherFunction<TMod, TModGetter>(SynthesisState<TMod, TModGetter> state)
            where TMod : class, IContextMod<TMod>, TModGetter
            where TModGetter : class, IContextGetterMod<TMod>;

        public delegate Task AsyncPatcherFunction<TMod, TModGetter>(SynthesisState<TMod, TModGetter> state)
            where TMod : class, IContextMod<TMod>, TModGetter
            where TModGetter : class, IContextGetterMod<TMod>;

        #region Patch
        /// <summary>
        /// Takes in the main line command arguments, and handles PatcherRunSettings CLI inputs.
        /// </summary>
        /// <typeparam name="TMod">Setter mod interface</typeparam>
        /// <typeparam name="TModGetter">Getter only mod interface</typeparam>
        /// <param name="args">Main command line args</param>
        /// <param name="patcher">Patcher func that processes a load order, and returns a mod object to export.</param>
        /// <param name="userPreferences">Any custom user preferences</param>
        /// <returns>Int error code of the operation</returns>
        public async Task<int> Patch<TMod, TModGetter>(
            string[] args,
            AsyncPatcherFunction<TMod, TModGetter> patcher,
            UserPreferences? userPreferences = null)
            where TMod : class, IContextMod<TMod>, TModGetter
            where TModGetter : class, IContextGetterMod<TMod>
        {
            if (args.Length == 0)
            {
                var prefs = userPreferences ?? new UserPreferences();
                if (prefs.ActionsForEmptyArgs != null)
                {
                    try
                    {
                        await Patch(
                            GetDefaultRun(prefs, prefs.ActionsForEmptyArgs),
                            patcher,
                            prefs);
                    }
                    catch (Exception ex)
                    {
                        System.Console.Error.WriteLine(ex);
                        if (prefs.ActionsForEmptyArgs.BlockAutomaticExit)
                        {
                            System.Console.Error.WriteLine("Error occurred.  Press enter to exit");
                            System.Console.ReadLine();
                        }
                        return -1;
                    }
                    if (prefs.ActionsForEmptyArgs.BlockAutomaticExit)
                    {
                        System.Console.Error.WriteLine("Press enter to exit");
                        System.Console.ReadLine();
                    }
                    return 0;
                }
            }
            var parser = new Parser((s) =>
            {
                s.IgnoreUnknownArguments = true;
            });
            return await parser.ParseArguments(args, typeof(RunSynthesisMutagenPatcher))
                .MapResult(
                    async (RunSynthesisMutagenPatcher settings) =>
                    {
                        try
                        {
                            System.Console.WriteLine(settings.ToString());
                            await Patch(
                                settings,
                                patcher,
                                userPreferences);
                        }
                        catch (Exception ex)
                        {
                            System.Console.Error.WriteLine(ex);
                            return -1;
                        }
                        return 0;
                    },
                    async _ =>
                    {
                        return -1;
                    });
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
        public int Patch<TMod, TModGetter>(
            string[] args,
            PatcherFunction<TMod, TModGetter> patcher,
            UserPreferences? userPreferences = null)
            where TMod : class, IContextMod<TMod>, TModGetter
            where TModGetter : class, IContextGetterMod<TMod>
        {
            Console.WriteLine($"Mutagen version: {Versions.MutagenVersion}");
            Console.WriteLine($"Mutagen sha: {Versions.MutagenSha}");
            Console.WriteLine($"Synthesis version: {Versions.SynthesisVersion}");
            Console.WriteLine($"Synthesis sha: {Versions.SynthesisSha}");
            if (args.Length == 0)
            {
                var prefs = userPreferences ?? new UserPreferences();
                if (prefs.ActionsForEmptyArgs != null)
                {
                    try
                    {
                        Patch(
                            GetDefaultRun(prefs, prefs.ActionsForEmptyArgs),
                            patcher,
                            prefs);
                    }
                    catch (Exception ex)
                    {
                        System.Console.Error.WriteLine(ex);
                        if (prefs.ActionsForEmptyArgs.BlockAutomaticExit)
                        {
                            System.Console.Error.WriteLine("Error occurred.  Press enter to exit");
                            System.Console.ReadLine();
                        }
                        return -1;
                    }
                    if (prefs.ActionsForEmptyArgs.BlockAutomaticExit)
                    {
                        System.Console.Error.WriteLine("Press enter to exit");
                        System.Console.ReadLine();
                    }
                    return 0;
                }
            }
            var parser = new Parser((s) =>
            {
                s.IgnoreUnknownArguments = true;
            });
            return parser.ParseArguments(args, typeof(RunSynthesisMutagenPatcher))
                .MapResult(
                    (RunSynthesisMutagenPatcher settings) =>
                    {
                        try
                        {
                            System.Console.WriteLine(settings.ToString());
                            Patch(
                                settings,
                                patcher,
                                userPreferences);
                        }
                        catch (Exception ex)
                        {
                            System.Console.Error.WriteLine(ex);
                            return -1;
                        }
                        return 0;
                    },
                    _ =>
                    {
                        return -1;
                    });
        }

        /// <summary>
        /// Takes in the main line command arguments, and handles PatcherRunSettings CLI inputs.
        /// </summary>
        /// <typeparam name="TMod">Setter mod interface</typeparam>
        /// <typeparam name="TModGetter">Getter only mod interface</typeparam>
        /// <param name="settings">Patcher run settings</param>
        /// <param name="patcher">Patcher func that processes a load order, and returns a mod object to export.</param>
        /// <param name="userPreferences">Any custom user preferences</param>
        public async Task Patch<TMod, TModGetter>(
            RunSynthesisMutagenPatcher settings,
            AsyncPatcherFunction<TMod, TModGetter> patcher,
            UserPreferences? userPreferences = null)
            where TMod : class, IContextMod<TMod>, TModGetter
            where TModGetter : class, IContextGetterMod<TMod>
        {
            try
            {
                System.Console.WriteLine("Prepping state.");
                WarmupAll.Init();
                using var state = Utility.ToState<TMod, TModGetter>(settings, userPreferences?.ToPatcherPrefs() ?? new PatcherPreferences());
                System.Console.WriteLine("Running patch.");
                await patcher(state).ConfigureAwait(false);
                if (!settings.OutputPath.IsNullOrWhitespace())
                {
                    System.Console.WriteLine($"Writing to output: {settings.OutputPath}");
                    state.PatchMod.WriteToBinaryParallel(path: settings.OutputPath, param: GetWriteParams(state.RawLoadOrder.Select(x => x.ModKey)));
                }
            }
            catch (Exception ex)
            when (Environment.GetCommandLineArgs().Length == 0
                && (userPreferences?.ActionsForEmptyArgs?.BlockAutomaticExit ?? false))
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
        public void Patch<TMod, TModGetter>(
            RunSynthesisMutagenPatcher settings,
            PatcherFunction<TMod, TModGetter> patcher,
            UserPreferences? userPreferences = null)
            where TMod : class, IContextMod<TMod>, TModGetter
            where TModGetter : class, IContextGetterMod<TMod>
        {
            try
            {
                System.Console.WriteLine("Prepping state.");
                WarmupAll.Init();
                using var state = Utility.ToState<TMod, TModGetter>(settings, userPreferences?.ToPatcherPrefs() ?? new PatcherPreferences());
                System.Console.WriteLine("Running patch.");
                patcher(state);
                if (!settings.OutputPath.IsNullOrWhitespace())
                {
                    System.Console.WriteLine($"Writing to output: {settings.OutputPath}");
                    state.PatchMod.WriteToBinaryParallel(path: settings.OutputPath, param: GetWriteParams(state.RawLoadOrder.Select(x => x.ModKey)));
                }
            }
            catch (Exception ex)
            when (Environment.GetCommandLineArgs().Length == 0
                && (userPreferences?.ActionsForEmptyArgs?.BlockAutomaticExit ?? false))
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

        public IEnumerable<LoadOrderListing> GetLoadOrder(
            RunSynthesisMutagenPatcher settings,
            PatcherPreferences? userPrefs = null,
            bool throwOnMissingMods = true)
        {
            return GetLoadOrder(
                release: settings.GameRelease,
                loadOrderFilePath: settings.LoadOrderFilePath,
                dataFolderPath: settings.DataFolderPath,
                userPrefs: userPrefs,
                throwOnMissingMods: throwOnMissingMods);
        }

        public IEnumerable<LoadOrderListing> GetLoadOrder(
            GameRelease release,
            string loadOrderFilePath,
            string dataFolderPath,
            PatcherPreferences? userPrefs = null,
            bool throwOnMissingMods = true)
        {
            // This call will impliticly get Creation Club entries, too, as the Synthesis systems should be merging
            // things into a singular load order file for consumption here
            var loadOrderListing =
                ImplicitListings.GetListings(release, dataFolderPath)
                    .Select(x => new LoadOrderListing(x, enabled: true))
                .Concat(PluginListings.RawListingsFromPath(loadOrderFilePath, release))
                .Distinct(x => x.ModKey);
            if (userPrefs?.InclusionMods != null)
            {
                var inclusions = userPrefs.InclusionMods.ToHashSet();
                loadOrderListing = loadOrderListing
                    .Where(m => inclusions.Contains(m.ModKey));
            }
            if (userPrefs?.ExclusionMods != null)
            {
                var exclusions = userPrefs.ExclusionMods.ToHashSet();
                loadOrderListing = loadOrderListing
                    .Where(m => !exclusions.Contains(m.ModKey));
            }
            return loadOrderListing;
        }

        public static RunSynthesisMutagenPatcher GetDefaultRun(UserPreferences prefs, RunDefaultPatcher def)
        {
            var dataPath = Path.Combine(def.TargetRelease.ToWjGame().MetaData().GameLocation().ToString(), "Data");
            if (!PluginListings.TryGetListingsFile(def.TargetRelease, out var path))
            {
                throw new FileNotFoundException("Could not locate load order automatically.");
            }
            return new RunSynthesisMutagenPatcher()
            {
                DataFolderPath = dataPath,
                SourcePath = null,
                OutputPath = Path.Combine(dataPath, def.IdentifyingModKey.FileName),
                GameRelease = def.TargetRelease,
                LoadOrderFilePath = path.Path,
                ExtraDataFolder = Path.GetFullPath("./Data")
            };
        }
    }
}
