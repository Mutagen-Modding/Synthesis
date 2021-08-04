using System;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.WPF.Reflection;
using Noggog;
using Serilog;
using Synthesis.Bethesda.DTO;
using Synthesis.Bethesda.Execution.Patchers;
using Synthesis.Bethesda.Execution.Patchers.Common;

namespace Mutagen.Bethesda.Synthesis.WPF
{
    public interface IProvideReflectionSettingsBundle
    {
        Task<GetResponse<ReflectionSettingsBundleVm>> ExtractBundle(
            string projPath,
            ReflectionSettingsConfig[] targets,
            IObservable<IChangeSet<IModListingGetter>> detectedLoadOrder,
            IObservable<ILinkCache?> linkCache,
            CancellationToken cancel);
    }

    public class ProvideReflectionSettingsBundle : IProvideReflectionSettingsBundle
    {
        private readonly ILogger _Logger;
        private readonly IPatcherExtraDataPathProvider _extraDataPathProvider;
        private readonly IFileSystem _FileSystem;
        private readonly IExtractInfoFromProject _Extract;

        public ProvideReflectionSettingsBundle(
            ILogger logger,
            IPatcherExtraDataPathProvider extraDataPathProvider,
            IFileSystem fileSystem,
            IExtractInfoFromProject extract)
        {
            _Logger = logger;
            _extraDataPathProvider = extraDataPathProvider;
            _FileSystem = fileSystem;
            _Extract = extract;
        }
        
        public async Task<GetResponse<ReflectionSettingsBundleVm>> ExtractBundle(
            string projPath,
            ReflectionSettingsConfig[] targets,
            IObservable<IChangeSet<IModListingGetter>> detectedLoadOrder,
            IObservable<ILinkCache?> linkCache,
            CancellationToken cancel)
        {
            var vms = await _Extract.Extract<ReflectionSettingsVM[]>(
                projPath: projPath,
                cancel: cancel,
                getter: (assemb) =>
                {
                    return targets
                        .Select((s, index) =>
                        {
                            try
                            {
                                var t = assemb.GetType(s.TypeName);
                                if (t == null) return null;
                                return new ReflectionSettingsVM(
                                    ReflectionSettingsParameters.FromType(
                                        detectedLoadOrder,
                                        linkCache,
                                        t,
                                        Activator.CreateInstance(t)),
                                    nickname: targets[index].Nickname,
                                    settingsFolder: _extraDataPathProvider.Path,
                                    settingsSubPath: targets[index].Path,
                                    logger: _Logger,
                                    fileSystem: _FileSystem);
                            }
                            catch (Exception ex)
                            {
                                _Logger.Error($"Error creating reflected settings:", ex);
                                throw new ArgumentException($"Error creating reflected settings: {ex.Message}");
                            }
                        })
                        .NotNull()
                        .ToArray();
                });
            if (vms.Failed)
            {
                return vms.BubbleFailure<ReflectionSettingsBundleVm>();
            }
            await Task.WhenAll(vms.Value.Item.Select(vm => vm.Import(cancel)));
            return new ReflectionSettingsBundleVm(vms.Value.Item, vms.Value.Temp, _Logger);
        }
    }
}