using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AssemblyVersionGenerator
{
    public class AssemblyVersionReceiver : ISyntaxReceiver
    {
        public List<NameSyntax> Located = new();
        
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is not InvocationExpressionSyntax invocation) return;
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccessExpressionSyntax) return;
            if (memberAccessExpressionSyntax.Expression is not IdentifierNameSyntax nameSyntax) return;
            if (nameSyntax.ToString() != "AssemblyVersions") return;
            if (memberAccessExpressionSyntax.Name is not GenericNameSyntax genName) return;
            if (genName.Identifier.ToString() != "For") return;
            if (genName.TypeArgumentList.Arguments.Count != 1) return;
            if (genName.TypeArgumentList.Arguments[0] is not NameSyntax typeNameSyntax) return;
            Located.Add(typeNameSyntax);
        }
    }
}