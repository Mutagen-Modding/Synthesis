namespace Synthesis.Bethesda.GUI.Services.Main
{
    public interface IGuiSettingsPath
    {
        string Path { get; }
    }

    public class GuiSettingsPath : IGuiSettingsPath
    {
        public string Path { get; } = "GuiSettings.json";
    }
}