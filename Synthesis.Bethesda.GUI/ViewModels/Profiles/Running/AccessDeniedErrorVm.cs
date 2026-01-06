using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.GUI.Services.Main;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

/// <summary>
/// View model wrapper for AccessDeniedErrorClassification
/// </summary>
public class AccessDeniedErrorVm : ErrorClassificationVm
{
    private readonly AccessDeniedErrorClassification _error;

    // Expose specific error properties for binding
    public string FilePath => _error.FilePath;

    public delegate AccessDeniedErrorVm Factory(AccessDeniedErrorClassification error);

    public AccessDeniedErrorVm(
        AccessDeniedErrorClassification error,
        INavigateTo navigateTo)
        : base(error, navigateTo)
    {
        _error = error;
    }
}
