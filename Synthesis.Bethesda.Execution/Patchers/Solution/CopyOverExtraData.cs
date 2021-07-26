using System;
using System.IO.Abstractions;
using Noggog;
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
        private readonly IDefaultDataPathProvider _defaultDataPathProvider;
        private readonly IPatcherExtraDataPathProvider _userExtraData;

        public delegate ICopyOverExtraData Factory(IDefaultDataPathProvider defaultDataPathProvider);

        public CopyOverExtraData(
            IFileSystem fileSystem,
            ILogger logger,
            IDefaultDataPathProvider defaultDataPathProvider,
            IPatcherExtraDataPathProvider userExtraData)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            _defaultDataPathProvider = defaultDataPathProvider;
            _userExtraData = userExtraData;
        }
        
        public void Copy()
        {
            var inputExtraData = _defaultDataPathProvider.Path;
            if (!_fileSystem.File.Exists(inputExtraData))
            {
                _logger.Information("No extra data to consider");
                return;
            }


            var outputExtraData = _userExtraData.Path;
            if (_fileSystem.File.Exists(outputExtraData))
            {
                _logger.Information("Extra data folder already exists. Leaving as is: {OutputExtraData}", outputExtraData);
                return;
            }

            _logger.Information("Copying extra data folder");
            _logger.Information("  From: {InputExtraData}", inputExtraData);
            _logger.Information("  To: {OutputExtraData}", outputExtraData);
            _fileSystem.Directory.DeepCopy(inputExtraData, outputExtraData);
        }
    }
}