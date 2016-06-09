#if !_LIB
using System;
using Microsoft.CodeAnalysis.CSharp;

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

            var sourceText = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TopLevel {

    using Microsoft;
    using System.ComponentModel;

    namespace Child1
    {
        using Microsoft.Win32;
        using System.Runtime.InteropServices;

        class Foo { }
    }

    namespace Child2
    {
        using System.CodeDom;
        using Microsoft.CSharp;

        class Bar { }
    }
}";

            var sourceNode = (CSharpSyntaxTree.ParseText(sourceText).GetRoot() as CSharpSyntaxNode);
#if Full
            var quoter = new Quoter
            {
                OpenParenthesisOnNewLine = false,
                ClosingParenthesisOnNewLine = false,
                UseDefaultFormatting = true,
            };
            //var tree = quoter.Parse(sourceNode);
            //var dyn = quoter.Print(tree);
            //Console.WriteLine(dyn);
            var generatedCode = quoter.Quote(sourceNode);
            var resultText = quoter.Evaluate(generatedCode);
            Console.WriteLine(generatedCode);
            Console.WriteLine(resultText);
#else
            var quoter = new Quoter { };
            var tree = quoter.Parse(sourceNode);
            //var code = Quoter.FromApi(tree);

            var dyn1 = Quoter.ToJson(tree);
            Console.WriteLine(dyn1);
            Console.WriteLine();
            var dyn2 = Quoter.FromJson(dyn1);
            Console.WriteLine(dyn2);
            Console.WriteLine();

//var generatedCode = quoter.Quote(sourceNode);
            //var resultText = quoter.Evaluate(generatedCode);
            //Console.WriteLine(generatedCode);
            //Console.WriteLine(resultText);
#endif
        }
    }
}
#endif