using Noggog.Utility;
using Noggog.WPF;
using System;
using System.Collections.Generic;
using Serilog;

namespace Mutagen.Bethesda.Synthesis.WPF
{
    public class ReflectionSettingsBundleVm : ViewModel
    {
        private readonly ILogger? _logger;
        private readonly TempFolder? _tempFolder;

        public ICollection<ReflectionSettingsVM>? Settings { get; private set; }

        public ReflectionSettingsBundleVm(
            ICollection<ReflectionSettingsVM> settings,
            TempFolder tempFolder,
            ILogger logger)
        {
            _tempFolder = tempFolder;
            _logger = logger;
            Settings = settings;
        }

        public ReflectionSettingsBundleVm()
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
                    _logger?.Error($"Could not clean up reflection settings:", ex);
                }
            }
        }
    }
}
