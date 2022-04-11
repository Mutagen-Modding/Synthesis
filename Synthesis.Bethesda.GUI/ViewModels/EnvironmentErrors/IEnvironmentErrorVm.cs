namespace Synthesis.Bethesda.GUI.ViewModels.EnvironmentErrors;

public interface IEnvironmentErrorVm
{
    bool InError { get; }
    string? ErrorString { get; }
}