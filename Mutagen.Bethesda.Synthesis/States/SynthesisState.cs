using Mutagen.Bethesda.Plugins.Allocators;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Synthesis.CLI;
using System;
using System.Collections.Generic;
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
        public ILoadOrder<IModListing<TModGetter>> LoadOrder { get; }

        /// <inheritdoc />
        public IReadOnlyList<IModListingGetter> RawLoadOrder { get; }

        /// <inheritdoc />
        public ILinkCache<TModSetter, TModGetter> LinkCache { get; }

        /// <inheritdoc />
        public TModSetter PatchMod { get; }
        IMod IPatcherState.PatchMod => PatchMod;

        /// <inheritdoc />
        public CancellationToken Cancel { get; }

        /// <inheritdoc />
        public string ExtraSettingsDataPath { get; }

        /// <inheritdoc />
        public string? InternalDataPath { get; }

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

        IFormKeyAllocator? IPatcherState.FormKeyAllocator => _formKeyAllocator;

        private readonly IFormKeyAllocator? _formKeyAllocator;

        public SynthesisState(
            RunSynthesisMutagenPatcher runArguments,
            IReadOnlyList<IModListingGetter> rawLoadOrder,
            ILoadOrder<IModListing<TModGetter>> loadOrder,
            ILinkCache<TModSetter, TModGetter> linkCache,
            TModSetter patchMod,
            string extraDataPath,
            string? internalDataPath,
            string? defaultDataPath,
            CancellationToken cancellation,
            IFormKeyAllocator? formKeyAllocator)
        {
            LinkCache = linkCache;
            RawLoadOrder = rawLoadOrder;
            LoadOrder = loadOrder;
            PatchMod = patchMod;
            ExtraSettingsDataPath = extraDataPath;
            InternalDataPath = internalDataPath;
            DefaultSettingsDataPath = defaultDataPath;
            Cancel = cancellation;
            LoadOrderFilePath = runArguments.LoadOrderFilePath;
            DataFolderPath = runArguments.DataFolderPath;
            GameRelease = runArguments.GameRelease;
            OutputPath = runArguments.OutputPath;
            SourcePath = runArguments.SourcePath;
            _formKeyAllocator = formKeyAllocator;
        }

        public void Dispose()
        {
            LoadOrder.Dispose();
            if (_formKeyAllocator is IDisposable disp)
            {
                disp.Dispose();
            }
        }
    }
}
