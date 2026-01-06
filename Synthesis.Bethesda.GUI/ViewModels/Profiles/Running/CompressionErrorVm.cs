using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.GUI.Services.Main;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

/// <summary>
/// View model wrapper for CompressionErrorClassification
/// </summary>
public class CompressionErrorVm : ErrorClassificationVm
{
    public delegate CompressionErrorVm Factory(CompressionErrorClassification error);

    public CompressionErrorVm(
        CompressionErrorClassification error,
        INavigateTo navigateTo)
        : base(error, navigateTo)
    {
    }
}
