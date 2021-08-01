using System;
using System.Diagnostics.CodeAnalysis;
using Noggog;
using Noggog.IO;

namespace Synthesis.Bethesda.Execution.Pathing
{
    public interface IExtraDataPathProvider
    {
        DirectoryPath Path { get; }
    }

    public class ExtraDataPathProvider : IExtraDataPathProvider
    {
        public ICurrentDirectoryProvider CurrentDirectoryProvider { get; }

        public ExtraDataPathProvider(ICurrentDirectoryProvider currentDirectoryProvider)
        {
            CurrentDirectoryProvider = currentDirectoryProvider;
        }
    
        public DirectoryPath Path => System.IO.Path.Combine(CurrentDirectoryProvider.CurrentDirectory, "Data");
    }

    [ExcludeFromCodeCoverage]
    public class ExtraDataPathInjection : IExtraDataPathProvider
    {
        public DirectoryPath Path { get; init; }
    }
}