using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.GUI.Services.Main;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

/// <summary>
/// Base view model for error classifications with common functionality
/// </summary>
public abstract class ErrorClassificationVm : ViewModel
{
    protected readonly INavigateTo _navigateTo;

    /// <summary>
    /// The underlying error classification
    /// </summary>
    public ErrorClassification Error { get; }

    /// <summary>
    /// Command to open the discussion link in a browser
    /// </summary>
    public ICommand ReadMoreCommand { get; }

    // Expose error properties for binding
    public string ErrorType => Error.ErrorType;
    public string Message => Error.Message;
    public string? DiscussionLink => Error.DiscussionLink;

    protected ErrorClassificationVm(ErrorClassification error, INavigateTo navigateTo)
    {
        Error = error;
        _navigateTo = navigateTo;

        ReadMoreCommand = ReactiveCommand.Create(
            () =>
            {
                if (!string.IsNullOrWhiteSpace(DiscussionLink))
                {
                    _navigateTo.Navigate(DiscussionLink);
                }
            },
            this.WhenAnyValue(x => x.DiscussionLink)
                .Select(link => !string.IsNullOrWhiteSpace(link)));
    }
}
