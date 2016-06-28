#if !_LIB
using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace QuoterHost
{
    class Program
    {
        static void Main(string[] args)
        {
            //var sourceText = "class C{ public static void Print() {} }";

            //            var sourceText = @"
            //public class Startup
            //{
            //    public object Invoke(object input)
            //    {
            //          return (int)input + 7;
            //    }
            //}";

            //            var sourceText1 = @"
            //using System;
            //using System.Collections.Generic;
            //using System.Linq;
            //using System.Text;
            //using Microsoft.CodeAnalysis;
            //using Microsoft.CodeAnalysis.CSharp;

            //namespace TopLevel {

            //    using Microsoft;
            //    using System.ComponentModel;

            //    namespace Child1
            //    {
            //        using Microsoft.Win32;
            //        using System.Runtime.InteropServices;

            //        class Foo { }
            //    }

            //    namespace Child2
            //    {
            //        using System.CodeDom;
            //        using Microsoft.CSharp;

            //        class Bar { }
            //    }
            //}";


            //            var sourceText = @"
            //class User
            //{
            //    [DisplayName(""Name""), Required, MaxLength(10)]
            //    public string Field { get; set; }
            //}";

            var sourceText = @"
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Anto;
            class User
            {
                public string Field { get; set; }
            }";

            //var alterJson = "[{'method':'cunit.using','ast':{'f:List<UsingDirectiveSyntax>':[{'n:UsingDirectiveSyntax':[{'f:UsingDirective':[{'f:IdentifierName':['System']}]},{'f:UsingDirective':[{'f:QualifiedName':[{'f:IdentifierName':['System']},{'f:IdentifierName':['Data']}]}]}]}]},'bindings':{}}]".Replace("'", "\"");
            //var alterJson = "[{'method':'cunit.using','ast':{'f:List<UsingDirectiveSyntax>':[{'n:UsingDirectiveSyntax':[{'f:UsingDirective':[{'f:IdentifierName':['System']}]},{'f:UsingDirective':[{'f:QualifiedName':[{'f:IdentifierName':['System']},{'f:IdentifierName':['ComponentModel']}]}]},{'f:UsingDirective':[{'f:QualifiedName':[{'f:IdentifierName':['System']},{'f:IdentifierName':['ComponentModel']},{'f:IdentifierName':['DataAnnotations']}]}]}]}]},'bindings':{}}]".Replace("'", "\"");
            //var alterNode = NodeAlter.FromJson(alterJson).First();
            //var syntax = alterNode.Node.ToSyntax<UsingDirectiveSyntax>();





            var sourceNode = (CSharpSyntaxTree.ParseText(sourceText).GetRoot() as CSharpSyntaxNode);
#if _Full
            var builder = new SyntaxBuilder
            {
                OpenParenthesisOnNewLine = false,
                ClosingParenthesisOnNewLine = false,
                UseDefaultFormatting = true,
            };
            //var tree = quoter.Parse(sourceNode);
            //var dyn = quoter.Print(tree);
            //Console.WriteLine(dyn);
            var generatedCode = builder.Quote(sourceNode);
            var resultText = builder.Evaluate(generatedCode);
            Console.WriteLine(generatedCode);
            Console.WriteLine(resultText);
#else
            var builder = new SyntaxBuilder { };
            var tree = builder.Parse(sourceNode);
            //var code = SyntaxBuilder.FromApi(tree).ToFullString();
            //Console.WriteLine(code);
            //Console.WriteLine();

            var treeJson = tree.ToJson();
            var tree2 = Node.FromJson(treeJson);
            Console.WriteLine(treeJson); // tree2.ToString());
            Console.WriteLine();

            Console.WriteLine(SyntaxBuilder.Print(tree));
            Console.WriteLine();

            //var treeSyntax = Node.FromJsonToSyntax(tree.ToJson());
            //Console.WriteLine(treeSyntax.ToFullString());
            //Console.WriteLine();

            //var generatedCode = builder.Quote(sourceNode);
            //var resultText = builder.Evaluate(generatedCode);
            //Console.WriteLine(generatedCode);
            //Console.WriteLine(resultText);
#endif
        }
    }
}
#endif