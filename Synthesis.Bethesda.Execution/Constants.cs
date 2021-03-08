using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Synthesis.Bethesda.Execution.Settings;
using System;
using System.IO;

namespace Synthesis.Bethesda.Execution
{
    public static class Constants
    {
        public static readonly JsonSerializerSettings JsonSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Error = ErrorHandler
        };

        static Constants()
        {
            JsonSettings.Converters.Add(new StringEnumConverter());
        }

        static void ErrorHandler(object? sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
        {
            if (args.ErrorContext.Member?.Equals(nameof(GithubPatcherSettings.PatcherVersioning)) ?? false)
            {
                args.ErrorContext.Handled = true;
            }
        }
    }
}
