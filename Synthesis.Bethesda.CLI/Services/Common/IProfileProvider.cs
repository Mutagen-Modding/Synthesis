using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.CLI.Services.Common;

public interface IProfileProvider : IProfileIdentifier, IProfileNameProvider
{
    Lazy<ISynthesisProfileSettings> Profile { get; }
}