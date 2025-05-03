using Shouldly;
using Noggog.Testing.TestClassData;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git;

public class GitPatcherVersioningTests
{
    public static IEnumerable<object[]> TrueFalse => MemberData.TestPerItem(true, false);

    public static IEnumerable<object[]> DoubleTrueFalse => MemberData.AlternatingBools(2);
        
    [Theory, SynthMemberData(nameof(TrueFalse))]
    public void TagVersioningWithAutoForwardsTag(
        bool autoBranch,
        string tag,
        string commit,
        string branch)
    {
        var ret = GitPatcherVersioning.Factory(
            PatcherVersioningEnum.Tag,
            tag,
            commit,
            branch,
            autoTag: true,
            autoBranch: autoBranch);
        ret.Versioning.ShouldBe(PatcherVersioningEnum.Tag);
        ret.Target.ShouldBe(tag);
    }
        
    [Theory, SynthMemberData(nameof(TrueFalse))]
    public void TagVersioningWithoutAutoReturnsCommit(
        bool autoBranch,
        string tag,
        string commit,
        string branch)
    {
        var ret = GitPatcherVersioning.Factory(
            PatcherVersioningEnum.Tag,
            tag,
            commit,
            branch,
            autoTag: false,
            autoBranch: autoBranch);
        ret.Versioning.ShouldBe(PatcherVersioningEnum.Commit);
        ret.Target.ShouldBe(commit);
    }
        
    [Theory, SynthMemberData(nameof(DoubleTrueFalse))]
    public void CommitVersioningReturnsCommit(
        bool autoBranch,
        bool autoTag,
        string tag,
        string commit,
        string branch)
    {
        var ret = GitPatcherVersioning.Factory(
            PatcherVersioningEnum.Commit,
            tag,
            commit,
            branch,
            autoTag: autoTag,
            autoBranch: autoBranch);
        ret.Versioning.ShouldBe(PatcherVersioningEnum.Commit);
        ret.Target.ShouldBe(commit);
    }
        
    [Theory, SynthMemberData(nameof(TrueFalse))]
    public void BranchVersioningWithAutoForwardsBranch(
        bool autoTag,
        string tag,
        string commit,
        string branch)
    {
        var ret = GitPatcherVersioning.Factory(
            PatcherVersioningEnum.Branch,
            tag,
            commit,
            branch,
            autoTag: autoTag,
            autoBranch: true);
        ret.Versioning.ShouldBe(PatcherVersioningEnum.Branch);
        ret.Target.ShouldBe(branch);
    }
        
    [Theory, SynthMemberData(nameof(TrueFalse))]
    public void BranchVersioningWithoutAutoReturnsCommit(
        bool autoTag,
        string tag,
        string commit,
        string branch)
    {
        var ret = GitPatcherVersioning.Factory(
            PatcherVersioningEnum.Branch,
            tag,
            commit,
            branch,
            autoTag: autoTag,
            autoBranch: false);
        ret.Versioning.ShouldBe(PatcherVersioningEnum.Commit);
        ret.Target.ShouldBe(commit);
    }
}