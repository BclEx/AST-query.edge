using System;
using Microsoft.CodeAnalysis.CSharp;

namespace QuoterHost
{
    class Program
    {
        static void Main(string[] args)
        {
            var sourceText = "class C{ public static void Print() {} }";
//            var sourceText = @"
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;

//namespace TopLevel

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
            var sourceNode = (CSharpSyntaxTree.ParseText(sourceText).GetRoot() as CSharpSyntaxNode);
            var quoter = new Quoter
            {
                OpenParenthesisOnNewLine = false,
                ClosingParenthesisOnNewLine = false,
                UseDefaultFormatting = true
            };
            var generatedCode = quoter.Quote(sourceNode);
            var resultText = quoter.Evaluate(generatedCode);
            Console.WriteLine(generatedCode);
            Console.WriteLine(resultText);
        }
    }
}
