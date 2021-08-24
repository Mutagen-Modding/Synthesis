using System.IO.Abstractions;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins.Masters.DI;
using Mutagen.Bethesda.Plugins.Order.DI;

namespace Mutagen.Bethesda.Synthesis.States.DI
{
    public interface IEnableImplicitMastersFactory
    {
        IEnableImplicitMasters Get(string dataFolder, GameRelease release);
    }

    public class EnableImplicitMastersFactory : IEnableImplicitMastersFactory
    {
        private readonly IFileSystem _fileSystem;

        public EnableImplicitMastersFactory(
            IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }
        
        public IEnableImplicitMasters Get(string dataFolder, GameRelease release)
        {
            return new EnableImplicitMasters(
                new FindImplicitlyIncludedMods(
                    _fileSystem,
                    new DataDirectoryInjection(dataFolder),
                    new MasterReferenceReaderFactory(
                        _fileSystem,
                        new GameReleaseInjection(release))));
        }
    }
}