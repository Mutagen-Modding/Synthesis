using Loqui;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Mutagen.Bethesda.Oblivion;
using Mutagen.Bethesda.Synthesis.Core.Settings;
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

namespace Mutagen.Bethesda.Synthesis.Core.Patchers
{
    public class CodeSnippetPatcher : IPatcher
    {
        private static int UniquenessNumber;
        public const string ClassName = "PatcherClass";

        public string Name { get; }
        public string Code { get; }

        private readonly TaskCompletionSource _complete = new TaskCompletionSource();
        public Task Complete => _complete.Task;

        public string AssemblyName { get; }

        private Assembly? _assembly;

        public CodeSnippetPatcher(CodeSnippetPatcherSettings settings)
        {
            Name = settings.Nickname;
            Code = settings.Code;
            AssemblyName = $"{Name} - {System.Threading.Interlocked.Increment(ref UniquenessNumber)}";
        }

        public async Task Run(ModPath? sourcePath, ModPath outputPath)
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
            Task t = (Task)type.InvokeMember("Run",
                BindingFlags.Default | BindingFlags.InvokeMethod,
                null,
                patcherCodeClass,
                new[]
                {
                    sourcePath,
                    outputPath,
                });
            await t;
        }

        public async Task Prep(CancellationToken? cancel = null)
        {
            cancel ??= CancellationToken.None;

            var emitResult = Compile(cancel.Value, out _assembly);
            if (!emitResult.Success)
            {
                var err = emitResult.Diagnostics.First(d => d.Severity == DiagnosticSeverity.Error);
                throw new ArgumentException(err.ToString());
            }
        }

        public EmitResult Compile(CancellationToken cancel, out Assembly? assembly)
        {
            FileGeneration fg = new FileGeneration();
            fg.AppendLine($"using System;");
            fg.AppendLine($"using System.Threading;");
            fg.AppendLine($"using System.Threading.Tasks;");
            fg.AppendLine($"using System.Linq;");
            fg.AppendLine($"using System.IO;");
            fg.AppendLine($"using System.Collections;");
            fg.AppendLine($"using System.Collections.Generic;");
            fg.AppendLine($"using Mutagen.Bethesda;");
            foreach (var game in EnumExt.GetValues<GameCategory>())
            {
                fg.AppendLine($"using Mutagen.Bethesda.{game};");
            }

            fg.AppendLine($"public class {ClassName}");
            using (new BraceWrapper(fg))
            {
                fg.AppendLine($"public async Task Run({nameof(ModPath)}? sourcePath, {nameof(ModPath)} outputPath)");
                using (new BraceWrapper(fg))
                {
                    fg.AppendLine(this.Code);
                }
            }

            var code = fg.ToString();

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);

            var options = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release);

            Compilation compilation = CSharpCompilation.Create(assemblyName: AssemblyName, options: options)
              .AddSyntaxTrees(syntaxTree)
              .AddReferences(new[]
              {
                  MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                  MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                  MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
                  MetadataReference.CreateFromFile(typeof(File).Assembly.Location),
                  MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
                  MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                  MetadataReference.CreateFromFile(Assembly.Load("Mutagen.Bethesda.Core").Location),
              });
            foreach (var game in EnumExt.GetValues<GameCategory>())
            {
                compilation = compilation.AddReferences(MetadataReference.CreateFromFile(Assembly.Load($"Mutagen.Bethesda.{game}").Location));
            }

            var stream = new MemoryStream();
            var emit = compilation.Emit(stream, cancellationToken: cancel);
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
            // Will eventually want to unload the assembly
        }
    }
}
