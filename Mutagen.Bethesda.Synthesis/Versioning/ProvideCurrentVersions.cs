namespace Mutagen.Bethesda.Synthesis.Versioning
{
    public interface IProvideCurrentVersions
    {
        string SynthesisVersion { get; }
        string MutagenVersion { get; }
    }
    
    public class ProvideCurrentVersions : IProvideCurrentVersions
    {
        public string SynthesisVersion { get; }

        public string MutagenVersion { get; }

        public ProvideCurrentVersions()
        {
            SynthesisVersion = Mutagen.Bethesda.Synthesis.Versioning.Versions.SynthesisVersion;
            MutagenVersion = Mutagen.Bethesda.Synthesis.Versioning.Versions.MutagenVersion;
        }
    }
}