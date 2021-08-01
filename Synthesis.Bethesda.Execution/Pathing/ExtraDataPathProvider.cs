using System;
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
        private readonly ICurrentDirectoryProvider _currentDirectoryProvider;

        public ExtraDataPathProvider(ICurrentDirectoryProvider currentDirectoryProvider)
        {
            _currentDirectoryProvider = currentDirectoryProvider;
        }
    
        public DirectoryPath Path => System.IO.Path.Combine(_currentDirectoryProvider.CurrentDirectory, "Data");
    }

    public class ExtraDataPathInjection : IExtraDataPathProvider
    {
        public DirectoryPath Path { get; init; }
    }
}