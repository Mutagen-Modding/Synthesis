using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;

namespace AssemblyVersionGenerator
{
    [Generator]
    public class Generator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new AssemblyVersionReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not AssemblyVersionReceiver receiver) return;
            if (context.CancellationToken.IsCancellationRequested) return;

            Dictionary<IAssemblySymbol, HashSet<INamedTypeSymbol>> targets = new(SymbolEqualityComparer.Default);
            var namespaces = new HashSet<string>()
            {
                "System",
                "System.Reflection",
                "System.Diagnostics"
            };

            foreach (var identifier in receiver.Located)
            {
                var model = context.Compilation.GetSemanticModel(identifier.SyntaxTree.GetRoot().SyntaxTree);
                var typeInfo = model.GetTypeInfo(identifier, context.CancellationToken);
                if (typeInfo.Type is not INamedTypeSymbol namedTypeSymbol)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "SY0001",
                            "Unknown type passed to AssemblyVersions",
                            "Unknown type passed to AssemblyVersions",
                            "AssemblyVersions",
                            DiagnosticSeverity.Error,
                            true,
                            description: "Need to pass a known concrete type, rather than a generic."), identifier.GetLocation()));
                    continue;
                }

                if (!targets.TryGetValue(namedTypeSymbol.ContainingAssembly, out var set))
                {
                    set = new(SymbolEqualityComparer.Default);
                    targets[namedTypeSymbol.ContainingAssembly] = set;
                }

                set.Add(namedTypeSymbol);
                namespaces.Add(namedTypeSymbol.ContainingNamespace.ToString());
            }

            var sb = new StringBuilder();
            foreach (var ns in namespaces.OrderBy(x => x))
            {
                sb.AppendLine($"using {ns};");
            }
            sb.AppendLine(@"
#nullable enable

public record AssemblyVersions(string PrettyName, string? ProductVersion)
{");
            foreach (var pair in targets)
            {
                INamedTypeSymbol? first = null;
                foreach (var item in pair.Value)
                {
                    if (first == null)
                    {
                        var attrs = item.ContainingAssembly.GetAttributes();
                        var vers = item.ContainingAssembly.GetAttributes()
                            .Where(x => x.AttributeClass?.Name == "AssemblyInformationalVersionAttribute")
                            .FirstOrDefault()?
                            .ConstructorArguments[0].Value?.ToString() ?? "0.0.0.0";
                        var pretty = item.ContainingAssembly.GetAttributes()
                            .Where(x => x.AttributeClass?.Name == "AssemblyTitleAttribute")
                            .FirstOrDefault()?
                            .ConstructorArguments[0].Value?.ToString() ?? "<global assembly>";
                        sb.AppendLine($"    private static readonly AssemblyVersions _{item.Name} = new(\"{pretty}\", \"{vers}\");");
                        first = item;
                    }
                    else
                    {
                        sb.AppendLine($@"    private static readonly AssemblyVersions _{item.Name} = _{first.Name};");
                    }
                }
            }

            sb.AppendLine(@"
    public static AssemblyVersions For<TTypeFromAssembly>()
    {
        var t = typeof(TTypeFromAssembly);");

            foreach (var item in targets.SelectMany(x => x.Value))
            {
                sb.AppendLine($"        if (t == typeof({item.ContainingNamespace}.{item.Name})) return _{item.Name};");
            }
            sb.AppendLine(@"
        throw new NotImplementedException();
    }
}");
        
            context.AddSource("AssemblyVersions.g.cs", sb.ToString());
        }
    }
}