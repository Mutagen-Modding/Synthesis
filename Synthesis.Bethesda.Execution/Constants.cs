using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Synthesis.Bethesda.Execution.Settings;
using System;
using System.IO;

namespace Synthesis.Bethesda.Execution
{
    public static class Constants
    {
        public readonly static string WorkingDirectory = Path.Combine(Path.GetTempPath(), "Synthesis")!;
        public static string ProfileWorkingDirectory(string id) => Path.Combine(WorkingDirectory, id, "Workspace");
        public const string SettingsFileName = "PipelineSettings.json";
        public static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Error = ErrorHandler
        };
        public static string TypicalExtraData => Path.Combine(Environment.CurrentDirectory, "Data");

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
