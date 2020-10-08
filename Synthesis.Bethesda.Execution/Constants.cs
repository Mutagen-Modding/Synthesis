using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution
{
    public static class Constants
    {
        public const string SettingsFileName = "PipelineSettings.json";
        public static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Auto,
        };

        static Constants()
        {
            JsonSettings.Converters.Add(new StringEnumConverter());
        }
    }
}
