namespace Synthesis.Bethesda.Execution.DotNet
{
    public interface IDotNetCommandPathProvider
    {
        string Path { get; }
    }

    public class DotNetCommandPathProvider : IDotNetCommandPathProvider
    {
        public string Path => "dotnet";
    }
}