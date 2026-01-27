namespace Synthesis.Bethesda.Execution.Utility;

/// <summary>
/// Detects if the application is running within Mod Organizer 2 by checking for MO2-specific environment variables
/// </summary>
public class Mo2EnvironmentDetector : IMo2EnvironmentDetector
{
    private static readonly string[] Mo2EnvironmentVariables =
    {
        "MO_DATAPATH",
        "MO_GAMEPATH",
        "MO_PROFILE",
        "MO_PROFILEDIR",
        "MO_MODSDIR",
        "USVFS_LOGFILE",
        "VIRTUAL_STORE"
    };

    /// <inheritdoc />
    public bool IsRunningInsideMo2()
    {
        foreach (var variable in Mo2EnvironmentVariables)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            if (!string.IsNullOrEmpty(value))
            {
                return true;
            }
        }

        return false;
    }
}
