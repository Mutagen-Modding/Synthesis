namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Classification for access denied errors when running builds inside MO2's VFS.
/// This is triggered when an AccessDenied error is detected and we're running inside MO2.
/// Suggests both running outside MO2 and enabling the BlockBuildingWithinMo2 protection.
/// </summary>
public class RanBuildInMo2ErrorClassification : ErrorClassification
{
    public const string ErrorTypeString = "Mo2 Causing Build Errors";

    public string FilePath { get; }

    public RanBuildInMo2ErrorClassification(string filePath)
    {
        FilePath = filePath;
    }

    public override string? DiscussionLink => "https://github.com/Mutagen-Modding/Synthesis/discussions/562";
    public const string SuggestionMessage =
        "MO2's virtual file system (VFS) is causing build errors.\n\n" +
        "Please run Synthesis outside of Mod Organizer 2 when building patchers.  Once built, " +
        "you can open from within Mo2 for the actual patching run.";

    public override string ErrorType => ErrorTypeString;
    public override string Message => SuggestionMessage;
}

/// <summary>
/// Classification for when the BlockBuildingWithinMo2 setting actively blocked a build.
/// This is a gentler message since the user already has protection enabled.
/// </summary>
public class Mo2BuildBlockedErrorClassification : ErrorClassification
{
    public const string ErrorTypeString = "Ran Build In MO2";

    public override string? DiscussionLink => "https://github.com/Mutagen-Modding/Synthesis/discussions/562";
    public override string ErrorType => ErrorTypeString;
    public override string Message => "Build was blocked because Synthesis is running inside Mod Organizer 2, and you have the block builds setting on.\n\n" +
                                      "Run Synthesis once outside of MO2 to build the patchers and then try again.";
}
