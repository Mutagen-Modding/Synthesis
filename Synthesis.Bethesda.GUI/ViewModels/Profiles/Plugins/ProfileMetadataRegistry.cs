using StructureMap;

namespace Synthesis.Bethesda.GUI.Profiles.Plugins
{
    public class ProfileMetadataRegistry
    {
        private readonly IProfileDataFolder _DataFolder;
        private readonly IProfileDirectories _Directories;
        private readonly IProfileLoadOrder _LoadOrder;
        private readonly IProfilePatchersList _PatchersList;
        private readonly IProfileSimpleLinkCache _LinkCache;
        private readonly IProfileVersioning _Versioning;
        private readonly IRemovePatcherFromProfile _RemovePatcher;

        public ProfileMetadataRegistry(
            IProfileDataFolder dataFolder,
            IProfileDirectories directories,
            IProfileLoadOrder loadOrder,
            IProfilePatchersList patchersList,
            IProfileSimpleLinkCache linkCache,
            IProfileVersioning versioning,
            IRemovePatcherFromProfile removePatcher)
        {
            _DataFolder = dataFolder;
            _Directories = directories;
            _LoadOrder = loadOrder;
            _PatchersList = patchersList;
            _LinkCache = linkCache;
            _Versioning = versioning;
            _RemovePatcher = removePatcher;
        }

        public ExplicitArgsExpression Configure()
        {
            return Inject.Container
                .With(_DataFolder)
                .With(_Directories)
                .With(_LoadOrder)
                .With(_PatchersList)
                .With(_LinkCache)
                .With(_RemovePatcher)
                .With(_Versioning);
        }
    }
}