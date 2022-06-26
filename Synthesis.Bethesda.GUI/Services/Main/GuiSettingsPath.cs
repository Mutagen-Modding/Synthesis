using Noggog;

namespace Synthesis.Bethesda.GUI.Services.Main;

public interface IGuiSettingsPath
{
    FilePath Path { get; }
}

public class GuiSettingsPath : IGuiSettingsPath
{
    public FilePath Path { get; } = "GuiSettings.json";
}