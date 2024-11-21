using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Services;

public class CalculateProfileVersioning
{
    public ActiveNugetVersioning Calculate(
        NugetVersioningEnum mutaVersioning, 
        string? mutaManual,
        string? newestMuta, 
        NugetVersioningEnum synthVersioning, 
        string? synthManual,
        string? newestSynth)
    {
        return new ActiveNugetVersioning(
            new NugetsToUse("Mutagen", mutaVersioning, mutaManual ?? newestMuta ?? string.Empty, newestMuta),
            new NugetsToUse("Synthesis", synthVersioning, synthManual ?? newestSynth ?? string.Empty, newestSynth));
    }
}