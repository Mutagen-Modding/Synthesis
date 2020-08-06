using DynamicData;
using DynamicData.Binding;
using Synthesis.Bethesda.Execution.Settings;
using Newtonsoft.Json;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    public class MainVM : ViewModel
    {
        public ConfigurationVM Configuration { get; }

        public MainVM()
        {
            Configuration = new ConfigurationVM(this);
        }

        public void Load(SynthesisSettings? settings)
        {
            if (settings == null) return;
            Configuration.Load(settings);
        }

        public SynthesisSettings Save() => Configuration.Save();
    }
}
