using Mutagen.Bethesda;
using System.IO;
using System.Reflection;

namespace Synthesis.Bethesda.GUI;

public static class ResourceConstants
{
    public static readonly string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name!;
    public static readonly string ResourceFolder = "Resources";
    public static readonly string OblivionLargeIcon = Path.Combine(ResourceFolder, "Oblivion.png");
    public static readonly string SkyrimLeLargeIcon = Path.Combine(ResourceFolder, "SkyrimLE.png");
    public static readonly string SkyrimSseLargeIcon = Path.Combine(ResourceFolder, "SkyrimSSE.png");
    public static readonly string SkyrimVrLargeIcon = Path.Combine(ResourceFolder, "SkyrimVR.png");
    public static readonly string FalloutLargeIcon = Path.Combine(ResourceFolder, "Fallout4.png");
    public static readonly string EnderalLeLargeIcon = Path.Combine(ResourceFolder, "enderal.png");
    public static readonly string EnderalSeLargeIcon = Path.Combine(ResourceFolder, "enderal-se.png");
    public static string GetIcon(GameRelease release)
    {
        return release switch
        {
            GameRelease.Oblivion => OblivionLargeIcon,
            GameRelease.SkyrimLE => SkyrimLeLargeIcon,
            GameRelease.SkyrimSE => SkyrimSseLargeIcon,
            GameRelease.SkyrimSEGog => SkyrimSseLargeIcon,
            GameRelease.SkyrimVR => SkyrimVrLargeIcon,
            GameRelease.Fallout4 => FalloutLargeIcon,
            GameRelease.EnderalLE => EnderalLeLargeIcon,
            GameRelease.EnderalSE => EnderalSeLargeIcon,
            _ => throw new NotImplementedException()
        };
    }
}