using Autofac;
using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.GUI.ViewModels.Errors;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

namespace Synthesis.Bethesda.GUI.Services.Profile.ErrorClassification;

/// <summary>
/// Factory for creating view models for error classifications
/// </summary>
public class ClassificationVmFactory : IClassificationVmFactory
{
    /// <inheritdoc />
    public object CreateVm(Execution.Reporters.ErrorClassification classification, ILifetimeScope scope, PatcherVm? patcher = null)
    {
        // Wrap each error type with its corresponding VM for enhanced UI functionality
        return classification switch
        {
            TooManyMastersError tooManyMastersError =>
                scope.Resolve<TooManyMastersErrorVm.Factory>()(tooManyMastersError, patcher),

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

            MissingModsErrorClassification missingModsError =>
                scope.Resolve<MissingModsErrorVm.Factory>()(missingModsError),

            NonAdjacentSplitModsErrorClassification nonAdjacentSplitModsError =>
                scope.Resolve<NonAdjacentSplitModsErrorVm.Factory>()(nonAdjacentSplitModsError),

            OutputFileLockedErrorClassification outputFileLockedError =>
                scope.Resolve<OutputFileLockedErrorVm.Factory>()(outputFileLockedError),

            // For unrecognized error types, return the classification as-is
            _ => classification
        };
    }
}
