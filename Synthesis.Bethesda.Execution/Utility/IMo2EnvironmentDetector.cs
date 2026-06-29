namespace Synthesis.Bethesda.Execution.Utility;

/// <summary>
/// Detects if the application is running within Mod Organizer 2
/// </summary>
public interface IMo2EnvironmentDetector
{
    /// <summary>
    /// Checks if the application is currently running within MO2 by examining environment variables
    /// </summary>
    /// <returns>True if running within MO2, false otherwise</returns>
    bool IsRunningInsideMo2();
}
