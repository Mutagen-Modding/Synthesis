using Autofac;
using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.Services.Profile.ErrorClassification;

/// <summary>
/// Factory for creating view models for error classifications
/// </summary>
public interface IClassificationVmFactory
{
    /// <summary>
    /// Creates a view model wrapper for the given error classification.
    /// For most classifications, returns the classification itself.
    /// For special cases, wraps it in a VM with additional functionality.
    /// </summary>
    /// <param name="classification">The error classification to wrap</param>
    /// <param name="scope">The DI scope to resolve dependencies from</param>
    /// <param name="patcher">Optional patcher that caused the error, for version detection</param>
    /// <returns>An object to use as the DataContext (either the classification or a VM wrapper)</returns>
    object CreateVm(Execution.Reporters.ErrorClassification classification, ILifetimeScope scope, PatcherVm? patcher = null);
}
