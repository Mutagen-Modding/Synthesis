using System.Text.RegularExpressions;

namespace Synthesis.Bethesda.Execution.Reporters.Classifications;

/// <summary>
/// Shared detection of file access denied errors within captured process output.
/// </summary>
public static class AccessDeniedDetection
{
    // Pattern for IOException with file path
    private static readonly Regex IoExceptionPattern = new Regex(
        @"System\.IO\.IOException:\s+The process cannot access the file\s+'([^']+)'\s+because it is being used by another process",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Pattern for Win32Exception access denied (no file path)
    private static readonly Regex Win32ExceptionPattern = new Regex(
        @"System\.ComponentModel\.Win32Exception\s*\(\d+\):\s*Access is denied",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool TryFind(IEnumerable<string>? lines, out string filePath)
    {
        if (lines != null)
        {
            foreach (var line in lines)
            {
                var ioMatch = IoExceptionPattern.Match(line);
                if (ioMatch.Success)
                {
                    filePath = ioMatch.Groups[1].Value;
                    return true;
                }

                if (Win32ExceptionPattern.IsMatch(line))
                {
                    filePath = string.Empty;
                    return true;
                }
            }
        }

        filePath = string.Empty;
        return false;
    }
}
