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
        public IEnumerable<ReflectionSettingsVM> Settings { get; private set; } = Enumerable.Empty<ReflectionSettingsVM>();

        public ReflectionSettingsBundleVM(IEnumerable<ReflectionSettingsVM> settings)
        {
            Settings = settings;
        }

        public ReflectionSettingsBundleVM()
        {
        }
    }
}
