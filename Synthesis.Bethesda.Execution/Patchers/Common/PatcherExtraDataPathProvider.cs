using Noggog;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Profile;

namespace Synthesis.Bethesda.Execution.Patchers.Common;

public interface IPatcherExtraDataPathProvider
{
    DirectoryPath Path { get; }
    DirectoryPath GetPathForName(string name);
}

public class PatcherExtraDataPathProvider : IPatcherExtraDataPathProvider
{
    public IPatcherNameProvider NameProvider { get; }
    public IProfileNameProvider ProfileNameProvider { get; }
    public IExtraDataPathProvider ExtraDataPathProvider { get; }

    public PatcherExtraDataPathProvider(
        IPatcherNameProvider nameProvider,
        IProfileNameProvider profileNameProvider,
        IExtraDataPathProvider extraDataPathProvider)
    {
        NameProvider = nameProvider;
        ProfileNameProvider = profileNameProvider;
        ExtraDataPathProvider = extraDataPathProvider;
    }
        
    public DirectoryPath GetPathForName(string name) => System.IO.Path.Combine(ExtraDataPathProvider.Path, ProfileNameProvider.Name, name);

    public DirectoryPath Path => GetPathForName(NameProvider.Name);
}