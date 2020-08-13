using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Synthesis.Bethesda.Execution.Settings;
using Noggog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using System.Linq.Expressions;
using System.Reactive.Linq;

namespace Synthesis.Bethesda.Execution.Patchers
{
    public class CodeSnippetPatcherRun : IPatcherRun
    {
        private static int UniquenessNumber;
        public const string ClassName = "PatcherClass";

        public string Name { get; }
        public string Code { get; }

        public string AssemblyName { get; }

        public IObservable<string> Output => Observable.Empty<string>();

        public IObservable<string> Error => Observable.Empty<string>();

        private Assembly? _assembly;

        public CodeSnippetPatcherRun(CodeSnippetPatcherSettings settings)
        {
            Name = settings.Nickname;
            Code = settings.Code;
            AssemblyName = $"{Name} - {System.Threading.Interlocked.Increment(ref UniquenessNumber)}";
        }

        public CodeSnippetPatcherRun(string name, Assembly assembly)
        {
            Name = name;
            Code = string.Empty;
            AssemblyName = assembly.FullName;
            _assembly = assembly;
        }

        public async Task Run(RunSynthesisPatcher settings, CancellationToken? cancel = null)
        {
            if (_assembly == null)
            {
                throw new ArgumentException("Tried to run a code snippet patcher that did not have an assembly.");
            }
            Type? type = _assembly.GetType(ClassName);
            if (type == null)
            {
                throw new ArgumentException("Could not find compiled class within assembly.");
            }
            var patcherCodeClass = System.Activator.CreateInstance(type);
            var userPrefs = new UserPreferences()
            {
                Cancel = cancel ?? CancellationToken.None
            };
            var synthesisState = ConstructStateFactory(settings.GameRelease)(settings, userPrefs);
            Task t = (Task)type.InvokeMember("Run",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                patcherCodeClass,
                new[]
                {
                    synthesisState,
                });
            await t;
            synthesisState.PatchMod.WriteToBinaryParallel(
                settings.OutputPath,
                new BinaryWriteParameters()
                {
                    ModKey = BinaryWriteParameters.ModKeyOption.NoCheck,
                    MastersListOrdering = new BinaryWriteParameters.MastersListOrderingByLoadOrder(synthesisState.LoadOrder)
                });
        }

        public async Task Prep(GameRelease release, CancellationToken? cancel = null)
        {
            if (_assembly != null) return;
            cancel ??= CancellationToken.None;

            var emitResult = Compile(release, cancel.Value, out _assembly);
            if (!emitResult.Success)
            {
                var err = emitResult.Diagnostics.First(d => d.Severity == DiagnosticSeverity.Error);
                throw new ArgumentException(err.ToString());
            }
        }

        internal static Func<RunSynthesisPatcher, UserPreferences?, ISynthesisState> ConstructStateFactory(GameRelease release)
        {
            var regis = release.ToCategory().ToModRegistration();
            var cliSettingsParam = Expression.Parameter(typeof(RunSynthesisPatcher));
            var userPrefs = Expression.Parameter(typeof(UserPreferences));
            MethodCallExpression callExp = Expression.Call(
                typeof(Mutagen.Bethesda.Synthesis.Internal.Utility),
                nameof(Mutagen.Bethesda.Synthesis.Internal.Utility.ToState),
                new Type[]
                {
                    regis.SetterType,
                    regis.GetterType,
                },
                cliSettingsParam,
                userPrefs);
            LambdaExpression lambda = Expression.Lambda(callExp, cliSettingsParam, userPrefs);
            var deleg = lambda.Compile();
            return (RunSynthesisPatcher settings, UserPreferences? prefs) =>
            {
                return (ISynthesisState)deleg.DynamicInvoke(settings, prefs);
            };
        }

        public static EmitResult Compile(GameRelease release, string assemblyName, string code, CancellationToken cancel, out MemoryStream assemblyStream)
        {
            var gameCategory = release.ToCategory();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"using System;");
            sb.AppendLine($"using Noggog;");
            sb.AppendLine($"using System.Threading;");
            sb.AppendLine($"using System.Threading.Tasks;");
            sb.AppendLine($"using System.Linq;");
            sb.AppendLine($"using System.IO;");
            sb.AppendLine($"using System.Collections;");
            sb.AppendLine($"using System.Collections.Generic;");
            sb.AppendLine($"using Mutagen.Bethesda.Synthesis;");
            sb.AppendLine($"using Mutagen.Bethesda;");
            sb.AppendLine($"using Mutagen.Bethesda.{release.ToCategory()};");

            sb.AppendLine($"public class {ClassName}");
            sb.AppendLine("{");
            sb.AppendLine($"public async Task Run(Mutagen.Bethesda.Synthesis.SynthesisState<Mutagen.Bethesda.{gameCategory}.I{gameCategory}Mod, Mutagen.Bethesda.{gameCategory}.I{gameCategory}ModGetter> state)");
            sb.AppendLine("{");
            sb.AppendLine(code);
            sb.AppendLine("}");
            sb.AppendLine("}");

            code = sb.ToString();

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);

            var options = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release);

            Compilation compilation = CSharpCompilation.Create(assemblyName: assemblyName, options: options)
              .AddSyntaxTrees(syntaxTree)
              .AddReferences(new[]
              {
                  MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                  MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                  MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
                  MetadataReference.CreateFromFile(typeof(File).Assembly.Location),
                  MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
                  MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                  MetadataReference.CreateFromFile(Assembly.Load("Loqui").Location),
                  MetadataReference.CreateFromFile(Assembly.Load("Noggog.CSharpExt").Location),
                  MetadataReference.CreateFromFile(Assembly.Load("Mutagen.Bethesda.Kernel").Location),
                  MetadataReference.CreateFromFile(Assembly.Load("Mutagen.Bethesda.Core").Location),
                  MetadataReference.CreateFromFile(Assembly.Load("Mutagen.Bethesda.Synthesis").Location),
              });
            foreach (var game in EnumExt.GetValues<GameCategory>())
            {
                compilation = compilation.AddReferences(MetadataReference.CreateFromFile(Assembly.Load($"Mutagen.Bethesda.{game}").Location));
            }

            assemblyStream = new MemoryStream();
            return compilation.Emit(assemblyStream, cancellationToken: cancel);
        }

        public EmitResult Compile(GameRelease release, CancellationToken cancel, out Assembly? assembly)
        {
            var emit = Compile(release, assemblyName: AssemblyName, code: Code, cancel, out var stream);
            if (emit.Success)
            {
                assembly = Assembly.Load(stream.ToArray());
            }
            else
            {
                assembly = null;
            }
            return emit;
        }

        public void Dispose()
        {
            // ToDo
            // Will eventually want to unload the assembly
        }
    }
}
