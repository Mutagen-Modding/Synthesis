using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running.Runner
{
    public interface IGroupRunPreparer
    {
        Task Prepare(
            ModKey modKey,
            PersistenceMode persistenceMode = PersistenceMode.None,
            string? persistencePath = null);
    }

    public class GroupRunPreparer : IGroupRunPreparer
    {
        public IRunLoadOrderPreparer RunLoadOrderPreparer { get; }
        public IRunPersistencePreparer PersistencePreparer { get; }

        public GroupRunPreparer(
            IRunLoadOrderPreparer runLoadOrderPreparer,
            IRunPersistencePreparer persistencePreparer)
        {
            RunLoadOrderPreparer = runLoadOrderPreparer;
            PersistencePreparer = persistencePreparer;
        }
        
        public async Task Prepare(
            ModKey modKey,
            PersistenceMode persistenceMode = PersistenceMode.None,
            string? persistencePath = null)
        {
            await Task.WhenAll(
                Task.Run(() =>
                {
                    RunLoadOrderPreparer.Write(modKey);
                }), 
                Task.Run(() =>
                {
                    PersistencePreparer.Prepare(persistenceMode, persistencePath);
                }));
        }
    }
}