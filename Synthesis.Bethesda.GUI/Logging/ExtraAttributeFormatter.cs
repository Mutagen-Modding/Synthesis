using Serilog.Events;
using Serilog.Formatting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Synthesis.Bethesda.GUI
{
    public class ExtraAttributeFormatter : ITextFormatter
    {
        public void Format(LogEvent logEvent, TextWriter output)
        {
            try
            {
                var timeSpan = DateTime.Now - Log.StartTime;
                output.Write("[");
                output.Write((int)timeSpan.TotalSeconds);
                output.Write(".");
                output.Write(timeSpan.Milliseconds / 100);
                output.Write("]");

                foreach (var prop in logEvent.Properties)
                {
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
}
