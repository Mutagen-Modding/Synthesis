using Autofac;
using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

namespace Synthesis.Bethesda.GUI.Services.Profile.Running;

/// <summary>
/// Factory for creating view models for error classifications
/// </summary>
public class ClassificationVmFactory : IClassificationVmFactory
{
    /// <inheritdoc />
    public object CreateVm(ErrorClassification classification, ILifetimeScope scope)
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

            // For unrecognized error types, return the classification as-is
            _ => classification
        };
    }
}
