using System;
using Noggog;

namespace Synthesis.Bethesda.Execution.Pathing
{
    public interface IExtraDataPathProvider
    {
        DirectoryPath Path { get; }
    }

    public class ExtraDataPathProvider : IExtraDataPathProvider
    {
        public DirectoryPath Path => System.IO.Path.Combine(Environment.CurrentDirectory, "Data");
    }

    public class ExtraDataPathInjection : IExtraDataPathProvider
    {
        public DirectoryPath Path { get; init; }
    }
}