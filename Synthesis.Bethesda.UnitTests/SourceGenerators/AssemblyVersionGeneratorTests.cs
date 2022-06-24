using Microsoft.CodeAnalysis.Testing;
using Xunit;
using AssemblyVersionGenerator;
using Microsoft.CodeAnalysis;
using VerifyCS = Synthesis.Bethesda.UnitTests.Source_Generators.CSharpSourceGeneratorVerifier<AssemblyVersionGenerator.Generator>;

namespace Synthesis.Bethesda.UnitTests.SourceGenerators
{
	public class AssemblyVersionGeneratorTests
	{
		private async Task TypicalTest(
			string source,
			params (Type sourceGeneratorType, string filename, string content)[] content)
		{
			var testState = new VerifyCS.Test
			{
				TestState =
				{
					Sources =
					{
						("SomeFile.cs", source)
					},
				},
			};
			foreach (var c in content)
			{
				testState.TestState.GeneratedSources.Add(c);
			}
			await testState.RunAsync();
		}

		[Fact]
		public async Task BasicGeneration()
		{
			var source = @"

namespace System.Runtime.CompilerServices
{
    public sealed class IsExternalInit
    {
    }
}";
			var expected = @"using System;
using System.Diagnostics;
using System.Reflection;

#nullable enable

public record AssemblyVersions(string FullName, string PrettyName, string? ProductVersion)
{

    private static AssemblyVersions From(Assembly assemb)
    {
        var version = FileVersionInfo.GetVersionInfo(assemb.Location).ProductVersion;
        return new(assemb.FullName!, assemb.FullName!, version);
    }

    public static AssemblyVersions For<TTypeFromAssembly>()
    {
        var t = typeof(TTypeFromAssembly);

        throw new NotImplementedException();
    }
}
";

			await TypicalTest(source, 
				(typeof(Generator), "AssemblyVersions.g.cs", expected));
		}

		[Fact]
		public async Task BasicUsage()
		{
			var source = @"
namespace SomeNamespace
{
    public class MyClass
    {
        public AssemblyVersions Test() => AssemblyVersions.For<MyClass>();
    }
}

namespace System.Runtime.CompilerServices
{
    public sealed class IsExternalInit
    {
    }
}";
			var expected = @"using SomeNamespace;
using System;
using System.Diagnostics;
using System.Reflection;

#nullable enable

public record AssemblyVersions(string FullName, string PrettyName, string? ProductVersion)
{
    private static readonly AssemblyVersions _MyClass = From(typeof(MyClass).Assembly);

    private static AssemblyVersions From(Assembly assemb)
    {
        var version = FileVersionInfo.GetVersionInfo(assemb.Location).ProductVersion;
        return new(assemb.FullName!, assemb.FullName!, version);
    }

    public static AssemblyVersions For<TTypeFromAssembly>()
    {
        var t = typeof(TTypeFromAssembly);
        if (t == typeof(SomeNamespace.MyClass)) return _MyClass;

        throw new NotImplementedException();
    }
}
";

			await TypicalTest(source, 
				(typeof(Generator), "AssemblyVersions.g.cs", expected));
		}

		[Fact]
		public async Task NamespaceSpecified()
		{
			var source = @"
namespace SomeNamespace
{
    public class MyClass
    {
        public AssemblyVersions Test() => AssemblyVersions.For<SomeNamespace.MyClass>();
    }
}

namespace System.Runtime.CompilerServices
{
    public sealed class IsExternalInit
    {
    }
}";
			var expected = @"using SomeNamespace;
using System;
using System.Diagnostics;
using System.Reflection;

#nullable enable

public record AssemblyVersions(string FullName, string PrettyName, string? ProductVersion)
{
    private static readonly AssemblyVersions _MyClass = From(typeof(MyClass).Assembly);

    private static AssemblyVersions From(Assembly assemb)
    {
        var version = FileVersionInfo.GetVersionInfo(assemb.Location).ProductVersion;
        return new(assemb.FullName!, assemb.FullName!, version);
    }

    public static AssemblyVersions For<TTypeFromAssembly>()
    {
        var t = typeof(TTypeFromAssembly);
        if (t == typeof(SomeNamespace.MyClass)) return _MyClass;

        throw new NotImplementedException();
    }
}
";

			await TypicalTest(source, 
				(typeof(Generator), "AssemblyVersions.g.cs", expected));
		}

		[Fact]
		public async Task GenericPassed()
		{
			var source = @"
namespace SomeNamespace
{
    public class MyClass
    {
        public AssemblyVersions Test<T>() => AssemblyVersions.For<T>();
    }
}

namespace System.Runtime.CompilerServices
{
    public sealed class IsExternalInit
    {
    }
}";
			var expected = @"using System;
using System.Diagnostics;
using System.Reflection;

#nullable enable

public record AssemblyVersions(string FullName, string PrettyName, string? ProductVersion)
{

    private static AssemblyVersions From(Assembly assemb)
    {
        var version = FileVersionInfo.GetVersionInfo(assemb.Location).ProductVersion;
        return new(assemb.FullName!, assemb.FullName!, version);
    }

    public static AssemblyVersions For<TTypeFromAssembly>()
    {
        var t = typeof(TTypeFromAssembly);

        throw new NotImplementedException();
    }
}
";

			var testState = new VerifyCS.Test
			{
				TestState =
				{
					Sources =
					{
						("SomeFile.cs", source)
					},
				},
				CompilerDiagnostics = CompilerDiagnostics.Errors,
				ExpectedDiagnostics = { new DiagnosticResult(
					new DiagnosticDescriptor(
						"SY0001",
						"Unknown type passed to AssemblyVersions",
						"Unknown type passed to AssemblyVersions",
						"AssemblyVersions",
						DiagnosticSeverity.Error,
						true,
						description: "Need to pass a known concrete type, rather than a generic."))
					.WithSpan("SomeFile.cs", 6, 67, 6, 68)
				}
			};
			testState.TestState.GeneratedSources.Add((typeof(Generator), "AssemblyVersions.g.cs", expected));
			await testState.RunAsync();
		}
    }
}
