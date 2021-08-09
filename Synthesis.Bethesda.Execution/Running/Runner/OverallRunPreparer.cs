using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running.Runner
{
    public interface IOverallRunPreparer
    {
        Task Prepare(
            ModPath outputPath,
            PersistenceMode persistenceMode = PersistenceMode.None,
            string? persistencePath = null);
    }

    public class OverallRunPreparer : IOverallRunPreparer
    {
        public IRunLoadOrderPreparer RunLoadOrderPreparer { get; }
        public IRunPersistencePreparer PersistencePreparer { get; }

        public OverallRunPreparer(
            IRunLoadOrderPreparer runLoadOrderPreparer,
            IRunPersistencePreparer persistencePreparer)
        {
            RunLoadOrderPreparer = runLoadOrderPreparer;
            PersistencePreparer = persistencePreparer;
        }
        
        public async Task Prepare(
            ModPath outputPath,
            PersistenceMode persistenceMode = PersistenceMode.None,
            string? persistencePath = null)
        {
            await Task.WhenAll(
                Task.Run(() =>
                {
                    RunLoadOrderPreparer.Write(outputPath);
                }), 
                Task.Run(() =>
                {
                    PersistencePreparer.Prepare(persistenceMode, persistencePath);
                }));
        }
    }
}