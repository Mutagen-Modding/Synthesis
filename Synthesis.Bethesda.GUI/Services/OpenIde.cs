using System;
using System.Diagnostics;
using System.IO;
using Synthesis.Bethesda.GUI.Settings;

namespace Synthesis.Bethesda.GUI.Services
{
    public interface IOpenIde
    {
        void OpenSolution(string path, IDE option);
    }

    public class OpenIde : IOpenIde
    {
        private readonly IIdeLocator _Locator;

        public OpenIde(IIdeLocator locator)
        {
            _Locator = locator;
        }
        
        public void OpenSolution(string path, IDE option)
        {
            switch (option)
            {
                case IDE.None:
                    break;
                case IDE.SystemDefault:
                {
                    Process.Start(new ProcessStartInfo(path)
                    {
                        UseShellExecute = true,
                    });
                    break;
                }
                case IDE.VisualStudio:
                {
                    if (_Locator.VSPath == null)
                        break;

                    var info = new ProcessStartInfo
                    {
                        FileName = _Locator.VSPath,
                        ArgumentList = { path },
                        UseShellExecute = true
                    };

                    Process.Start(info);
                    break;
                }
                case IDE.Rider:
                {
                    if (_Locator.RiderPath == null)
                        break;

                    var info = new ProcessStartInfo
                    {
                        FileName = _Locator.RiderPath,
                        ArgumentList = { path },
                        UseShellExecute = true
                    };

                    Process.Start(info);

                    break;
                }
                case IDE.VisualCode:
                {
                    var info = new ProcessStartInfo
                    {
                        FileName = "code",
                        ArgumentList = { "." },
                        WorkingDirectory = Path.GetDirectoryName(path)!,
                        UseShellExecute = true
                    };
                    Process.Start(info);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}