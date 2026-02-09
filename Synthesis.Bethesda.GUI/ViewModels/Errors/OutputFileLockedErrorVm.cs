using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.GUI.Services.Main;

namespace Synthesis.Bethesda.GUI.ViewModels.Errors;

/// <summary>
/// View model wrapper for OutputFileLockedErrorClassification
/// </summary>
public class OutputFileLockedErrorVm : ErrorClassificationVm
{
    private readonly OutputFileLockedErrorClassification _error;

    // Expose specific error properties for binding
    public string FilePath => _error.FilePath;

    public delegate OutputFileLockedErrorVm Factory(OutputFileLockedErrorClassification error);

    public OutputFileLockedErrorVm(
        OutputFileLockedErrorClassification error,
        INavigateTo navigateTo)
        : base(error, navigateTo)
    {
        _error = error;
    }
}
