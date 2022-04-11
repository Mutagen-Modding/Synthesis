using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.Settings.V1;

[ExcludeFromCodeCoverage]
public record PipelineSettings
{
    public IList<SynthesisProfile> Profiles { get; set; } = new List<SynthesisProfile>();
}