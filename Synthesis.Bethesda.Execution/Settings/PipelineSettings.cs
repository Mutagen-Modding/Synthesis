using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.Settings
{
    public interface IPipelineSettings
    {
        IList<ISynthesisProfileSettings> Profiles { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public record PipelineSettings : IPipelineSettings
    {
        public IList<ISynthesisProfileSettings> Profiles { get; set; } = new List<ISynthesisProfileSettings>();
    }
}
