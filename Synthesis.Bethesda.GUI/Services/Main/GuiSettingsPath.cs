using Noggog;
using Noggog.IO;

namespace Synthesis.Bethesda.GUI.Services.Main;

public interface IGuiSettingsPath
{
    FilePath Path { get; }
}

public class GuiSettingsPath : IGuiSettingsPath
{
    private readonly ICurrentDirectoryProvider _currentDirectoryProvider;

    public GuiSettingsPath(ICurrentDirectoryProvider currentDirectoryProvider)
    {
        _currentDirectoryProvider = currentDirectoryProvider;
    }

    public FilePath Path => System.IO.Path.Combine(_currentDirectoryProvider.CurrentDirectory, "GuiSettings.json");
}