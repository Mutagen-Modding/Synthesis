using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Synthesis.Bethesda.Execution.Settings;
using System.Diagnostics.CodeAnalysis;
using Mutagen.Bethesda.Json;
using Noggog.Json;
using Synthesis.Bethesda.Execution.Settings.V2;

namespace Synthesis.Bethesda.Execution;

[ExcludeFromCodeCoverage]
public static class Constants
{
    public static readonly JsonSerializerSettings JsonSettings = new()
    {
        TypeNameHandling = TypeNameHandling.Auto,
        Error = ErrorHandler,
        Converters =
        {
            new StringEnumConverter(),
            new AbstractConverter<SynthesisProfile, ISynthesisProfileSettings>(),
        }
    };

    static Constants()
    {
        JsonSettings.AddMutagenConverters();
        JsonSettings.AddNoggogConverters();
    }

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