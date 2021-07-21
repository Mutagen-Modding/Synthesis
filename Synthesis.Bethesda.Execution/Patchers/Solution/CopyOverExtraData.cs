using System;
using System.IO.Abstractions;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers.TopLevel;

namespace Synthesis.Bethesda.Execution.Patchers.Solution
{
    public interface ICopyOverExtraData
    {
        void Copy(Action<string> log);
    }

    public class CopyOverExtraData : ICopyOverExtraData
    {
        private readonly IFileSystem _fileSystem;
        private readonly IDefaultDataPathProvider _defaultDataPathProvider;
        private readonly IPatcherExtraDataPathProvider _userExtraData;

        public delegate ICopyOverExtraData Factory(IDefaultDataPathProvider defaultDataPathProvider);

        public CopyOverExtraData(
            IFileSystem fileSystem,
            IDefaultDataPathProvider defaultDataPathProvider,
            IPatcherExtraDataPathProvider userExtraData)
        {
            _fileSystem = fileSystem;
            _defaultDataPathProvider = defaultDataPathProvider;
            _userExtraData = userExtraData;
        }
        
        public void Copy(Action<string> log)
        {
            var inputExtraData = _defaultDataPathProvider.Path;
            if (!_fileSystem.File.Exists(inputExtraData))
            {
                log("No extra data to consider.");
                return;
            }


            var outputExtraData = _userExtraData.Path;
            if (_fileSystem.File.Exists(outputExtraData))
            {
                log($"Extra data folder already exists. Leaving as is: {outputExtraData}");
                return;
            }

            log("Copying extra data folder");
            log($"  From: {inputExtraData}");
            log($"  To: {outputExtraData}");
            _fileSystem.Directory.DeepCopy(inputExtraData, outputExtraData);
        }
    }
}