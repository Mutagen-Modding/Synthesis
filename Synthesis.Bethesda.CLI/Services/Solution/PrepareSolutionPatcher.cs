using Serilog;
using Synthesis.Bethesda.Execution.Exceptions;
using Synthesis.Bethesda.Execution.Patchers.Running.Solution;
using Synthesis.Bethesda.Execution.Reporters;

namespace Synthesis.Bethesda.CLI.Services.Solution;

public class PrepareSolutionPatcher
{
    private readonly SolutionPatcherPrep _solutionPatcherPrep;
    private readonly IErrorClassifier _errorClassifier;
    private readonly ILogger _logger;

    public PrepareSolutionPatcher(
        SolutionPatcherPrep solutionPatcherPrep,
        IErrorClassifier errorClassifier,
        ILogger logger)
    {
        _solutionPatcherPrep = solutionPatcherPrep;
        _errorClassifier = errorClassifier;
        _logger = logger;
    }

    public async Task Prepare(CancellationToken cancel)
    {
        try
        {
            await _solutionPatcherPrep.Prep(cancel);
        }
        catch (Exception ex)
        {
            var classification = _errorClassifier.Classify(ex);
            if (classification != null)
            {
                _logger.Error("Error detected: {ErrorType}", classification.ErrorType);
                _logger.Error("{Message}", classification.Message);

                if (!string.IsNullOrWhiteSpace(classification.DiscussionLink))
                {
                    _logger.Error("Read more: {DiscussionLink}", classification.DiscussionLink);
                }

                throw new ClassifiedErrorException(ex);
            }

            throw;
        }
    }
}