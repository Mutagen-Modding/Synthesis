// /*
//     Copyright (C) 2020  erri120
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <https://www.gnu.org/licenses/>.
// */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace Synthesis.Bethesda.GUI
{
    public enum OpenWithEnum
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

    public static class OpenWithProgram
    {
        private static readonly string? RiderPath;
        private static readonly string? VSPath;
        
        static OpenWithProgram()
        {
            #region Visual Studio

            {
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                var vsMainDir = Path.Combine(programFiles, "Microsoft Visual Studio");
                List<int>? versions = Directory.EnumerateDirectories(vsMainDir, "*", SearchOption.TopDirectoryOnly)
                    .Select(x =>
                    {
                        ReadOnlySpan<char> span = x.AsSpan(vsMainDir[^1] == Path.PathSeparator ? vsMainDir.Length : vsMainDir.Length+1);
                        if(int.TryParse(span.ToString(), out var y))
                            return y;
                        return -1;
                    })
                    .Where(x => x != -1)
                    .ToList();
                var currentVersion = versions.Max();
                //TODO: assuming Community Version for now
                var exePath = Path.Combine(vsMainDir, $"{currentVersion}", "Community", "Common7", "IDE", "devenv.exe");
                if (File.Exists(exePath))
                    VSPath = exePath;
            }

            #endregion
            
            #region Rider
            {
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var defaultRiderLocation = Path.Combine(programFiles, "JetBrains", "JetBrains Rider", "bin");
                if (Directory.Exists(defaultRiderLocation))
                {
                    var exePath = Path.Combine(defaultRiderLocation, "rider64.exe");
                    if(File.Exists(exePath))
                        RiderPath = exePath;
                }
                else
                {
                    const string toolBoxRegKey = @"Software\JetBrains\Toolbox";
                    var toolBoxKey = Registry.CurrentUser.OpenSubKey(toolBoxRegKey);
                    var toolboxBinPath = toolBoxKey?.GetValue("");
                    if (toolboxBinPath == null)
                        return;

                    var toolBoxPath = Path.GetDirectoryName(toolboxBinPath.ToString());

                    const string riderVersionRegKey = @"Software\JetBrains\Rider";
                    var riderVersionKey = Registry.CurrentUser.OpenSubKey(riderVersionRegKey);
                    var riderVersion = riderVersionKey?.GetValue("");
                    if (riderVersion == null)
                        return;

                    var exePath = Path.Combine(toolBoxPath ?? string.Empty, "apps", "Rider", "ch-0",
                        riderVersion.ToString() ?? string.Empty, "bin", "rider64.exe");
                        
                    if(File.Exists(exePath))
                        RiderPath = exePath;
                }
            }

            #endregion
        }
        
        public static void OpenSolution(string path, OpenWithEnum option)
        {
            switch (option)
            {
                case OpenWithEnum.None:
                    break;
                case OpenWithEnum.SystemDefault:
                {
                    Process.Start(new ProcessStartInfo(path)
                    {
                        UseShellExecute = true,
                    });
                    break;
                }
                case OpenWithEnum.VisualStudio:
                {
                    if (VSPath == null)
                        break;
                    
                    var info = new ProcessStartInfo
                    {
                        FileName = VSPath,
                        ArgumentList = { path },
                        UseShellExecute = true
                    };

                    Process.Start(info);
                    break;
                }
                case OpenWithEnum.Rider:
                {
                    if (RiderPath == null)
                        break;

                    var info = new ProcessStartInfo
                    {
                        FileName = RiderPath,
                        ArgumentList = { path },
                        UseShellExecute = true
                    };

                    Process.Start(info);
                    
                    break;
                }
                case OpenWithEnum.VisualCode:
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