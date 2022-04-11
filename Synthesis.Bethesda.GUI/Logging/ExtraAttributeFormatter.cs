using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog.Events;
using Serilog.Formatting;

namespace Synthesis.Bethesda.GUI.Logging;

public class ExtraAttributeFormatter : ITextFormatter
{
    private readonly HashSet<string> _propertiesToOmit;

    public ExtraAttributeFormatter(params string[] propertiesToOmit)
    {
        _propertiesToOmit = propertiesToOmit.ToHashSet();
    }
        
    public void Format(LogEvent logEvent, TextWriter output)
    {
        try
        {
            TimeFormatter.Print(output);

            foreach (var prop in logEvent.Properties)
            {
                if (_propertiesToOmit.Contains(prop.Key)) continue;
                if (EventUsingProperty(logEvent, prop.Key)) continue;
                output.Write("[");
                WriteProperty(prop.Value, output);
                output.Write("]");
            }
            output.Write(" ");
            foreach (var token in logEvent.MessageTemplate.Tokens)
            {
                var text = logEvent.MessageTemplate.Text.AsSpan().Slice(token.StartIndex, token.Length);
                if (text.Length > 0
                    && text[0] == '{'
                    && text[^1] == '}'
                    && logEvent.Properties.TryGetValue(text[1..^1].ToString(), out var prop))
                {
                    WriteProperty(prop, output);
                }
                else
                {
                    output.Write(text);
                }
            }
            output.Write(Environment.NewLine);
            if (logEvent.Exception != null)
            {
                output.Write(logEvent.Exception);
                output.Write(Environment.NewLine);
            }
        }
        catch (Exception)
        {
            output.Write("Error in logging formatter");
            output.Write(Environment.NewLine);
        }
    }

    private static void WriteProperty(LogEventPropertyValue prop, TextWriter output)
    {
        if (prop is ScalarValue scalar)
        {
            output.Write(scalar.Value);
        }
        else
        {
            prop.Render(output);
        }
    }

    private static bool EventUsingProperty(LogEvent logEvent, ReadOnlySpan<char> key)
    {
        if (key == "ThreadId") return true;
        foreach (var token in logEvent.MessageTemplate.Tokens)
        {
            var text = logEvent.MessageTemplate.Text.AsSpan().Slice(token.StartIndex, token.Length);
            if (text.Length == 0) continue;
            if (text[0] != '{') continue;
            if (text[^1] != '}') continue;
            if (text[1..^1].Equals(key, StringComparison.CurrentCulture)) return true;
        }
        return false;
    }
}