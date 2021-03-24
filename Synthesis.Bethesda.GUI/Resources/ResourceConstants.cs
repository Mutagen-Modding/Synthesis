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
        public static readonly string OblivionIcon = Path.Combine(ResourceFolder, "Oblivion.png");
        public static readonly string SkyrimLEIcon = Path.Combine(ResourceFolder, "SkyrimLE.png");
        public static readonly string SkyrimSEIcon = Path.Combine(ResourceFolder, "SkyrimSE.png");
        public static readonly string SkyrimVRIcon = Path.Combine(ResourceFolder, "SkyrimVR.png");
        public static string GetIcon(GameRelease release)
        {
            return release switch
            {
                GameRelease.Oblivion => OblivionIcon,
                GameRelease.SkyrimLE => SkyrimLEIcon,
                GameRelease.SkyrimSE => SkyrimSEIcon,
                GameRelease.SkyrimVR => SkyrimVRIcon,
                _ => throw new NotImplementedException()
            };
        }
    }
}
