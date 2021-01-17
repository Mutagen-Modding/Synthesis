using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class DecimalSettingsVM : BasicSettingsVM<decimal>
    {
        public DecimalSettingsVM(string memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public DecimalSettingsVM()
            : base(string.Empty, default)
        {
        }

        public override decimal Get(JsonElement property) => property.GetDecimal();

        public override decimal GetDefault() => default(decimal);
    }
}
