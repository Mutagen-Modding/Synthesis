using System;
using System.Reactive.Linq;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git
{
    public interface IGitStatusDisplay
    {
        IObservable<StatusRecord> StatusDisplay { get; }
    }

    public class GitStatusDisplay : IGitStatusDisplay
    {
        public IObservable<StatusRecord> StatusDisplay { get; }

        public GitStatusDisplay(
            IRunnableStateProvider runnableStateProvider,
            ICompliationProvider compliationProvider,
            IPatcherRunnabilityCliState runnabilityCliState,
            IDriverRepositoryPreparationFollower driverRepositoryPreparation)
        {
            StatusDisplay = Observable.CombineLatest(
                    driverRepositoryPreparation.DriverInfo,
                    runnableStateProvider.State,
                    compliationProvider.State,
                    runnabilityCliState.Runnable,
                    (driver, runnable, comp, runnability) =>
                    {
                        if (driver.RunnableState.Failed)
                        {
                            if (driver.IsHaltingError)
                            {
                                return new StatusRecord(
                                    Text: "Blocking Error",
                                    Processing: false,
                                    Blocking: true,
                                    Command: null);
                            }

                            return new StatusRecord(
                                Text: "Analyzing repository",
                                Processing: true,
                                Blocking: false,
                                Command: null);
                        }

                        if (runnable.RunnableState.Failed)
                        {
                            if (runnable.IsHaltingError)
                            {
                                return new StatusRecord(
                                    Text: "Blocking Error",
                                    Processing: false,
                                    Blocking: true,
                                    Command: null);
                            }

                            return new StatusRecord(
                                Text: "Checking out desired state",
                                Processing: true,
                                Blocking: false,
                                Command: null);
                        }

                        if (comp.RunnableState.Failed)
                        {
                            if (comp.IsHaltingError)
                            {
                                return new StatusRecord(
                                    Text: "Blocking Error",
                                    Processing: false,
                                    Blocking: true,
                                    Command: null);
                            }

                            return new StatusRecord(
                                Text: "Compiling",
                                Processing: true,
                                Blocking: false,
                                Command: null);
                        }

                        if (runnability.RunnableState.Failed)
                        {
                            if (runnability.IsHaltingError)
                            {
                                return new StatusRecord(
                                    Text: "Blocking Error",
                                    Processing: false,
                                    Blocking: true,
                                    Command: null);
                            }

                            return new StatusRecord(
                                Text: "Checking runnability",
                                Processing: true,
                                Blocking: false,
                                Command: null);
                        }

                        return new StatusRecord(
                            Text: "Ready",
                            Processing: false,
                            Blocking: false,
                            Command: null);
                    })
                .Replay(1)
                .RefCount();
        }
    }
}