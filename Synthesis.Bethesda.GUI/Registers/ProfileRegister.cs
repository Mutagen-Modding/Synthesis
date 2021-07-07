using Mutagen.Bethesda.Environments.DI;
using StructureMap;
using StructureMap.AutoFactory;
using Synthesis.Bethesda.GUI.Profiles.Plugins;
using Synthesis.Bethesda.GUI.Services.Profile;

namespace Synthesis.Bethesda.GUI.Registers
{
    public class ProfileRegister : Registry
    {
        public ProfileRegister()
        {
            ForSingletonOf<ProfilePatchersList>();
            Forward<ProfilePatchersList, IRemovePatcherFromProfile>();
            Forward<ProfilePatchersList, IProfilePatchersList>();
            ForSingletonOf<IProfileLoadOrder>().Use<ProfileLoadOrder>();
            ForSingletonOf<IProfileDirectories>().Use<ProfileDirectories>();
            ForSingletonOf<ProfileDataFolder>();
            Forward<ProfileDataFolder, IProfileDataFolder>();
            Forward<ProfileDataFolder, IDataDirectoryProvider>();
            ForSingletonOf<IProfileVersioning>().Use<ProfileVersioning>();
            ForSingletonOf<IProfileSimpleLinkCache>().Use<ProfileSimpleLinkCache>();
            For<IPatchersRunFactory>().CreateFactory();
            For<PatchersRunVM>();
            
            Scan(s =>
            {
                s.AssemblyContainingType<IPatcherFactory>();
                s.IncludeNamespaceContainingType<IPatcherFactory>();
                s.WithDefaultConventions();
            });
        }
    }
}