using Mutagen.Bethesda;
using Newtonsoft.Json.Linq;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
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

        public FormLinkSettingsVM(IObservable<ILinkCache> linkCache, SettingsMeta memberName, Type targetType, FormKey defaultVal) 
            : base(memberName)
        {
            _targetType = targetType;
            _defaultVal = defaultVal;
            Value = defaultVal;
            _linkCache = linkCache;
            _LinkCache = linkCache
                .ToGuiProperty(this, nameof(LinkCache), default);
            ScopedTypes = targetType.GenericTypeArguments[0].AsEnumerable();
        }

        public override SettingsNodeVM Duplicate()
        {
            return new FormLinkSettingsVM(_linkCache, Meta, _targetType, _defaultVal);
        }

        public override void Import(JsonElement property, ILogger logger)
        {
            Value = FormKeySettingsVM.Import(property);
        }

        public override void Persist(JObject obj, ILogger logger)
        {
            obj[Meta.DiskName] = JToken.FromObject(FormKeySettingsVM.Persist(Value));
        }

        public override void WrapUp()
        {
            _defaultVal = FormKeySettingsVM.StripOrigin(_defaultVal);
            Value = FormKeySettingsVM.StripOrigin(Value);
            base.WrapUp();
        }

        public static FormLinkSettingsVM Factory(IObservable<ILinkCache> linkCache, SettingsMeta memberName, Type targetType, object? defaultVal)
        {
            var formLink = defaultVal as IFormLink;
            return new FormLinkSettingsVM(linkCache, memberName, targetType, formLink?.FormKey ?? FormKey.Null);
        }
    }
}
