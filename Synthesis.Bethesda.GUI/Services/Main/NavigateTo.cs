using System;
using System.Diagnostics;
using Serilog;

namespace Synthesis.Bethesda.GUI.Services.Main;

public interface INavigateTo
{
    void Navigate(string path);
}

public class NavigateTo : INavigateTo
{
    private readonly ILogger _Logger;

    public NavigateTo(ILogger logger)
    {
        _Logger = logger;
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
            _Logger.Error(ex, $"Error navigating to path: {path}");
        }
    }
}