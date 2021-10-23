using System;
using System.Threading;
using System.Threading.Tasks;
using Noggog;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.Execution.WorkEngine;

namespace Synthesis.Bethesda.Execution.DotNet.Builder
{
    public interface IBuild
    {
        Task<ErrorResponse> Compile(FilePath targetPath, CancellationToken cancel);
    }

    public class Build : IBuild
    {
        public IWorkDropoff Dropoff { get; }
        public Func<IBuildOutputAccumulator> OutputAccumulatorFactory { get; }
        public IBuildResultsProcessor ResultsProcessor { get; }
        public IProcessRunner ProcessRunner { get; }
        public IBuildStartInfoProvider BuildStartInfoProvider { get; }

        public Build(
            IProcessRunner processRunner,
            IWorkDropoff workDropoff,
            Func<IBuildOutputAccumulator> outputAccumulatorFactory,
            IBuildResultsProcessor resultsProcessor,
            IBuildStartInfoProvider buildStartInfoProvider)
        {
            Dropoff = workDropoff;
            OutputAccumulatorFactory = outputAccumulatorFactory;
            ResultsProcessor = resultsProcessor;
            ProcessRunner = processRunner;
            BuildStartInfoProvider = buildStartInfoProvider;
        }
        
        public async Task<ErrorResponse> Compile(FilePath targetPath, CancellationToken cancel)
        {
            var start = BuildStartInfoProvider.Construct(targetPath.Name.ToString());
            start.WorkingDirectory = targetPath.Directory!;

            var accumulator = OutputAccumulatorFactory();

            var result = await Dropoff.EnqueueAndWait(() =>
            {
                return ProcessRunner.RunWithCallback(
                    start,
                    outputCallback: accumulator.Process,
                    errorCallback: e => {},
                    cancel: cancel);
            }, cancel);
            
            if (result == 0) return ErrorResponse.Success;

            return ResultsProcessor.GetResults(
                targetPath,
                cancel,
                accumulator);
        }
    }
}