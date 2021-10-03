using System;

namespace Synthesis.Bethesda.Execution.DotNet
{
    public interface ITrimErrorMessage
    {
        ReadOnlySpan<char> Trim(ReadOnlySpan<char> message, ReadOnlySpan<char> relativePathTrim);
    }

    public class TrimErrorMessage : ITrimErrorMessage
    {
        public ReadOnlySpan<char> Trim(ReadOnlySpan<char> message, ReadOnlySpan<char> relativePathTrim)
        {
            if (message.StartsWith(relativePathTrim))
            {
                message = message.Slice(relativePathTrim.Length);
            }

            int index = 0;
            while (index < message.Length)
            {
                var slice = message.Slice(index);
                var actualIndex = slice.IndexOf('[');
                if (actualIndex == -1) break;
                index = actualIndex + index;
                if (index == message.Length) break;
                if (message.Slice(index + 1).StartsWith(relativePathTrim))
                {
                    message = message.Slice(0, index);
                }
                index++;
            }

            return message.Trim();
        }
    }
}