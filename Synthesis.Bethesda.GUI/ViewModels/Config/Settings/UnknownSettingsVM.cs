using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class UnknownSettingsVM : SettingsNodeVM
    {
        public UnknownSettingsVM(FieldMeta fieldMeta)
            : base(fieldMeta)
        {
        }

        public override void Import(JsonElement property, ILogger logger)
        {
            logger.Error($"Tried to import for unknown setting: {this.Meta}");
        }

        public override void Persist(JObject obj, ILogger logger)
        {
        }

        public override SettingsNodeVM Duplicate() => this;
    }
}
