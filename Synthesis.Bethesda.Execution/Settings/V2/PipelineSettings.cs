using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.Settings.V2
{
    [ExcludeFromCodeCoverage]
    public record PipelineSettings : IPipelineSettings
    {
        public int Version => 2; 
        public IList<ISynthesisProfileSettings> Profiles { get; set; } = new List<ISynthesisProfileSettings>();
    }
}
