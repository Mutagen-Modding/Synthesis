using System.Diagnostics;
using Noggog.IO;
using Serilog;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.GUI.Logging;

namespace Synthesis.Bethesda.GUI.Services.Startup;

public class RegisterMetaFileType : IStartupTask
{
    private readonly ILogger _logger;

    public RegisterMetaFileType(ILogger logger)
    {
        _logger = logger;
    }
    
    public void Start()
    {
        if (LogPreferences.IsTesting) return;
        try
        {
            FileAssociations.EnsureAssociationsSet(
                new FileAssociation(
                    Extension: ".synth",
                    ProgId: "Synthesis.meta",
                    FileTypeDescription: "Patcher meta file for Synthesis",
                    ExecutableFilePath: Process.GetCurrentProcess().MainModule!.FileName!));
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error registering synth file association");
        }
    }
}