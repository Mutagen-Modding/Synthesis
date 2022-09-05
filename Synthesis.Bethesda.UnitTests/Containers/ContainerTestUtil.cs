using Autofac;
using Noggog;
using Noggog.Autofac.Validation;
using Noggog.IO;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Placement;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Views;

namespace Synthesis.Bethesda.UnitTests.Containers;

public static class ContainerTestUtil
{
    public static void RegisterCommonMocks(ContainerBuilder builder)
    {
        builder.RegisterMock<IMainWindow>();
        builder.RegisterMock<IWindowPlacement>();
        builder.RegisterMock<IGithubPatcherIdentifier>();
        builder.RegisterMock<IWatchSingleAppArguments>();
        builder.RegisterMock<ISynthesisProfileSettings>();
    }
}