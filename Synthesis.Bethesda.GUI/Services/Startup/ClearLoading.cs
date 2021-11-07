using System;
using System.IO;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.GUI.Services.Startup
{
    public class ClearLoading : IStartupTask
    {
        private readonly IWorkingDirectorySubPaths _paths;
        private readonly ILogger _logger;

        public ClearLoading(
            IWorkingDirectorySubPaths paths,
            ILogger logger)
        {
            _paths = paths;
            _logger = logger;
        }
        
        public void Start()
        {
            try
            {
                var loadingDir = new DirectoryInfo(_paths.LoadingFolder);
                if (!loadingDir.Exists) return;
                _logger.Information("Clearing Loading folder");
                loadingDir.DeleteEntireFolder(deleteFolderItself: false);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error clearing Loading folder");
            }
        }
    }
}