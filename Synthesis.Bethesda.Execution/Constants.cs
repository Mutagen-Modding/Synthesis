using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
        };
        public static string TypicalExtraData => Path.Combine(Environment.CurrentDirectory, "Data");

        static Constants()
        {
            JsonSettings.Converters.Add(new StringEnumConverter());
        }
    }
}
