using Mutagen.Bethesda.Synthesis.CLI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Mutagen.Bethesda.Synthesis
{
    // Note:  Keep named as-is, as there's patchers out there that reference this class directly that would break

    /// <summary>
    /// A class housing all the tools, parameters, and entry points for a typical Synthesis patcher
    /// </summary>
    public class SynthesisState<TModSetter, TModGetter> : IPatcherState<TModSetter, TModGetter>
        where TModSetter : class, IMod, TModGetter
        where TModGetter : class, IModGetter
    {
        /// <inheritdoc />
        public LoadOrder<IModListing<TModGetter>> LoadOrder { get; }

        /// <inheritdoc />
        public IReadOnlyList<LoadOrderListing> RawLoadOrder { get; }

        /// <inheritdoc />
        public ILinkCache<TModSetter, TModGetter> LinkCache { get; }

        /// <inheritdoc />
        public TModSetter PatchMod { get; }
        IModGetter IPatcherState.PatchMod => PatchMod;

        /// <inheritdoc />
        public CancellationToken Cancel { get; } = CancellationToken.None;

        /// <inheritdoc />
        public string ExtraSettingsDataPath { get; } = string.Empty;

        /// <inheritdoc />
        public string? DefaultSettingsDataPath { get; }

        /// <inheritdoc />
        public string LoadOrderFilePath { get; }

        /// <inheritdoc />
        public string DataFolderPath { get; }

        /// <inheritdoc />
        public GameRelease GameRelease { get; }

        /// <inheritdoc />
        public string OutputPath { get; }

        /// <inheritdoc />
        public string? SourcePath { get; }

        public SynthesisState(
            RunSynthesisMutagenPatcher runArguments,
            IReadOnlyList<LoadOrderListing> rawLoadOrder,
            LoadOrder<IModListing<TModGetter>> loadOrder,
            ILinkCache<TModSetter, TModGetter> linkCache,
            TModSetter patchMod,
            string extraDataPath,
            string? defaultDataPath,
            CancellationToken cancellation)
        {
            LinkCache = linkCache;
            RawLoadOrder = rawLoadOrder;
            LoadOrder = loadOrder;
            PatchMod = patchMod;
            ExtraSettingsDataPath = extraDataPath;
            DefaultSettingsDataPath = defaultDataPath;
            Cancel = cancellation;
            LoadOrderFilePath = runArguments.LoadOrderFilePath;
            DataFolderPath = runArguments.DataFolderPath;
            GameRelease = runArguments.GameRelease;
            OutputPath = runArguments.OutputPath;
            SourcePath = runArguments.SourcePath;
        }

        public void Dispose()
        {
            LoadOrder.Dispose();
        }
    }
}
