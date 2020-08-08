using CommandLine;
using Mutagen.Bethesda.Internals;
using Mutagen.Bethesda.Synthesis.Internal;
using Noggog;
using Synthesis.Bethesda;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            where TMod : class, IMod, TModGetter
            where TModGetter : class, IModGetter;

        public delegate Task AsyncPatcherFunction<TMod, TModGetter>(SynthesisState<TMod, TModGetter> state)
            where TMod : class, IMod, TModGetter
            where TModGetter : class, IModGetter;

        #region Patch
        /// <summary>
        /// Takes in the main line command arguments, and handles PatcherRunSettings CLI inputs.
        /// </summary>
        /// <typeparam name="TMod">Setter mod interface</typeparam>
        /// <typeparam name="TModGetter">Getter only mod interface</typeparam>
        /// <param name="args">Main command line args</param>
        /// <param name="patcher">Patcher func that processes a load order, and returns a mod object to export.</param>
        /// <param name="userPreferences">Any custom user preferences</param>
        /// <returns>Null if args resulted in no actions being taken.  Otherwise int error code of the operation</returns>
        public async Task<int?> Patch<TMod, TModGetter>(
            string[] args,
            AsyncPatcherFunction<TMod, TModGetter> patcher,
            UserPreferences? userPreferences = null)
            where TMod : class, IMod, TModGetter
            where TModGetter : class, IModGetter
        {
            return await Parser.Default.ParseArguments(args, typeof(RunSynthesisPatcher))
                .MapResult(
                    async (RunSynthesisPatcher settings) =>
                    {
                        try
                        {
                            await Patch(
                                settings,
                                patcher,
                                userPreferences);
                        }
                        catch (Exception)
                        {
                            return (int?)-1;
                        }
                        return (int?)0;
                    },
                    async _ => default(int?));
        }

        /// <summary>
        /// Takes in the main line command arguments, and handles PatcherRunSettings CLI inputs.
        /// </summary>
        /// <typeparam name="TMod">Setter mod interface</typeparam>
        /// <typeparam name="TModGetter">Getter only mod interface</typeparam>
        /// <param name="args">Main command line args</param>
        /// <param name="patcher">Patcher func that processes a load order, and returns a mod object to export.</param>
        /// <param name="userPreferences">Any custom user preferences</param>
        /// <returns>Null if args resulted in no actions being taken.  Otherwise int error code of the operation</returns>
        public int? Patch<TMod, TModGetter>(
            string[] args,
            PatcherFunction<TMod, TModGetter> patcher,
            UserPreferences? userPreferences = null)
            where TMod : class, IMod, TModGetter
            where TModGetter : class, IModGetter
        {
            return Parser.Default.ParseArguments(args, typeof(RunSynthesisPatcher))
                .MapResult(
                    (RunSynthesisPatcher settings) =>
                    {
                        try
                        {
                            Patch(
                                settings,
                                patcher,
                                userPreferences);
                        }
                        catch (Exception)
                        {
                            return -1;
                        }
                        return 0;
                    },
                    _ => default(int?));
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
            RunSynthesisPatcher settings,
            AsyncPatcherFunction<TMod, TModGetter> patcher,
            UserPreferences? userPreferences = null)
            where TMod : class, IMod, TModGetter
            where TModGetter : class, IModGetter
        {
            var state = Utility.ToState<TMod, TModGetter>(settings, userPreferences ?? new UserPreferences());
            await patcher(state).ConfigureAwait(false);
            state.PatchMod.WriteToBinary(path: settings.OutputPath);
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
            RunSynthesisPatcher settings,
            PatcherFunction<TMod, TModGetter> patcher,
            UserPreferences? userPreferences = null)
            where TMod : class, IMod, TModGetter
            where TModGetter : class, IModGetter
        {
            var state = Utility.ToState<TMod, TModGetter>(settings, userPreferences ?? new UserPreferences());
            patcher(state);
            state.PatchMod.WriteToBinary(path: settings.OutputPath);
        }
        #endregion

        public IEnumerable<ModKey> GetLoadOrder(
            RunSynthesisPatcher settings,
            UserPreferences? userPrefs = null,
            bool throwOnMissingMods = true)
        {
            return GetLoadOrder(
                loadOrderFilePath: settings.LoadOrderFilePath,
                dataFolderPath: settings.DataFolderPath,
                userPrefs: userPrefs,
                throwOnMissingMods: throwOnMissingMods);
        }

        public IEnumerable<ModKey> GetLoadOrder(
            string loadOrderFilePath,
            string dataFolderPath,
            UserPreferences? userPrefs = null,
            bool throwOnMissingMods = true)
        {
            var loadOrderListing = LoadOrder.FromPath(loadOrderFilePath);
            loadOrderListing = LoadOrder.AlignToTimestamps(
                loadOrderListing,
                dataFolderPath,
                throwOnMissingMods: throwOnMissingMods);
            if (userPrefs?.InclusionMods != null)
            {
                var inclusions = userPrefs.InclusionMods.ToHashSet();
                loadOrderListing = loadOrderListing
                    .Where(m => inclusions.Contains(m))
                    .ToExtendedList();
            }
            if (userPrefs?.ExclusionMods != null)
            {
                var exclusions = userPrefs.ExclusionMods.ToHashSet();
                loadOrderListing = loadOrderListing
                    .Where(m => !exclusions.Contains(m))
                    .ToExtendedList();
            }
            return loadOrderListing;
        }
    }
}
