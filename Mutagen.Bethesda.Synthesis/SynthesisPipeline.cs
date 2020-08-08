using CommandLine;
using Mutagen.Bethesda.Internals;
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

        /// <summary>
        /// Takes in the main line command arguments, and handles PatcherRunSettings CLI inputs.
        /// </summary>
        /// <typeparam name="TMod">Setter mod interface</typeparam>
        /// <typeparam name="TModGetter">Getter only mod interface</typeparam>
        /// <param name="args">Main command line args</param>
        /// <param name="importer">Func to create a TModGetter</param>
        /// <param name="patcher">Patcher func that processes a load order, and returns a mod object to export.</param>
        /// <returns>Mod object to export as the result of this patch</returns>
        public async Task<bool> Patch<TMod, TModGetter>(
            string[] args,
            AsyncPatcherFunction<TMod, TModGetter> patcher)
            where TMod : class, IMod, TModGetter
            where TModGetter : class, IModGetter
        {
            return await Parser.Default.ParseArguments(args, typeof(RunSynthesisPatcher))
                .MapResult(
                    async (RunSynthesisPatcher settings) =>
                    {
                        await Patch(
                            settings,
                            patcher);
                        return true;
                    },
                    async _ => false);
        }

        /// <summary>
        /// Takes in the main line command arguments, and handles PatcherRunSettings CLI inputs.
        /// </summary>
        /// <typeparam name="TMod">Setter mod interface</typeparam>
        /// <typeparam name="TModGetter">Getter only mod interface</typeparam>
        /// <param name="args">Main command line args</param>
        /// <param name="importer">Func to create a TModGetter</param>
        /// <param name="patcher">Patcher func that processes a load order, and returns a mod object to export.</param>
        /// <returns>Mod object to export as the result of this patch</returns>
        public bool Patch<TMod, TModGetter>(
            string[] args,
            PatcherFunction<TMod, TModGetter> patcher)
            where TMod : class, IMod, TModGetter
            where TModGetter : class, IModGetter
        {
            return Parser.Default.ParseArguments(args, typeof(RunSynthesisPatcher))
                .MapResult(
                    (RunSynthesisPatcher settings) =>
                    {
                        Patch(
                            settings,
                            patcher);
                        return true;
                    },
                    _ => false);
        }

        public static SynthesisState<TMod, TModGetter> ToState<TMod, TModGetter>(RunSynthesisPatcher settings)
            where TMod : class, IMod, TModGetter
            where TModGetter : class, IModGetter
        {
            var loadOrderListing = LoadOrder.FromPath(settings.LoadOrderFilePath);
            loadOrderListing = LoadOrder.AlignToTimestamps(loadOrderListing, settings.DataFolderPath, throwOnMissingMods: true);
            var loadOrder = LoadOrder.Import<TModGetter>(
                settings.DataFolderPath,
                loadOrderListing,
                settings.GameRelease);
            var modKey = ModKey.Factory(Path.GetFileName(settings.OutputPath));
            TMod patchMod;
            ILinkCache cache;
            if (settings.SourcePath == null)
            {
                patchMod = ModInstantiator<TMod>.Activator(modKey, settings.GameRelease);
            }
            else
            {
                patchMod = ModInstantiator<TMod>.Importer(new ModPath(modKey, settings.SourcePath), settings.GameRelease);
            }
            cache = loadOrder.ToMutableLinkCache(patchMod);
            loadOrder.Add(new ModListing<TModGetter>(patchMod));
            return new SynthesisState<TMod, TModGetter>(settings, loadOrder, cache, patchMod);
        }

        /// <summary>
        /// Takes in the main line command arguments, and handles PatcherRunSettings CLI inputs.
        /// </summary>
        /// <typeparam name="TMod">Setter mod interface</typeparam>
        /// <typeparam name="TModGetter">Getter only mod interface</typeparam>
        /// <param name="settings">Patcher run settings</param>
        /// <param name="importer">Func to create a TModGetter</param>
        /// <param name="patcher">Patcher func that processes a load order, and returns a mod object to export.</param>
        /// <returns>Mod object to export as the result of this patch</returns>
        public async Task Patch<TMod, TModGetter>(
            RunSynthesisPatcher settings,
            AsyncPatcherFunction<TMod, TModGetter> patcher)
            where TMod : class, IMod, TModGetter
            where TModGetter : class, IModGetter
        {
            var state = ToState<TMod, TModGetter>(settings);
            await patcher(state).ConfigureAwait(false);
            state.PatchMod.WriteToBinary(path: settings.OutputPath);
        }

        /// <summary>
        /// Takes in the main line command arguments, and handles PatcherRunSettings CLI inputs.
        /// </summary>
        /// <typeparam name="TMod">Setter mod interface</typeparam>
        /// <typeparam name="TModGetter">Getter only mod interface</typeparam>
        /// <param name="settings">Patcher run settings</param>
        /// <param name="importer">Func to create a TModGetter</param>
        /// <param name="patcher">Patcher func that processes a load order, and returns a mod object to export.</param>
        /// <returns>Mod object to export as the result of this patch</returns>
        public void Patch<TMod, TModGetter>(
            RunSynthesisPatcher settings,
            PatcherFunction<TMod, TModGetter> patcher)
            where TMod : class, IMod, TModGetter
            where TModGetter : class, IModGetter
        {
            var state = ToState<TMod, TModGetter>(settings);
            patcher(state);
            state.PatchMod.WriteToBinary(path: settings.OutputPath);
        }
    }
}
