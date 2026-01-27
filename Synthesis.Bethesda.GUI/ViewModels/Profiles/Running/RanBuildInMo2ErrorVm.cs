using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.GUI.Services.Main;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

/// <summary>
/// View model wrapper for RanBuildInMo2ErrorClassification
/// </summary>
public class RanBuildInMo2ErrorVm : ErrorClassificationVm
{
    private readonly RanBuildInMo2ErrorClassification _error;

    // Expose specific error properties for binding
    public string FilePath => _error.FilePath;

    public delegate RanBuildInMo2ErrorVm Factory(RanBuildInMo2ErrorClassification error);

    public RanBuildInMo2ErrorVm(
        RanBuildInMo2ErrorClassification error,
        INavigateTo navigateTo)
        : base(error, navigateTo)
    {
        _error = error;
    }
}
