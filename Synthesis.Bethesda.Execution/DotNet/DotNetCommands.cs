using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Noggog;

namespace Synthesis.Bethesda.Execution.DotNet
{
    public static class DotNetCommands
    {
        public const int MinVersion = 5;

        public static bool IsApplicableErrorLine(ReadOnlySpan<char> str)
        {
            return str.Contains(": error ", StringComparison.Ordinal);
        }

        public static void PrintErrorMessage(ReadOnlySpan<char> message, ReadOnlySpan<char> relativePathTrim, ReadOnlySpanAction<char, object?> toDo)
        {
            foreach (var item in message.SplitLines())
            {
                if (!IsApplicableErrorLine(item.Line)) continue;
                toDo(TrimErrorMessage(item.Line, relativePathTrim), null);
            }
        }

        public static ReadOnlySpan<char> TrimErrorMessage(ReadOnlySpan<char> message, ReadOnlySpan<char> relativePathTrim)
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
