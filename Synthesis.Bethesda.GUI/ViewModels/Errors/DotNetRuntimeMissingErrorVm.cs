using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.GUI.Services.Main;

namespace Synthesis.Bethesda.GUI.ViewModels.Errors;

/// <summary>
/// View model wrapper for DotNetRuntimeMissingErrorClassification
/// </summary>
public class DotNetRuntimeMissingErrorVm : ErrorClassificationVm
{
    public string? RequiredVersion { get; }
    public bool HasRequiredVersion => RequiredVersion != null;

    public delegate DotNetRuntimeMissingErrorVm Factory(DotNetRuntimeMissingErrorClassification error);

    public DotNetRuntimeMissingErrorVm(
        DotNetRuntimeMissingErrorClassification error,
        INavigateTo navigateTo)
        : base(error, navigateTo)
    {
        RequiredVersion = error.RequiredVersion;
    }
}
