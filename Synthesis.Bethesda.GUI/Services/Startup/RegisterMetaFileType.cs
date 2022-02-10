using System;
using System.Diagnostics;
using Noggog.IO;
using Serilog;
using Synthesis.Bethesda.Execution.Utility;

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