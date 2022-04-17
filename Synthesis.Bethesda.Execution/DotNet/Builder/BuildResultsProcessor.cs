using System;
using System.Threading;
using Noggog;
using Synthesis.Bethesda.Execution.DotNet.Builder.Transient;

namespace Synthesis.Bethesda.Execution.DotNet.Builder;

public interface IBuildResultsProcessor
{
    ErrorResponse GetResults(
        FilePath targetPath,
        int result,
        CancellationToken cancel,
        IBuildOutputAccumulator accumulator);
}

public class BuildResultsProcessor : IBuildResultsProcessor
{
    public const string TargetPathSuffix = " : ";
        
    public ErrorResponse GetResults(
        FilePath targetPath,
        int result,
        CancellationToken cancel,
        IBuildOutputAccumulator accumulator)
    {
        var firstError = accumulator.FirstError;
        firstError = firstError?.TrimStart($"{targetPath}{TargetPathSuffix}");
        if (firstError == null && cancel.IsCancellationRequested)
        {
            firstError = "Cancelled";
        }
        return ErrorResponse.Fail(reason: firstError ?? $"Unknown Error ({result}): {string.Join(Environment.NewLine, accumulator.Output)}");
    }
}