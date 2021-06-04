using System;
using System.Diagnostics;

namespace Synthesis.Bethesda.GUI.Services
{
    public interface INavigateTo
    {
        void Navigate(string path);
    }

    public class NavigateTo : INavigateTo
    {
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
                Log.Logger.Error(ex, $"Error navigating to path: {path}");
            }
        }
    }
}