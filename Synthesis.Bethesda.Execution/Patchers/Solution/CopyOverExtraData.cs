using System;
using System.IO.Abstractions;
using Noggog;
using Noggog.IO;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.TopLevel;

namespace Synthesis.Bethesda.Execution.Patchers.Solution
{
    public interface ICopyOverExtraData
    {
        void Copy();
    }

    public class CopyOverExtraData : ICopyOverExtraData
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        public IDeepCopyDirectory DeepCopy { get; }
        public IDefaultDataPathProvider DefaultDataPathProvider { get; }
        public IPatcherExtraDataPathProvider UserExtraData { get; }

        public delegate ICopyOverExtraData Factory(IDefaultDataPathProvider defaultDataPathProvider);

        public CopyOverExtraData(
            IFileSystem fileSystem,
            ILogger logger,
            IDeepCopyDirectory deepCopyDirectory,
            IDefaultDataPathProvider defaultDataPathProvider,
            IPatcherExtraDataPathProvider userExtraData)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            DeepCopy = deepCopyDirectory;
            DefaultDataPathProvider = defaultDataPathProvider;
            UserExtraData = userExtraData;
        }
        
        public void Copy()
        {
            var inputExtraData = DefaultDataPathProvider.Path;
            if (!_fileSystem.Directory.Exists(inputExtraData))
            {
                _logger.Information("No extra data to consider");
                return;
            }

            var outputExtraData = UserExtraData.Path;
            if (_fileSystem.Directory.Exists(outputExtraData))
            {
                _logger.Information("Extra data folder already exists. Leaving as is: {OutputExtraData}", outputExtraData);
                return;
            }

            _logger.Information("Copying extra data folder");
            _logger.Information("  From: {InputExtraData}", inputExtraData);
            _logger.Information("  To: {OutputExtraData}", outputExtraData);
            DeepCopy.DeepCopy(inputExtraData, outputExtraData);
        }
    }
}