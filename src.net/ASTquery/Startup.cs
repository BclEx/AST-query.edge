#if !_Full
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Threading.Tasks;

namespace ASTquery
{
    public class Startup
    {
        public async Task<object> Parse(object input)
        {
            var quoter = new SyntaxBuilder();
            var tree = quoter.Parse((string)input);
            return tree.ToJson();
        }

        public async Task<object> Generate(dynamic input)
        {
            var jsonAst = (string)input.ast;
            var jsonAlters = (string)input.alters;
            var tree = (!string.IsNullOrEmpty(jsonAst) ? Node.FromJsonToSyntax(jsonAst) : SyntaxFactory.CompilationUnit());
            //Console.WriteLine(jsonAlters);
            var alters = (jsonAlters != null ? NodeAlter.FromJson(jsonAlters) : null);
            if (alters != null)
            {
                var rewriter = new SyntaxRewriter(alters);
                tree = rewriter.Visit(tree);
            }
            return Node.ToFullString(tree);
        }
    }
}
#endif