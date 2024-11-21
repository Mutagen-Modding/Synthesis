using FluentAssertions;
using Noggog;
using Synthesis.Bethesda.Execution.FileAssociations;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.FileAssociations;

public class AddGitPatcherPassthrough
{
    [Theory, SynthAutoData]
    public void Passthrough(
        AddGitPatcherInstruction add,
        FilePath missingPath,
        ExportGitAddFile exportGitAddFile,
        ImportMetaDto importMetaDto)
    {
        exportGitAddFile.ExportAsFile(missingPath, add.Url, add.SelectedProject);
        var dto = importMetaDto.Import(missingPath);
        dto.Should().Be(new MetaFileDto()
        {
            AddGitPatcher = add
        });
    }
}