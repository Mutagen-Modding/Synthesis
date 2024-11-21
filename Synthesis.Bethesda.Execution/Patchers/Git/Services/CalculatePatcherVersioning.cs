using System.Text;
using Serilog;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Versioning;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Services;

public class CalculatePatcherVersioning
{
    private readonly ILogger _logger;

    public CalculatePatcherVersioning(ILogger logger)
    {
        _logger = logger;
    }
    
    public ActiveNugetVersioning Calculate(
        ActiveNugetVersioning profile,
        NugetVersionPair newest,
        PatcherNugetVersioningEnum mutaVersioning,
        string mutaManual, 
        PatcherNugetVersioningEnum synthVersioning, 
        string synthManual)
    {
        var sb = new StringBuilder();
        NugetsToUse mutagen, synthesis;
        if (mutaVersioning == PatcherNugetVersioningEnum.Profile)
        {
            sb.Append($"  Mutagen following profile: {profile.Mutagen}");
            mutagen = profile.Mutagen;
        }
        else
        {
            mutagen = new NugetsToUse(
                "Mutagen",
                mutaVersioning.ToNugetVersioningEnum(),
                mutaManual,
                newest.Mutagen);
            sb.Append($"  {mutagen}");
        }

        if (synthVersioning == PatcherNugetVersioningEnum.Profile)
        {
            sb.Append($"  Synthesis following profile: {profile.Synthesis}");
            synthesis = profile.Synthesis;
        }
        else
        {
            synthesis = new NugetsToUse(
                "Synthesis",
                synthVersioning.ToNugetVersioningEnum(),
                synthManual, 
                newest.Synthesis);
            sb.Append($"  {synthesis}");
        }

        _logger.Information(sb.ToString());
        return new ActiveNugetVersioning(
            Mutagen: mutagen,
            Synthesis: synthesis);
    }
}