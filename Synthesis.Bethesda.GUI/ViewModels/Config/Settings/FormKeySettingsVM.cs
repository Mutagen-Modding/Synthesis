using Mutagen.Bethesda;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public class FormKeySettingsVM : BasicSettingsVM<FormKey>
    {
        public FormKeySettingsVM(FieldMeta fieldMeta, object? defaultVal)
            : base(fieldMeta, defaultVal is FormKey form ? StripOrigin(form) : null)
        {
        }

        public FormKeySettingsVM()
            : base(FieldMeta.Empty, default)
        {
        }

        public override SettingsNodeVM Duplicate() => new FormKeySettingsVM(Meta, DefaultValue);

        public override FormKey Get(JsonElement property)
        {
            return FormKey.Factory(property.GetString());
        }

        public override FormKey GetDefault() => FormKey.Null;

        public override void Import(JsonElement property, ILogger logger)
        {
            Value = Import(property);
        }

        public static FormKey Import(JsonElement property)
        {
            if (FormKey.TryFactory(property.GetString(), out var formKey))
            {
                return formKey;
            }
            else
            {
                return FormKey.Null;
            }
        }

        public override void Persist(JObject obj, ILogger logger)
        {
            obj[Meta.DiskName] = JToken.FromObject(Persist(Value));
        }

        public static string Persist(FormKey formKey)
        {
            if (formKey.IsNull)
            {
                return string.Empty;
            }
            else
            {
                return formKey.ToString();
            }
        }

        public static FormKey StripOrigin(FormKey formKey)
        {
            return FormKey.Factory(formKey.ToString());
        }

        public override void WrapUp()
        {
            Value = StripOrigin(Value);
        }
    }
}
