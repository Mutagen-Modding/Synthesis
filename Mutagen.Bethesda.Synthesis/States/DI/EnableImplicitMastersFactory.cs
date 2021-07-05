using System.IO.Abstractions;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins.Masters.DI;
using Mutagen.Bethesda.Plugins.Order.DI;
using Noggog;

namespace Mutagen.Bethesda.Synthesis.States.DI
{
    public interface IEnableImplicitMastersFactory
    {
        IEnableImplicitMasters Get(string dataFolder, GameRelease release);
    }

    public class EnableImplicitMastersFactory : IEnableImplicitMastersFactory
    {
        private readonly IFileSystem _FileSystem;

        public EnableImplicitMastersFactory(
            IFileSystem fileSystem)
        {
            _FileSystem = fileSystem;
        }
        
        public IEnableImplicitMasters Get(string dataFolder, GameRelease release)
        {
            return new EnableImplicitMasters(
                new FindImplicitlyIncludedMods(
                    new DataDirectoryInjection(dataFolder),
                    new MasterReferenceReaderFactory(
                        _FileSystem,
                        new GameReleaseInjection(release))));
        }
    }
}