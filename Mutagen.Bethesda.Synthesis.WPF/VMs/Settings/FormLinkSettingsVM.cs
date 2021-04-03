using Mutagen.Bethesda;
using Newtonsoft.Json.Linq;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mutagen.Bethesda.Synthesis.WPF
{
    public class FormLinkSettingsVM : SettingsNodeVM, IBasicSettingsNodeVM
    {
        private readonly Type _targetType;
        private readonly IObservable<ILinkCache> _linkCache;
        private FormKey _defaultVal;

        private readonly ObservableAsPropertyHelper<ILinkCache?> _LinkCache;
        public ILinkCache? LinkCache => _LinkCache.Value;

        [Reactive]
        public FormKey Value { get; set; }

        public IEnumerable<Type> ScopedTypes { get; } = Enumerable.Empty<Type>();

        object IBasicSettingsNodeVM.Value => Value;

        [Reactive]
        public bool IsSelected { get; set; }

        private readonly ObservableAsPropertyHelper<string> _DisplayName;
        public string DisplayName => _DisplayName.Value;

        public FormLinkSettingsVM(IObservable<ILinkCache> linkCache, FieldMeta fieldMeta, Type targetType, FormKey defaultVal) 
            : base(fieldMeta)
        {
            _targetType = targetType;
            _defaultVal = defaultVal;
            Value = defaultVal;
            _linkCache = linkCache;
            _LinkCache = linkCache
                .ToGuiProperty(this, nameof(LinkCache), default);
            ScopedTypes = targetType.GenericTypeArguments[0].AsEnumerable();
            _DisplayName = this.WhenAnyValue(x => x.Value)
                .CombineLatest(this.WhenAnyValue(x => x.LinkCache),
                    (key, cache) =>
                    {
                        if (cache != null
                            && cache.TryResolveIdentifier(key, ScopedTypes, out var edid)
                            && edid != null)
                        {
                            return edid;
                        }
                        return key.ToString();
                    })
                .ToGuiProperty(this, nameof(DisplayName), string.Empty, deferSubscription: true);
        }

        public override SettingsNodeVM Duplicate()
        {
            return new FormLinkSettingsVM(_linkCache, Meta, _targetType, _defaultVal);
        }

        public override void Import(JsonElement property, Action<string> logger)
        {
            Value = FormKeySettingsVM.Import(property);
        }

        public override void Persist(JObject obj, Action<string> logger)
        {
            obj[Meta.DiskName] = JToken.FromObject(FormKeySettingsVM.Persist(Value));
        }

        public override void WrapUp()
        {
            _defaultVal = FormKeySettingsVM.StripOrigin(_defaultVal);
            Value = FormKeySettingsVM.StripOrigin(Value);
            base.WrapUp();
        }

        public static FormLinkSettingsVM Factory(IObservable<ILinkCache> linkCache, FieldMeta fieldMeta, Type targetType, object? defaultVal)
        {
            FormKey formKey = FormKey.Null;
            if (defaultVal != null)
            {
                formKey = FormKey.Factory(
                    defaultVal.GetType().GetPublicProperties().FirstOrDefault(m => m.Name == "FormKey")!.GetValue(defaultVal)!.ToString());
            }
            return new FormLinkSettingsVM(linkCache, fieldMeta, targetType, formKey);
        }
    }
}
