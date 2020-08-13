using Mutagen.Bethesda;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Synthesis.Bethesda.GUI
{
    public static class ResourceConstants
    {
        public static readonly string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name!;
        public static readonly string ResourceFolder = "Resources";
        public static readonly string OblivionLargeIcon = Path.Combine(ResourceFolder, "Oblivion.png");
        public static readonly string SkyrimLeLargeIcon = Path.Combine(ResourceFolder, "SkyrimLE.png");
        public static readonly string SkyrimSseLargeIcon = Path.Combine(ResourceFolder, "SkyrimSSE.png");
        public static string GetIcon(GameRelease release)
        {
            return release switch
            {
                GameRelease.Oblivion => OblivionLargeIcon,
                GameRelease.SkyrimLE => SkyrimLeLargeIcon,
                GameRelease.SkyrimSE => SkyrimSseLargeIcon,
                _ => throw new NotImplementedException()
            };
        }
    }
}
