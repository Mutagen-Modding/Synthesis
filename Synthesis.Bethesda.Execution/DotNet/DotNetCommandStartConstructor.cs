using System.Diagnostics;
using Noggog;

namespace Synthesis.Bethesda.Execution.DotNet
{
    public interface IDotNetCommandStartConstructor
    {
        ProcessStartInfo Construct(string command, FilePath path, params string?[] args);
    }

    public class DotNetCommandStartConstructor : IDotNetCommandStartConstructor
    {
        public IDotNetCommandPathProvider DotNetPathProvider { get; }
        public ICommandStringConstructor Constructor { get; }

        public DotNetCommandStartConstructor(
            IDotNetCommandPathProvider dotNetPathProvider,
            ICommandStringConstructor constructor)
        {
            DotNetPathProvider = dotNetPathProvider;
            Constructor = constructor;
        }

        public ProcessStartInfo Construct(string command, FilePath path, params string?[] args)
        {
            return new ProcessStartInfo(DotNetPathProvider.Path, Constructor.Get(command, path, args));
        }
    }
}