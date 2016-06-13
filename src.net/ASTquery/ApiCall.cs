using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// "Stringly typed" representation of a C# property or method invocation expression, with a
/// string for the property or method name and a list of similarly loosely typed argument
/// expressions. Simply speaking, this is a tree of strings.
/// </summary>
/// <example>
/// Data structure to represent code (API calls) of simple hierarchical shape such as:
/// A.B(C, D.E(F(G, H), I))
/// </example>
public class ApiCall
{
    public string Name { get; private set; }
    public MemberCall FactoryMethodCall { get; internal set; }
    public List<MethodCall> InstanceMethodCalls { get; internal set; }
    public bool UseCurliesInsteadOfParentheses { get; internal set; }

    public ApiCall()
    {
    }

    public ApiCall(string parentPropertyName, string factoryMethodName)
    {
        Name = parentPropertyName;
        FactoryMethodCall = new MemberCall
        {
            Name = factoryMethodName
        };
    }

    public ApiCall(string parentPropertyName, string factoryMethodName, List<object> arguments)
    {
        Name = parentPropertyName;
        FactoryMethodCall = new MethodCall
        {
            Name = factoryMethodName,
            Arguments = arguments
        };
    }

    public ApiCall(string name, MethodCall factoryMethodCall)
    {
        Name = name;
        FactoryMethodCall = factoryMethodCall;
    }

    public void Add(MethodCall methodCall)
    {
        if (InstanceMethodCalls == null)
            InstanceMethodCalls = new List<MethodCall>();
        InstanceMethodCalls.Add(methodCall);
    }

    public void Remove(MethodCall methodCall)
    {
        if (InstanceMethodCalls == null)
            return;
        InstanceMethodCalls.Remove(methodCall);
    }

    #region Convert

    public override string ToString()
    {
        return Quoter.Print(this);
    }

    public string ToFullString()
    {
        var tree = (SyntaxNode)Quoter.FromApiRecurse(this);
        return tree.ToFullString();
    }

    public SyntaxNode ToSyntax()
    {
        var tree = (SyntaxNode)Quoter.FromApiRecurse(this);
        tree = tree.NormalizeWhitespace();
        return tree;
    }

    public string ToJson(bool jsonIndented = false)
    {
        var sb = new StringBuilder();
        var sw = new StringWriter(sb);
        using (var w = new JsonTextWriter(sw))
        {
            if (jsonIndented)
                w.Formatting = Formatting.Indented;
            Quoter.ToJsonRecurse(this, w);
        }
        return sb.ToString();
    }

    public static ApiCall FromJson(string json)
    {
        var r = new JsonTextReader(new StringReader(json));
        r.Read(); var codeBlock = (ApiCall)Quoter.FromJsonRecurse(r);
        return codeBlock;
    }

    public static SyntaxNode FromJsonToSyntax(string json)
    {
        var r = new JsonTextReader(new StringReader(json));
        r.Read(); var codeBlock = (SyntaxNode)Quoter.FromJsonToSyntaxRecurse(r);
        codeBlock = codeBlock.NormalizeWhitespace();
        return codeBlock;
    }

    #endregion
}

/// <summary>
/// Simple data structure to represent a member call, primarily just the string Name.
/// </summary>
public class MemberCall
{
    public string Name { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        Quoter.Print(this, sb, 0);
        return sb.ToString();
    }
}

/// <summary>
/// Represents a method call that has a Name and an arbitrary list of Arguments.
/// </summary>
public class MethodCall : MemberCall
{
    public List<object> Arguments { get; set; }

    public void AddArgument(object value)
    {
        if (Arguments == null)
            Arguments = new List<object>();
        Arguments.Add(value);
    }
}
