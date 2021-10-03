using System;

namespace Synthesis.Bethesda.Execution.DotNet
{
    public interface IIsApplicableErrorLine
    {
        bool IsApplicable(ReadOnlySpan<char> str);
    }

    public class IsApplicableErrorLine : IIsApplicableErrorLine
    {
        public bool IsApplicable(ReadOnlySpan<char> str)
        {
            return str.Contains(": error ", StringComparison.Ordinal);
        }
    }
}