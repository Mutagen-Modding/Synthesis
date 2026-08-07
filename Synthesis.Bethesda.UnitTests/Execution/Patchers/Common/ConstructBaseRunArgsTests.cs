using Shouldly;
using Mutagen.Bethesda.Strings;
using Mutagen.Bethesda.Synthesis.CLI;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Common;

public class ConstructBaseRunArgsTests
{
    [Theory, SynthAutoData]
    public void ForwardsEverySharedSetting(
        Language language,
        RunSynthesisPatcher settings,
        ConstructBaseRunArgs sut)
    {
        settings.TargetLanguage = language.ToString();
        var ret = sut.Construct(settings);
        foreach (var source in typeof(RunSynthesisPatcher).GetProperties())
        {
            var target = typeof(RunSynthesisMutagenPatcher).GetProperty(source.Name);
            if (target == null) continue;
            $"{source.Name}: {target.GetValue(ret)}"
                .ShouldBe($"{source.Name}: {source.GetValue(settings)}");
        }
    }

    [Theory, SynthAutoData]
    public void SetsExtraDataToProviderResult(
        DirectoryPath dir,
        Language language,
        RunSynthesisPatcher settings,
        ConstructBaseRunArgs sut)
    {
        settings.TargetLanguage = language.ToString();
        sut.ExtraDataPathProvider.Path.Returns(dir);
        sut.Construct(settings)
            .ExtraDataFolder.ShouldBe(dir);
    }
}
