using DynamicData;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.WPF.Reflection;
using Noggog;
using Noggog.Utility;
using Noggog.WPF;
using Synthesis.Bethesda.DTO;
using Synthesis.Bethesda.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins.Order;

namespace Mutagen.Bethesda.Synthesis.WPF
{
    public class ReflectionSettingsBundleVM : ViewModel
    {
        private readonly Action<string>? _log;
        private readonly TempFolder? _tempFolder;

        public ICollection<ReflectionSettingsVM>? Settings { get; private set; }

        public ReflectionSettingsBundleVM(
            ICollection<ReflectionSettingsVM> settings,
            TempFolder tempFolder,
            Action<string> log)
        {
            _tempFolder = tempFolder;
            _log = log;
            Settings = settings;
        }

        public ReflectionSettingsBundleVM()
        {
        }

        public override void Dispose()
        {
            base.Dispose();
            Settings = null;

            if (_tempFolder != null)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                try
                {
                    _tempFolder.Dispose();
                }
                catch (Exception ex)
                {
                    _log?.Invoke($"Could not clean up reflection settings: {ex}");
                }
            }
        }

        public static async Task<GetResponse<ReflectionSettingsBundleVM>> ExtractBundle(
            string projPath,
            string displayName,
            ReflectionSettingsConfig[] targets,
            IObservable<IChangeSet<IModListingGetter>> detectedLoadOrder,
            IObservable<ILinkCache?> linkCache,
            Action<string> log, 
            CancellationToken cancel)
        {
            var vms = await ReflectionUtility.ExtractInfoFromProject<ReflectionSettingsVM[]>(
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
                                    settingsFolder: Path.Combine(Paths.TypicalExtraData, displayName),
                                    settingsSubPath: targets[index].Path);
                            }
                            catch (Exception ex)
                            {
                                log(ex.ToString());
                                throw new ArgumentException($"Error creating reflected settings: {ex.Message}");
                            }
                        })
                        .NotNull()
                        .ToArray();
                },
                log);
            if (vms.Failed)
            {
                return vms.BubbleFailure<ReflectionSettingsBundleVM>();
            }
            await Task.WhenAll(vms.Value.Item.Select(vm => vm.Import(log, cancel)));
            return new ReflectionSettingsBundleVM(vms.Value.Item, vms.Value.Temp, log);
        }
    }
}
