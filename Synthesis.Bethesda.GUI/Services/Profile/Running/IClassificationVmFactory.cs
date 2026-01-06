using Autofac;
using Synthesis.Bethesda.Execution.Reporters;

namespace Synthesis.Bethesda.GUI.Services.Profile.Running;

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
    /// <returns>An object to use as the DataContext (either the classification or a VM wrapper)</returns>
    object CreateVm(ErrorClassification classification, ILifetimeScope scope);
}
