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

    /// <summary>
    /// Optional URL to a discussion or documentation page for more information
    /// </summary>
    public virtual string? DiscussionLink => null;
}
