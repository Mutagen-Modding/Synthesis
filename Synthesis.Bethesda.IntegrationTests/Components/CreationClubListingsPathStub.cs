using Mutagen.Bethesda.Plugins.Order.DI;
using Noggog;

namespace Synthesis.Bethesda.IntegrationTests.Components;

public class CreationClubListingsPathStub : ICreationClubListingsPathProvider
{
    public FilePath? Path => null;
}
