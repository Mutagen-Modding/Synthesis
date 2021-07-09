namespace Synthesis.Bethesda.GUI
{
    public interface IEnvironmentErrorVm
    {
        bool InError { get; }
        string? ErrorString { get; }
    }
}