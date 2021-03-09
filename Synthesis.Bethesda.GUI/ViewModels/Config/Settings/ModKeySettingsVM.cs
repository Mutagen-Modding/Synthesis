using DynamicData;
using Mutagen.Bethesda;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public class ModKeySettingsVM : BasicSettingsVM<ModKey>
    {
        public IObservable<IChangeSet<ModKey>> DetectedLoadOrder { get; }

        public ModKeySettingsVM(
            IObservable<IChangeSet<ModKey>> detectedLoadOrder,
            FieldMeta fieldMeta, 
            object? defaultVal)
            : base(fieldMeta, defaultVal)
        {
            DetectedLoadOrder = detectedLoadOrder;
        }

        public override SettingsNodeVM Duplicate() => new ModKeySettingsVM(DetectedLoadOrder, Meta, DefaultValue);

        public override ModKey Get(JsonElement property)
        {
            return ModKey.FromNameAndExtension(property.GetString());
        }

        public override ModKey GetDefault() => ModKey.Null;

        public override void Import(JsonElement property, Action<string> logger)
        {
            Value = Import(property);
        }

        public static ModKey Import(JsonElement property)
        {
            if (ModKey.TryFromNameAndExtension(property.GetString(), out var modKey))
            {
                return modKey;
            }
            else
            {
                return ModKey.Null;
            }
        }

        public override void Persist(JObject obj, Action<string> logger)
        {
            if (Value.IsNull)
            {
                obj[Meta.DiskName] = JToken.FromObject(string.Empty);
            }
            else
            {
                obj[Meta.DiskName] = JToken.FromObject(Value.ToString());
            }
        }

        public static string Persist(ModKey modKey)
        {
            if (modKey.IsNull)
            {
                return string.Empty;
            }
            else
            {
                return modKey.ToString();
            }
        }
    }
}
