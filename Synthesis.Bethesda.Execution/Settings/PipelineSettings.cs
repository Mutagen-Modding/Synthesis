using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Synthesis.Bethesda.Execution.Settings
{
    public interface IPipelineSettings
    {
        List<ISynthesisProfileSettings> Profiles { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class PipelineSettings : IPipelineSettings
    {
        public List<ISynthesisProfileSettings> Profiles { get; set; } = new();
    }
}
