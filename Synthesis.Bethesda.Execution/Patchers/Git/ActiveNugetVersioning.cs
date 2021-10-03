using Noggog;
using System;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public record ActiveNugetVersioning(NugetsToUse Mutagen, NugetsToUse Synthesis)
    {
        public GetResponse<NugetsVersioningTarget> TryGetTarget()
        {
            var mutaVersion = this.Mutagen.TryGetVersioning(); 
            if (mutaVersion.Failed) return mutaVersion.BubbleFailure<NugetsVersioningTarget>();
            var synthesisVersion = this.Synthesis.TryGetVersioning();
            if (synthesisVersion.Failed) return synthesisVersion.BubbleFailure<NugetsVersioningTarget>();
            return GetResponse<NugetsVersioningTarget>.Succeed(
                new NugetsVersioningTarget(
                    Mutagen: new NugetVersioningTarget(
                        mutaVersion.Value, this.Mutagen.Versioning), 
                    Synthesis: new NugetVersioningTarget(
                        synthesisVersion.Value, this.Synthesis.Versioning)));
        }

        public override string ToString()
        {
            return $"({Mutagen})({Synthesis})";
        }
    }
}