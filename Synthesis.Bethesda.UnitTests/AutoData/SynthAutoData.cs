using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using Noggog.Testing.AutoFixture;
using Serilog;
using Synthesis.Bethesda.Execution.GitRepository;
using Xunit;
using GameRelease = Mutagen.Bethesda.GameRelease;

namespace Synthesis.Bethesda.UnitTests.AutoData
{
    public class SynthAutoData : AutoDataAttribute
    {
        public SynthAutoData(
            bool ConfigureMembers = false, 
            bool UseMockFileSystem = true,
            bool GenerateDelegates = false,
            bool UseMockRepositoryProvider = true)
            : base(() =>
            {
                return new AutoFixture.Fixture()
                    .Customize(new SynthAutoDataCustomization(
                        useMockFilesystem: UseMockFileSystem,
                        configureMembers: ConfigureMembers,
                        generateDelegates: GenerateDelegates,
                        useMockRepositoryProvider: UseMockRepositoryProvider));
            })
        {
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
    
    public class SynthCustomInlineData : CompositeDataAttribute
    {
        public SynthCustomInlineData(
            bool ConfigureMembers = false, 
            bool UseMockFileSystem = true,
            bool GenerateDelegates = false,
            bool UseMockRepositoryProvider = true,
            params object[] ExtraParameters)
            : base(
                new InlineDataAttribute(ExtraParameters), 
                new SynthAutoData(
                    ConfigureMembers: ConfigureMembers, 
                    UseMockFileSystem: UseMockFileSystem,
                    GenerateDelegates: GenerateDelegates,
                    UseMockRepositoryProvider: UseMockRepositoryProvider))
        {
        }
    }
    
    public class SynthAutoDataCustomization : ICustomization
    {
        private readonly bool _useMockFilesystem;
        private readonly bool _generateDelegates;
        private readonly bool _useMockRepositoryProvider;
        private readonly bool _configureMembers;

        public SynthAutoDataCustomization(
            bool configureMembers, 
            bool useMockFilesystem,
            bool generateDelegates,
            bool useMockRepositoryProvider)
        {
            _useMockFilesystem = useMockFilesystem;
            _generateDelegates = generateDelegates;
            _useMockRepositoryProvider = useMockRepositoryProvider;
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
            fixture.OmitAutoProperties = true;
            fixture.Customizations.Add(new FileSystemBuilder(_useMockFilesystem));
            fixture.Customizations.Add(new SchedulerBuilder());
            fixture.Customizations.Add(new PathBuilder());
            fixture.Customizations.Add(new ProcessBuilder());
            fixture.Customizations.Add(new CancellationBuilder());
            fixture.Behaviors.Add(new ObservableEmptyBehavior());
            if (_useMockRepositoryProvider)
            {
                fixture.Register<IProvideRepositoryCheckouts>(
                    () => new ProvideRepositoryCheckouts(fixture.Create<ILogger>()));
            }
        }
    }
}