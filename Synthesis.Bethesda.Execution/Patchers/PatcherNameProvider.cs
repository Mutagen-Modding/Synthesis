using System;

namespace Synthesis.Bethesda.Execution.Patchers
{
    public interface IPatcherNameProvider
    {
        string Name { get; }
    }

    public class PatcherNameInjection : IPatcherNameProvider
    {
        public string Name { get; init; } = string.Empty;
    }
}