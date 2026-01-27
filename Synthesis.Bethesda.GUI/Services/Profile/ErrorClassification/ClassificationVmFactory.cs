using Autofac;
using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.GUI.ViewModels.Errors;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

namespace Synthesis.Bethesda.GUI.Services.Profile.ErrorClassification;

/// <summary>
/// Factory for creating view models for error classifications
/// </summary>
public class ClassificationVmFactory : IClassificationVmFactory
{
    /// <inheritdoc />
    public object CreateVm(Execution.Reporters.ErrorClassification classification, ILifetimeScope scope)
    {
        // Wrap each error type with its corresponding VM for enhanced UI functionality
        return classification switch
        {
            TooManyMastersError tooManyMastersError =>
                scope.Resolve<TooManyMastersErrorVm.Factory>()(tooManyMastersError),

            ReferencedModMissingError referencedModMissingError =>
                scope.Resolve<ReferencedModMissingErrorVm.Factory>()(referencedModMissingError),

            CompressionErrorClassification compressionError =>
                scope.Resolve<CompressionErrorVm.Factory>()(compressionError),

            AccessDeniedErrorClassification accessDeniedError =>
                scope.Resolve<AccessDeniedErrorVm.Factory>()(accessDeniedError),

            RanBuildInMo2ErrorClassification ranBuildInMo2Error =>
                scope.Resolve<RanBuildInMo2ErrorVm.Factory>()(ranBuildInMo2Error),

            Mo2BuildBlockedErrorClassification mo2BuildBlockedError =>
                scope.Resolve<Mo2BuildBlockedErrorVm.Factory>()(mo2BuildBlockedError),

            // For unrecognized error types, return the classification as-is
            _ => classification
        };
    }
}
