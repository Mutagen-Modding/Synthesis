using DynamicData;
using Mutagen.Bethesda;
using Mutagen.Bethesda.WPF;
using Newtonsoft.Json.Linq;
using Noggog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    public class EnumerableModKeySettingsVM : SettingsNodeVM
    {
        private readonly ModKey[] _defaultVal;
        public ObservableCollection<ModKeyItemViewModel> Values { get; } = new ObservableCollection<ModKeyItemViewModel>();
        public IObservable<IChangeSet<ModKey>> DetectedLoadOrder { get; }

        public EnumerableModKeySettingsVM(
            IObservable<IChangeSet<ModKey>> detectedLoadOrder,
            string memberName,
            IEnumerable<ModKey> defaultVal)
            : base(memberName)
        {
            _defaultVal = defaultVal.ToArray();
            DetectedLoadOrder = detectedLoadOrder;
            Values.SetTo(defaultVal.Select(i => new ModKeyItemViewModel(i)));
        }

        public static EnumerableModKeySettingsVM Factory(SettingsParameters param, string memberName, object? defaultVal)
        {
            return new EnumerableModKeySettingsVM(
                param.DetectedLoadOrder.Transform(x => x.Listing.ModKey),
                memberName,
                defaultVal as IEnumerable<ModKey> ?? Enumerable.Empty<ModKey>());
        }

        public override void Import(JsonElement property, ILogger logger)
        {
            Values.Clear();
            foreach (var elem in property.EnumerateArray())
            {
                if (ModKey.TryFromNameAndExtension(elem.GetString(), out var modKey))
                {
                    Values.Add(new ModKeyItemViewModel(modKey));
                }
                else
                {
                    Values.Add(new ModKeyItemViewModel(ModKey.Null));
                }
            }
        }

        public override void Persist(JObject obj, ILogger logger)
        {
            obj[MemberName] = new JArray(Values
                .Select(x =>
                {
                    if (x.ModKey.IsNull)
                    {
                        return string.Empty;
                    }
                    else
                    {
                        return x.ModKey.ToString();
                    }
                }).ToArray());
        }

        public override SettingsNodeVM Duplicate()
        {
            return new EnumerableModKeySettingsVM(DetectedLoadOrder, MemberName, _defaultVal);
        }
    }
}
