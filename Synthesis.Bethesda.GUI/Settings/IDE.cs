using System.ComponentModel;

namespace Synthesis.Bethesda.GUI.Settings
{
    public enum IDE
    {
        [Description("None")]
        None,
        [Description("System Default")]
        SystemDefault,
        [Description("Visual Studio")]
        VisualStudio,
        [Description("Rider")]
        Rider,
        [Description("Visual Code")]
        VisualCode
    }
}