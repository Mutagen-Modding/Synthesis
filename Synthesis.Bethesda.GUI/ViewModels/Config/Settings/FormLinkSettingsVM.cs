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
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public class FormLinkSettingsVM : SettingsNodeVM
    {
        private readonly ObservableAsPropertyHelper<ILinkCache?> _LinkCache;
        public ILinkCache? LinkCache => _LinkCache.Value;

        [Reactive]
        public FormKey Value { get; set; }

        public IEnumerable<Type> ScopedTypes { get; } = Enumerable.Empty<Type>();

        public FormLinkSettingsVM(SettingsParameters param, string memberName, Type targetType) 
            : base(memberName)
        {
            _LinkCache = param.LinkCache
                .ToGuiProperty(this, nameof(LinkCache), default);
            ScopedTypes = targetType.GenericTypeArguments[0].AsEnumerable();
        }

        public override SettingsNodeVM Duplicate()
        {
            throw new NotImplementedException();
        }

        public override void Import(JsonElement property, ILogger logger)
        {
        }

        public override void Persist(JObject obj, ILogger logger)
        {
        }
    }
}
