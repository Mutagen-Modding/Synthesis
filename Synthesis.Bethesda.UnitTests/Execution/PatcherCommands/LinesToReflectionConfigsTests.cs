using FluentAssertions;
using Synthesis.Bethesda.DTO;
using Synthesis.Bethesda.Execution.Settings.Json;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.PatcherCommands;

public class LinesToReflectionConfigsTests
{
    [Theory, SynthAutoData]
    public void Typical(
        LinesToReflectionConfigsParser sut)
    {
        var ret = sut.Parse(new string[]
        {
            "{\"Configs\":[{\"TypeName\":\"FaceFixer.Settings\",\"Nickname\":\"Settings\",\"Path\":\"settings.json\"}]}"
        });

        ret.Configs.Should().Equal(
            new ReflectionSettingsConfig(
                TypeName: "FaceFixer.Settings",
                Nickname: "Settings",
                Path: "settings.json"));
    }
        
    [Theory, SynthAutoData]
    public void Multiple(
        LinesToReflectionConfigsParser sut)
    {
        var ret = sut.Parse(new string[]
        {
            "{\"Configs\":[\n"
            + "{\"TypeName\":\"FaceFixer.Settings\",\"Nickname\":\"Settings\",\"Path\":\"settings.json\"},\n"
            + "{\"TypeName\":\"OtherClass.Settings\",\"Nickname\":\"OtherSettings\",\"Path\":\"othersettings.json\"}\n"
            + "]}"
        });

        ret.Configs.Should().Equal(
            new ReflectionSettingsConfig(
                TypeName: "FaceFixer.Settings",
                Nickname: "Settings",
                Path: "settings.json"),
            new ReflectionSettingsConfig(
                TypeName: "OtherClass.Settings",
                Nickname: "OtherSettings",
                Path: "othersettings.json"));
    }
        
    [Theory, SynthAutoData]
    public void UsingLaunchSettings(
        LinesToReflectionConfigsParser sut)
    {
        var ret = sut.Parse(new string[]
        {
            "Using launch settings from ",
            "{\"Configs\":[{\"TypeName\":\"FaceFixer.Settings\",\"Nickname\":\"Settings\",\"Path\":\"settings.json\"}]}"
        });

        ret.Configs.Should().Equal(
            new ReflectionSettingsConfig(
                TypeName: "FaceFixer.Settings",
                Nickname: "Settings",
                Path: "settings.json"));
    }
        
    [Theory, SynthAutoData]
    public void AnsiCodes(
        LinesToReflectionConfigsParser sut)
    {
        var ret = sut.Parse(new string[]
        {
            "\u001b]9;4;3;\u001b\\\u001b]9;4;0;\u001b{\"Configs\":[{\"TypeName\":\"FaceFixer.Settings\",\"Nickname\":\"Settings\",\"Path\":\"settings.json\"}]}"
        });

        ret.Configs.Should().Equal(
            new ReflectionSettingsConfig(
                TypeName: "FaceFixer.Settings",
                Nickname: "Settings",
                Path: "settings.json"));
    }
}