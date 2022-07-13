using System.Diagnostics;
using Serilog;

namespace Synthesis.Bethesda.GUI.Services.Main;

public interface INavigateTo
{
    void Navigate(string path);
}

public class NavigateTo : INavigateTo
{
    private readonly ILogger _logger;

    public NavigateTo(ILogger logger)
    {
        _logger = logger;
    }
        
    public void Navigate(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error navigating to path: {Path}", path);
        }
    }
}