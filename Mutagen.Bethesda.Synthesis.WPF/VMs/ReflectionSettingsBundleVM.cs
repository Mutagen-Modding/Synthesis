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
    }
}
