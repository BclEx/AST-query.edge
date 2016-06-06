using System.Collections.Generic;
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
internal class ApiCall
{
    public string Name { get; private set; }
    public MemberCall FactoryMethodCall { get; private set; }
    public List<MethodCall> InstanceMethodCalls { get; private set; }
    public bool UseCurliesInsteadOfParentheses { get; private set; }

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

    public ApiCall(string parentPropertyName, string factoryMethodName, List<object> arguments, bool useCurliesInsteadOfParentheses = false)
    {
        UseCurliesInsteadOfParentheses = useCurliesInsteadOfParentheses;
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
        {
            InstanceMethodCalls = new List<MethodCall>();
        }

        InstanceMethodCalls.Add(methodCall);
    }

    public void Remove(MethodCall methodCall)
    {
        if (InstanceMethodCalls == null)
        {
            return;
        }

        InstanceMethodCalls.Remove(methodCall);
    }

    public override string ToString()
    {
        return Quoter.PrintWithDefaultFormatting(this);
    }
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
        {
            Arguments = new List<object>();
        }

        Arguments.Add(value);
    }
}
