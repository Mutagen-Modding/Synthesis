using System;
using System.IO;
using Noggog;
using Serilog;

namespace Synthesis.Bethesda.GUI.Services
{
    public interface IClearLoading
    {
        void Do();
    }

    public class ClearLoading : IClearLoading
    {
        private readonly ILogger _Logger;

        public ClearLoading(
            ILogger logger)
        {
            _Logger = logger;
        }
        
        public void Do()
        {
            try
            {
                var loadingDir = new DirectoryInfo(Execution.Paths.LoadingFolder);
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