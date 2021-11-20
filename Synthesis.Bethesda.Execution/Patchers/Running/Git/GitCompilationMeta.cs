namespace Synthesis.Bethesda.Execution.Patchers.Running.Git
{
    public class GitCompilationMeta
    {
        public string MutagenVersion { get; set; } = string.Empty;
        public string SynthesisVersion { get; set; } = string.Empty;
        public string Sha { get; set; } = string.Empty;
        public bool DoesNotHaveRunnability { get; set; }
    }
}