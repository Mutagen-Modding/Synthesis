using DynamicData;
using Mutagen.Bethesda;
using Mutagen.Bethesda.WPF;
using Newtonsoft.Json.Linq;
using Noggog;
using Noggog.WPF;
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
    public class EnumerableFormLinkSettingsVM : SettingsNodeVM
    {
        public ObservableCollection<FormKeyItemViewModel> Values { get; } = new ObservableCollection<FormKeyItemViewModel>();

        private readonly IObservable<ILinkCache> _linkCacheObs;
        private readonly ObservableAsPropertyHelper<ILinkCache?> _LinkCache;
        public ILinkCache? LinkCache => _LinkCache.Value;

        public IEnumerable<Type> ScopedTypes { get; }

        public EnumerableFormLinkSettingsVM(IObservable<ILinkCache> linkCache, Type type, string memberName)
            : base(memberName)
        {
            ScopedTypes = type.AsEnumerable();
            _linkCacheObs = linkCache;
            _LinkCache = linkCache
                .ToGuiProperty(this, nameof(LinkCache), default(ILinkCache?));
        }

        public static SettingsNodeVM Factory(SettingsParameters param, string memberName, Type type, object? defaultVal)
        {
            var ret = new EnumerableFormLinkSettingsVM(param.LinkCache, type, memberName);
            if (defaultVal is IEnumerable<IFormLink> items)
            {
                ret.Values.SetTo(items.Select(i => new FormKeyItemViewModel(i.FormKey)));
            }
            return ret;
        }

        public override void Import(JsonElement property, ILogger logger)
        {
            Values.Clear();
            foreach (var elem in property.EnumerateArray())
            {
                if (FormKey.TryFactory(elem.GetString(), out var formKey))
                {
                    Values.Add(new FormKeyItemViewModel(formKey));
                }
                else
                {
                    Values.Add(new FormKeyItemViewModel(FormKey.Null));
                }
            }
        }

        public override void Persist(JObject obj, ILogger logger)
        {
            obj[MemberName] = new JArray(Values
                .Select(x =>
                {
                    if (x.FormKey.IsNull)
                    {
                        return string.Empty;
                    }
                    else
                    {
                        return x.FormKey.ToString();
                    }
                }).ToArray());
        }

        public override SettingsNodeVM Duplicate()
        {
            return new EnumerableFormLinkSettingsVM(_linkCacheObs, ScopedTypes.First(), string.Empty);
        }
    }
}
