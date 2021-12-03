using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.WPF.Reflection;
using Noggog;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.DTO;

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
        private readonly ReflectionSettingsVM.Factory _reflFactory;
        private readonly IExtractInfoFromProject _Extract;

        public ProvideReflectionSettingsBundle(
            ILogger logger,
            ReflectionSettingsVM.Factory reflFactory,
            IExtractInfoFromProject extract)
        {
            _Logger = logger;
            _reflFactory = reflFactory;
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
                                return _reflFactory(
                                    ReflectionSettingsParameters.FromType(
                                        detectedLoadOrder.ObserveOn(RxApp.MainThreadScheduler),
                                        linkCache,
                                        t,
                                        Activator.CreateInstance(t)),
                                    nickname: targets[index].Nickname,
                                    settingsSubPath: targets[index].Path);
                            }
                            catch (Exception ex)
                            {
                                _Logger.Error(ex, "Error creating reflected settings");
                                throw new ArgumentException($"Error creating reflected settings: {ex.Message}");
                            }
                        })
                        .NotNull()
                        .ToArray();
                }).ConfigureAwait(false);
            if (vms.Failed)
            {
                return vms.BubbleFailure<ReflectionSettingsBundleVm>();
            }
            await Task.WhenAll(vms.Value.Item.Select(vm => vm.Import(cancel))).ConfigureAwait(false);
            return new ReflectionSettingsBundleVm(vms.Value.Item, vms.Value.Temp, _Logger);
        }
    }
}