using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Noggog;
using Synthesis.Bethesda.DTO;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Registry
{
    public interface IRegistryListingReader
    {
        MutagenPatchersListing Read(FilePath path);
    }

    public class RegistryListingReader : IRegistryListingReader
    {
        private readonly IFileSystem _fileSystem;

        public RegistryListingReader(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }
        
        public MutagenPatchersListing Read(FilePath path)
        {
            var settings = new JsonSerializerOptions();
            settings.Converters.Add(new JsonStringEnumConverter());
            return JsonSerializer.Deserialize<MutagenPatchersListing>(
                _fileSystem.File.ReadAllText(path),
                settings)!;
        }
    }
}