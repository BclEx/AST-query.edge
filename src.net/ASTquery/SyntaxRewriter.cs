using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

public class SyntaxRewriter : CSharpSyntaxRewriter
{
    readonly ILookup<string, Node> _alters;

    public SyntaxRewriter(IEnumerable<NodeAlter> alters)
    {
        _alters = alters.ToLookup(x => x.Method, x => x.Node);
    }

    public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
    {
        node = (CompilationUnitSyntax)base.VisitCompilationUnit(node);
        if (_alters.Contains("cunit.add"))
        {
            var nodes = _alters["cunit.add"].Select(x => (MemberDeclarationSyntax)x.ToSyntax()).ToArray();
            node = node.AddMembers(nodes);
        }
        return node;
    }

    public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var className = node.Identifier.ValueText;
        if (_alters.Contains("class.add"))
        {
            var nodes = _alters["class.add"].Select(x => (MemberDeclarationSyntax)x.ToSyntax()).ToArray();
            node.AddMembers(nodes);
        }
        if (_alters.Contains("class.remove"))
        {
            if (_alters["class.remove"].Any(x => x.FactoryMethodCall.Name == className))
                return null;
        }
        if (_alters.Contains("class.rename"))
        {
            var single = _alters["class.rename"].Select(x => x.FactoryMethodCall as MethodCall).SingleOrDefault(x => x.Name == className);
            if (single != null)
                node = node.WithIdentifier(SyntaxFactory.Identifier((string)single.Arguments.First())).NormalizeWhitespace();
        }
        return base.VisitClassDeclaration(node);
    }
}
