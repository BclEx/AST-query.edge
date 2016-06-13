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

            var sourceText1 = @"
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

            var sourceText = @"
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Net;
            using System.Net.Http;
            using System.Web.Http;

namespace CORE.Site
    {
        public class AreaController : ApiController
        {
            // GET api/<controller>
            public IEnumerable<object> Get(string sort = null)
            {
                var list = new List<object>();
                list.Add(new { id = 1, address = ""address1"", city = ""city1"", bedrooms = ""1"", bathrooms = ""1"", price = 100M });
                list.Add(new { id = 2, address = ""address2"", city = ""city2"", bedrooms = ""2"", bathrooms = ""2"", price = 100M });
                list.Add(new { id = 3, address = ""address3"", city = ""city3"", bedrooms = ""3"", bathrooms = ""3"", price = 100M });
                list.Add(new { id = 4, address = ""address4"", city = ""city4"", bedrooms = ""4"", bathrooms = ""4"", price = 100M });
                return list;
            }

            // GET api/<controller>/5
            public object Get(int id)
            {
                return new { id = 1, address = ""address"", city = ""city"", bedrooms = ""bedrooms"", bathrooms = ""bathrooms"" };
            }

            // POST api/<controller>
            public void Post([FromBody]string value)
            {
            }

            // PUT api/<controller>/5
            public void Put(int id, [FromBody]string value)
            {
            }

            // DELETE api/<controller>/5
            public void Delete(int id)
            {
            }
        }
    };
}";

            var sourceNode = (CSharpSyntaxTree.ParseText(sourceText).GetRoot() as CSharpSyntaxNode);
#if _Full
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
            //var code = Quoter.FromApi(tree).ToFullString();
            //Console.WriteLine(code);
            //Console.WriteLine();

            //Console.WriteLine(Quoter.Print(tree));
            //Console.WriteLine();

            //var tree2 = ApiCall.FromJson(tree.ToJson());
            //Console.WriteLine(Quoter.Print(tree2));
            //Console.WriteLine();

            var treeSyntax = ApiCall.FromJsonToSyntax(tree.ToJson());
            Console.WriteLine(treeSyntax);
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