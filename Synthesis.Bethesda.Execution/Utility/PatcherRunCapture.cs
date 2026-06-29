namespace Synthesis.Bethesda.Execution.Utility;

/// <summary>
/// Simple capture class for holding patcher output and error streams
/// </summary>
public class PatcherRunCapture
{
    public List<string> Output { get; } = new();
    public List<string> Errors { get; } = new();
}
