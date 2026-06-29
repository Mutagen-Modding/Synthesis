namespace Synthesis.Bethesda.GUI.Logging;

/// <summary>
/// Static preferences for logging configuration
/// </summary>
public static class LogPreferences
{
    /// <summary>
    /// When true, Log will create a dummy logger instead of logging to files.
    /// Set this to true before accessing Log.Logger in test scenarios.
    /// </summary>
    public static bool IsTesting { get; set; }
}
