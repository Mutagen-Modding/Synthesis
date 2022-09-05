using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Testing.AutoData;
using Noggog.Testing.AutoFixture;
using Noggog.GitRepository;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.AutoData;

public class SynthAutoData : AutoDataAttribute
{
    public SynthAutoData(
        bool ConfigureMembers = true, 
        bool UseMockFileSystem = true,
        bool GenerateDelegates = false,
        bool UseMockRepositoryProvider = false,
        bool OmitAutoProperties = false)
        : base(() =>
        {
            return Factory(
                UseMockFileSystem: UseMockFileSystem,
                ConfigureMembers: ConfigureMembers,
                GenerateDelegates: GenerateDelegates,
                UseMockRepositoryProvider: UseMockRepositoryProvider,
                OmitAutoProperties: OmitAutoProperties);
        })
    {
    }

    public static AutoFixture.IFixture Factory(
        bool ConfigureMembers = true, 
        bool UseMockFileSystem = true,
        bool GenerateDelegates = false,
        bool UseMockRepositoryProvider = false,
        bool OmitAutoProperties = false)
    {
        return new AutoFixture.Fixture()
            .Customize(new SynthAutoDataCustomization(
                useMockFilesystem: UseMockFileSystem,
                configureMembers: ConfigureMembers,
                generateDelegates: GenerateDelegates,
                useMockRepositoryProvider: UseMockRepositoryProvider,
                omitAutoProperties: OmitAutoProperties));
    }
}
    
public class SynthInlineData : CompositeDataAttribute
{
    public SynthInlineData(
        params object[] ExtraParameters)
        : base(
            new InlineDataAttribute(ExtraParameters), 
            new SynthAutoData())
    {
    }
}
    
public class SynthMemberData : MemberAutoDataImprovedAttribute
{
    public SynthMemberData(string memberName, params object[] parameters)
        : base(memberName: memberName, parameters, () =>
        {
            return SynthAutoData.Factory();
        })
    {
    }
}
    
public class SynthCustomInlineData : CompositeDataAttribute
{
    public SynthCustomInlineData(
        bool ConfigureMembers = true, 
        bool UseMockFileSystem = true,
        bool GenerateDelegates = false,
        bool UseMockRepositoryProvider = false,
        bool OmitAutoProperties = false,
        params object[] ExtraParameters)
        : base(
            new InlineDataAttribute(ExtraParameters), 
            new SynthAutoData(
                ConfigureMembers: ConfigureMembers, 
                UseMockFileSystem: UseMockFileSystem,
                GenerateDelegates: GenerateDelegates,
                UseMockRepositoryProvider: UseMockRepositoryProvider,
                OmitAutoProperties: OmitAutoProperties))
    {
    }
}
    
public class SynthAutoDataCustomization : ICustomization
{
    private readonly bool _useMockFilesystem;
    private readonly bool _generateDelegates;
    private readonly bool _useMockRepositoryProvider;
    private readonly bool _omitAutoProperties;
    private readonly bool _configureMembers;

    public SynthAutoDataCustomization(
        bool configureMembers, 
        bool useMockFilesystem,
        bool generateDelegates,
        bool useMockRepositoryProvider,
        bool omitAutoProperties)
    {
        _useMockFilesystem = useMockFilesystem;
        _generateDelegates = generateDelegates;
        _useMockRepositoryProvider = useMockRepositoryProvider;
        _omitAutoProperties = omitAutoProperties;
        _configureMembers = configureMembers;
    }
        
    public void Customize(IFixture fixture)
    {
        var autoMock = new AutoNSubstituteCustomization()
        {
            ConfigureMembers = _configureMembers,
            GenerateDelegates = _generateDelegates
        };
        fixture.Customize(autoMock);
        fixture.OmitAutoProperties = _omitAutoProperties;
        fixture.Customize(new MutagenBaseCustomization());
        fixture.Customize(new MutagenReleaseCustomization(GameRelease.SkyrimSE));
        fixture.Customize(new DefaultCustomization(_useMockFilesystem));
        if (_useMockRepositoryProvider)
        {
            fixture.Register<IProvideRepositoryCheckouts>(
                () => new ProvideRepositoryCheckouts(
                    fixture.Create<ILogger<ProvideRepositoryCheckouts>>(),
                    new GitRepositoryFactory()));
        }
    }
}