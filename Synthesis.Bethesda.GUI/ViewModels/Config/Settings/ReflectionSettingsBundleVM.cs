using Noggog.Utility;
using Noggog.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public class ReflectionSettingsBundleVM : ViewModel
    {
        private readonly TempFolder? _tempFolder;

        public ICollection<ReflectionSettingsVM>? Settings { get; private set; }

        public ReflectionSettingsBundleVM(
            ICollection<ReflectionSettingsVM> settings,
            TempFolder tempFolder)
        {
            _tempFolder = tempFolder;
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
                    Log.Logger.Error(ex, "Could not clean up reflection settings");
                }
            }
        }
    }
}
