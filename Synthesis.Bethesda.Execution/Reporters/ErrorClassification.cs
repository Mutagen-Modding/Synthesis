namespace Synthesis.Bethesda.Execution.Reporters;

/// <summary>
/// Represents a classified error with additional context and suggestions
/// </summary>
public abstract class ErrorClassification
{
    /// <summary>
    /// Human-readable description of the error type
    /// </summary>
    public abstract string ErrorType { get; }

    /// <summary>
    /// Detailed message to display to the user
    /// </summary>
    public abstract string Message { get; }
}

/// <summary>
/// Classification for TooManyMasters errors
/// </summary>
public class TooManyMastersError : ErrorClassification
{
    public const string SuggestionMessage = "The output plugin has too many masters. Consider enabling 'SplitIfMaxMastersExceeded' in your profile settings to automatically split the output into multiple plugins.";

    public override string ErrorType => "TooManyMasters";
    public override string Message => SuggestionMessage;
}

/// <summary>
/// Classification for ReferencedModMissing errors
/// </summary>
public class ReferencedModMissingError : ErrorClassification
{
    public const string SuggestionMessage = "A referenced mod was not present on the load order being sorted against. This typically happens when a patcher references a mod that isn't in your current load order. Check your load order and ensure all required mods are present and enabled.";

    public override string ErrorType => "ReferencedModMissing";
    public override string Message => SuggestionMessage;
}
