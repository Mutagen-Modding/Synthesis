namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Classification for access denied errors when running builds inside MO2's VFS
/// </summary>
public class RanBuildInMo2ErrorClassification : ErrorClassification
{
    public string FilePath { get; }

    public RanBuildInMo2ErrorClassification(string filePath)
    {
        FilePath = filePath;
    }

    public override string? DiscussionLink => "https://github.com/Mutagen-Modding/Synthesis/discussions/562";
    public override string ErrorType => "Ran Build In MO2";
    public override string Message => "MO2's virtual file system (VFS) is incompatible with .NET SDK builds. " +
                                      "The VFS cannot properly handle the file access patterns used during compilation.\n\n" +
                                      "Please run Synthesis outside of Mod Organizer 2 when building patchers.";
}
