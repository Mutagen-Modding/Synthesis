namespace Synthesis.Bethesda.GUI.Services
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