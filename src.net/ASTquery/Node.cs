using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using System;
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
public class Node
{
    public string Name { get; private set; }
    public MemberCall FactoryMethodCall { get; internal set; }
    public List<MethodCall> InstanceMethodCalls { get; internal set; }
    public bool UseCurliesInsteadOfParentheses { get; internal set; }

    public Node()
    {
    }

    public Node(string parentPropertyName, string factoryMethodName)
    {
        Name = parentPropertyName;
        FactoryMethodCall = new MemberCall
        {
            Name = factoryMethodName
        };
    }

    public Node(string parentPropertyName, string factoryMethodName, List<object> arguments)
    {
        Name = parentPropertyName;
        FactoryMethodCall = new MethodCall
        {
            Name = factoryMethodName,
            Arguments = arguments
        };
    }

    public Node(string name, MethodCall factoryMethodCall)
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
#if !_Full

    public override string ToString()
    {
        return SyntaxBuilder.Print(this);
    }

    public string ToFullString()
    {
        var tree = (SyntaxNode)SyntaxBuilder.FromApiRecurse(this);
        tree = tree.NormalizeWhitespace();
        return InternalReplace(tree.ToFullString());
    }

    public static string ToFullString(SyntaxNode node)
    {
        return InternalReplace(node.ToFullString());
    }

    private static string InternalReplace(string text)
    {
        int offset, nextIdx = 0;
        while (nextIdx < text.Length)
        {
            var getIdx = text.IndexOf("get;", nextIdx); var setIdx = text.IndexOf("set;", nextIdx);
            var minIdx = (getIdx != -1 && setIdx != -1 ? Math.Min(getIdx, setIdx) : Math.Max(getIdx, setIdx)); var maxIdx = Math.Max(getIdx, setIdx);
            if (minIdx == -1) break; //: EXIT
            var startIdx = text.LastIndexOf("{", minIdx); var endIdx = text.IndexOf("}", minIdx);
            nextIdx += 4;
            if (startIdx == -1 || endIdx == -1) continue; // SKIP: should be surrounded
            if (maxIdx > endIdx)
                maxIdx = minIdx; // OUTSIDE: next token outside of scope

            // remove start to first token
            //string.Format("a: {0}-{1}:{2} | g:{3} s:{4}", startIdx, endIdx, nextIdx, minIdx, maxIdx).Dump();
            offset = minIdx - startIdx - 1;
            // Console.WriteLine("a: [" + text.Substring(startIdx + 1, offset) + "]");
            if (string.IsNullOrWhiteSpace(text.Substring(startIdx + 1, offset)))
            {
                text = text.Remove(startIdx + 1, offset--).Insert(startIdx + 1, " "); // remove whitespace
                minIdx -= offset; maxIdx -= offset; endIdx -= offset; // adjust indexs
                nextIdx = (maxIdx > 0 ? maxIdx : endIdx);
            }

            // remove last token to next token
            //string.Format("b: {0}-{1}:{2} | g:{3} s:{4}", startIdx, endIdx, nextIdx, minIdx, maxIdx).Dump();
            if (maxIdx > minIdx)
            {
                offset = maxIdx - minIdx - 4;
                // Console.WriteLine("b: [" + text.Substring(minIdx + 4, offset) + "]");
                if (string.IsNullOrWhiteSpace(text.Substring(minIdx + 4, offset)))
                {
                    text = text.Remove(minIdx + 4, offset--).Insert(minIdx + 4, " "); // remove whitespace
                    maxIdx -= offset; endIdx -= offset; // adjust indexs
                }
                nextIdx = maxIdx;
            }

            // remove last token to end
            //string.Format("c: {0}-{1}:{2} | g:{3} s:{4}", startIdx, endIdx, nextIdx, minIdx, maxIdx).Dump();
            offset = endIdx - nextIdx - 4;
            // Console.WriteLine("c: [" + text.Substring(nextIdx + 4, offset) + "]");
            if (string.IsNullOrWhiteSpace(text.Substring(nextIdx + 4, offset)))
            {
                text = text.Remove(nextIdx + 4, offset--).Insert(nextIdx + 4, " "); // remove whitespace
                endIdx -= offset; // adjust indexs
            }

            // remove start to last statement with scan
            var beforeStartIdx = startIdx - 1;
            for (; beforeStartIdx > 0 && char.IsWhiteSpace(text[beforeStartIdx]); beforeStartIdx--) { }
            if (beforeStartIdx > 0)
            {
                offset = startIdx - beforeStartIdx - 1;
                //Console.WriteLine("d: [" + text.Substring(beforeStartIdx + 1, offset) + "]");
                if (string.IsNullOrWhiteSpace(text.Substring(beforeStartIdx + 1, offset)))
                    text = text.Remove(beforeStartIdx + 1, offset--).Insert(beforeStartIdx + 1, " "); // remove whitespace
            }

            // nextToken
            nextIdx = endIdx;
        }
        return text;
    }

    public SyntaxNode ToSyntax()
    {
        var tree = (SyntaxNode)SyntaxBuilder.FromApiRecurse(this);
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
            SyntaxBuilder.ToJsonRecurse(this, w);
        }
        return sb.ToString();
    }

    public static Node FromJson(string json)
    {
        var r = new JsonTextReader(new StringReader(json));
        r.Read(); var codeBlock = (Node)SyntaxBuilder.FromJsonRecurse(r);
        return codeBlock;
    }

    public static SyntaxNode FromJsonToSyntax(string json)
    {
        var r = new JsonTextReader(new StringReader(json));
        r.Read(); var codeBlock = (SyntaxNode)SyntaxBuilder.FromJsonToSyntaxRecurse(r);
        codeBlock = codeBlock.NormalizeWhitespace();
        return codeBlock;
    }

#endif
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
        SyntaxBuilder.Print(this, sb, 0);
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
