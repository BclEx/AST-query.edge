#r "System.Runtime.dll"
#r "System.Text.Encoding.dll"
#r "System.Threading.Tasks.dll"
#r "lib/roslyn/Microsoft.CodeAnalysis.dll"
#r "lib/roslyn/Microsoft.CodeAnalysis.CSharp.dll"

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public class Startup
{
    public async Task<object> Invoke(string input)
    {
        return CSharpSyntaxTree.ParseText(input);
    }
}