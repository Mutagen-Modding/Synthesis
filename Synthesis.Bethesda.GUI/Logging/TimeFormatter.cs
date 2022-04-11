using System;
using System.IO;
using Serilog.Events;
using Serilog.Formatting;

namespace Synthesis.Bethesda.GUI.Logging;

public class TimeFormatter : ITextFormatter
{
    public static void Print(TextWriter output)
    {
        var timeSpan = DateTime.Now - Log.StartTime;
        output.Write("[");
        output.Write((int)timeSpan.TotalSeconds);
        output.Write(".");
        output.Write(timeSpan.Milliseconds / 100);
        output.Write("]");
    }

    public void Format(LogEvent logEvent, TextWriter output)
    {
        Print(output);
        var msg = logEvent.RenderMessage();
        output.WriteLine(msg);
    }
}