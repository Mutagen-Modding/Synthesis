using System;
using System.Buffers;
using Noggog;

namespace Synthesis.Bethesda.Execution.DotNet;

public interface IPrintErrorMessage
{
    void Print(ReadOnlySpan<char> message, ReadOnlySpan<char> relativePathTrim, ReadOnlySpanAction<char, object?> toDo);
}

public class PrintErrorMessage : IPrintErrorMessage
{
    private readonly ITrimErrorMessage _trimErrorMessage;
    private readonly IIsApplicableErrorLine _applicableErrorLine;

    public PrintErrorMessage(
        ITrimErrorMessage trimErrorMessage,
        IIsApplicableErrorLine applicableErrorLine)
    {
        _trimErrorMessage = trimErrorMessage;
        _applicableErrorLine = applicableErrorLine;
    }
        
    public void Print(ReadOnlySpan<char> message, ReadOnlySpan<char> relativePathTrim, ReadOnlySpanAction<char, object?> toDo)
    {
        foreach (var item in message.SplitLines())
        {
            if (!_applicableErrorLine.IsApplicable(item.Line)) continue;
            toDo(_trimErrorMessage.Trim(item.Line, relativePathTrim), null);
        }
    }
}