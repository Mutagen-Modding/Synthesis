namespace Synthesis.Bethesda.GUI
{
    public interface IEnvironmentErrorVM
    {
        bool InError { get; }
        string? ErrorString { get; }
    }
}