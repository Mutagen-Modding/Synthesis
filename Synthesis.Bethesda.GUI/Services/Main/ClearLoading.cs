using System;
using System.IO;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.Pathing;

namespace Synthesis.Bethesda.GUI.Services.Main
{
    public interface IClearLoading
    {
        void Do();
    }

    public class ClearLoading : IClearLoading
    {
        private readonly IWorkingDirectorySubPaths _Paths;
        private readonly ILogger _Logger;

        public ClearLoading(
            IWorkingDirectorySubPaths paths,
            ILogger logger)
        {
            _Paths = paths;
            _Logger = logger;
        }
        
        public void Do()
        {
            try
            {
                var loadingDir = new DirectoryInfo(_Paths.LoadingFolder);
                if (!loadingDir.Exists) return;
                _Logger.Information("Clearing Loading folder");
                loadingDir.DeleteEntireFolder(deleteFolderItself: false);
            }
            catch (Exception ex)
            {
                _Logger.Error(ex, "Error clearing Loading folder");
            }
        }
    }
}