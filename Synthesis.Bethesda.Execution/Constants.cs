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
            Error = ErrorHandler,
            Converters =
            {
                new StringEnumConverter(),
                new AbstractConverter<SynthesisProfile, ISynthesisProfile>(),
            }
        };

        static void ErrorHandler(object? sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
        {
            if (args.ErrorContext.Member?.Equals(nameof(GithubPatcherSettings.PatcherVersioning)) ?? false)
            {
                args.ErrorContext.Handled = true;
            }
        }
        
        public class AbstractConverter<TReal, TAbstract> : JsonConverter 
            where TReal : TAbstract, new()
        {
            public override Boolean CanConvert(Type objectType)
                => objectType == typeof(TAbstract);

            public override Object? ReadJson(JsonReader reader, Type type, Object? value, JsonSerializer jser)
                => jser.Deserialize<TReal>(reader);

            public override void WriteJson(JsonWriter writer, Object? value, JsonSerializer jser)
                => jser.Serialize(writer, value);
        }
    }
}
