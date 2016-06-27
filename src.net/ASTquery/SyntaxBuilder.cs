#if !_Full
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

public class SyntaxBuilder
{
    public bool UseDefaultFormatting { get; set; }
    public bool RemoveRedundantModifyingCalls { get; set; }

    #region Statics

    /// <summary>
    /// Enumerates names of properties on SyntaxNode, SyntaxToken and SyntaxTrivia classes that do
    /// not impact the shape of the syntax tree and are not essential to reconstructing the tree.
    /// </summary>
    static readonly string[] _nonStructuralProperties =
    {
        "AllowsAnyExpression",
        "Arity",
        "ContainsAnnotations",
        "ContainsDiagnostics",
        "ContainsDirectives",
        "ContainsSkippedText",
        "DirectiveNameToken",
        "FullSpan",
        "HasLeadingTrivia",
        "HasTrailingTrivia",
        "HasStructuredTrivia",
        "HasStructure",
        "IsConst",
        "IsDirective",
        "IsElastic",
        "IsFixed",
        "IsMissing",
        "IsStructuredTrivia",
        "IsUnboundGenericName",
        "IsVar",
        "Kind",
        "Language",
        "Parent",
        "ParentTrivia",
        "PlainName",
        "Span",
        "SyntaxTree",
    };

    /// <summary>
    /// Static methods on Microsoft.CodeAnalysis.CSharp.SyntaxFactory class that construct SyntaxNodes
    /// </summary>
    /// <example>Syntax.ClassDeclaration()</example>
    static readonly Dictionary<string, List<MethodInfo>> _factoryMethods = GetFactoryMethods();

    /// <summary>
    /// Five public properties on Microsoft.CodeAnalysis.CSharp.SyntaxFactory that return trivia: CarriageReturn,
    /// LineFeed, CarriageReturnLineFeed, Space and Tab.
    /// </summary>
    static readonly Dictionary<string, PropertyInfo> _triviaFactoryProperties = GetTriviaFactoryProperties();

    /// <summary>
    /// Gets the five properties on SyntaxFactory that return ready-made trivia: CarriageReturn,
    /// CarriageReturnLineFeed, LineFeed, Space and Tab.
    /// </summary>
    private static Dictionary<string, PropertyInfo> GetTriviaFactoryProperties()
    {
        return typeof(SyntaxFactory).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(propertyInfo => propertyInfo.PropertyType == typeof(SyntaxTrivia))
            .Where(propertyInfo => !propertyInfo.Name.Contains("Elastic"))
            .ToDictionary(propertyInfo => ((SyntaxTrivia)propertyInfo.GetValue(null)).ToString());
    }

    /// <summary>
    /// Returns static methods on Microsoft.CodeAnalysis.CSharp.SyntaxFactory that return types derived from
    /// SyntaxNode and bucketizes them by overloads.
    /// </summary>
    private static Dictionary<string, List<MethodInfo>> GetFactoryMethods()
    {
        var result = new Dictionary<string, List<MethodInfo>>();
        var staticMethods = typeof(SyntaxFactory).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.GetCustomAttribute<ObsoleteAttribute>() == null)
            .OrderBy(m => m.ToString());
        foreach (var method in staticMethods)
        {
            var returnTypeName = method.ReturnType.Name;
            List<MethodInfo> bucket;
            if (!result.TryGetValue(returnTypeName, out bucket))
            {
                bucket = new List<MethodInfo>();
                result.Add(returnTypeName, bucket);
            }
            bucket.Add(method);
        }
        return result;
    }

    #endregion

    public SyntaxBuilder()
    {
        UseDefaultFormatting = true;
        RemoveRedundantModifyingCalls = true;
    }

    public string Quote(string sourceText, bool jsonIndented = false)
    {
        var sourceTree = CSharpSyntaxTree.ParseText(sourceText);
        return Parse(sourceTree.GetRoot()).ToJson(jsonIndented);
    }

    public string Quote(SyntaxNode node, bool jsonIndented = false)
    {
        var rootApiCall = QuoteRecurse(node, name: null);
        return rootApiCall.ToJson(jsonIndented);
    }

    public Node Parse(string sourceText)
    {
        var sourceTree = CSharpSyntaxTree.ParseText(sourceText);
        return Parse(sourceTree.GetRoot());
    }

    public Node Parse(SyntaxNode node)
    {
        var rootApiCall = QuoteRecurse(node, name: null);
        return rootApiCall;
    }

    #region Utility

    private static string GetSyntaxFactory(string text)
    {
        return "f:" + text;
    }

    private static void AddIfNotNull(List<object> arguments, object value)
    {
        if (value != null)
            arguments.Add(value);
    }

    private static List<object> CreateArgumentList(params object[] args)
    {
        return new List<object>(args);
    }

    /// <summary>
    /// Finds a value in a list using case-insensitive search
    /// </summary>
    private Node FindValue(string parameterName, IEnumerable<Node> values)
    {
        return values.FirstOrDefault(v => parameterName.Equals(v.Name, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Recurse

    private Node QuoteRecurse(object treeElement, string name = null)
    {
        if (treeElement is SyntaxTrivia)
            return QuoteTrivia((SyntaxTrivia)treeElement);
        if (treeElement is SyntaxToken)
            return QuoteToken((SyntaxToken)treeElement, name);
        if (treeElement is SyntaxNodeOrToken)
        {
            var syntaxNodeOrToken = (SyntaxNodeOrToken)treeElement;
            return (syntaxNodeOrToken.IsNode ? QuoteNodeRecurse(syntaxNodeOrToken.AsNode(), name) : QuoteToken(syntaxNodeOrToken.AsToken(), name));
        }
        return QuoteNodeRecurse((SyntaxNode)treeElement, name);
    }

    /// <summary>
    /// The main recursive method that given a SyntaxNode recursively quotes the entire subtree.
    /// </summary>
    private Node QuoteNodeRecurse(SyntaxNode node, string name)
    {
        var quotedPropertyValues = QuotePropertyValues(node);
        var factoryMethod = PickFactoryMethodToCreateNode(node);
        var factoryMethodName = (factoryMethod.DeclaringType.Name == "SyntaxFactory" ? GetSyntaxFactory(factoryMethod.Name) : factoryMethod.DeclaringType.Name + "." + factoryMethod.Name);
        var factoryMethodCall = new MethodCall { Name = factoryMethodName };
        var codeBlock = new Node(name, factoryMethodCall);
        AddFactoryMethodArguments(factoryMethod, factoryMethodCall, quotedPropertyValues);
        AddModifyingCalls(node, codeBlock, quotedPropertyValues);
        return codeBlock;
    }

    /// <summary>
    /// Uses Reflection to inspect static factory methods on the Microsoft.CodeAnalysis.CSharp.SyntaxFactory
    /// class and pick an overload that creates a node of the same type as the input <paramref name="node"/>
    /// </summary>
    private MethodInfo PickFactoryMethodToCreateNode(SyntaxNode node)
    {
        var name = node.GetType().Name;
        List<MethodInfo> candidates;
        if (!_factoryMethods.TryGetValue(name, out candidates))
            throw new NotSupportedException(name + " is not supported");
        var minParameterCount = candidates.Min(m => m.GetParameters().Length);
        // HACK: for LiteralExpression pick the overload with two parameters - the overload with one parameter only allows true/false/null literals
        if (node is LiteralExpressionSyntax)
        {
            var kind = ((LiteralExpressionSyntax)node).Kind();
            if (kind != SyntaxKind.TrueLiteralExpression &&
                kind != SyntaxKind.FalseLiteralExpression &&
                kind != SyntaxKind.NullLiteralExpression)
                minParameterCount = 2;
        }
        MethodInfo factory = null;
        if ((node is BaseTypeDeclarationSyntax ||
             node is IdentifierNameSyntax))
        {
            var desiredParameterType = typeof(string);
            factory = candidates.FirstOrDefault(m => m.GetParameters()[0].ParameterType == desiredParameterType);
            if (factory != null)
                return factory;
        }
        var candidatesWithMinParameterCount = candidates.Where(m => m.GetParameters().Length == minParameterCount).ToArray();
        if (minParameterCount == 1 && candidatesWithMinParameterCount.Length > 1)
        {
            // first see if we have a method that accepts params parameter and return that if found
            var paramArray = candidatesWithMinParameterCount.FirstOrDefault(m => m.GetParameters()[0].GetCustomAttribute<ParamArrayAttribute>() != null);
            if (paramArray != null)
                return paramArray;
            // if there are multiple candidates with one parameter, pick the one that is optional
            var firstParameterOptional = candidatesWithMinParameterCount.FirstOrDefault(m => m.GetParameters()[0].IsOptional);
            if (firstParameterOptional != null)
                return firstParameterOptional;
        }
        // otherwise just pick the first one (this is arbitrary)
        return candidatesWithMinParameterCount[0];
    }

    /// <summary>
    /// Adds information about subsequent modifying fluent interface style calls on an object (like foo.With(...).With(...))
    /// </summary>
    private void AddModifyingCalls(object treeElement, Node apiCall, List<Node> values)
    {
        var methods = treeElement.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<ObsoleteAttribute>() == null && m.Name.StartsWith("With"))
            .ToList();
        foreach (var value in values)
        {
            var properCase = ProperCase(value.Name);
            var methodName = "With" + properCase;
            if (methods.Any(m => m.Name == methodName))
                methodName = "w:" + methodName.Substring(4);
            else
                throw new NotSupportedException("Sorry, this is a bug in the AST-query Quoter. Please file a bug at https://github.com/BclEx/AST-query.edge/issues/new.");
            var methodCall = new MethodCall
            {
                Name = methodName,
                Arguments = CreateArgumentList(value)
            };
            AddModifyingCall(apiCall, methodCall);
        }
    }

    private void AddModifyingCall(Node apiCall, MethodCall methodCall)
    {
        if (RemoveRedundantModifyingCalls)
        {
            var before = apiCall.ToFullString();
            apiCall.Add(methodCall);
            var after = apiCall.ToFullString();
            if (before == after)
                apiCall.Remove(methodCall);
            return;
        }
        apiCall.Add(methodCall);
    }

    internal static string ProperCase(string str)
    {
        return char.ToUpperInvariant(str[0]) + str.Substring(1);
    }

    /// <summary>
    /// Inspects the property values of the <paramref name="node"/> object using Reflection and
    /// creates API call descriptions for the property values recursively. Properties that are not
    /// essential to the shape of the syntax tree (such as Span) are ignored.
    /// </summary>
    private List<Node> QuotePropertyValues(SyntaxNode node)
    {
        var result = new List<Node>();
        // get properties and filter out non-essential properties listed in nonStructuralProperties
        var properties = node.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        result.AddRange(properties
            .Where(propertyInfo => !_nonStructuralProperties.Contains(propertyInfo.Name))
            .Where(p => p.GetCustomAttribute<ObsoleteAttribute>() == null)
            .Select(propertyInfo => QuotePropertyValue(node, propertyInfo))
            .Where(apiCall => apiCall != null));
        // HACK: factory methods for the following node types accept back the first "kind" parameter
        // that we filter out above. Add an artificial "property value" that can be later used to
        // satisfy the first parameter of type SyntaxKind.
        if (node is AccessorDeclarationSyntax ||
            node is AssignmentExpressionSyntax ||
            node is BinaryExpressionSyntax ||
            node is ClassOrStructConstraintSyntax ||
            node is CheckedExpressionSyntax ||
            node is CheckedStatementSyntax ||
            node is ConstructorInitializerSyntax ||
            node is GotoStatementSyntax ||
            node is InitializerExpressionSyntax ||
            node is LiteralExpressionSyntax ||
            node is MemberAccessExpressionSyntax ||
            node is OrderingSyntax ||
            node is PostfixUnaryExpressionSyntax ||
            node is PrefixUnaryExpressionSyntax ||
            node is DocumentationCommentTriviaSyntax ||
            node is YieldStatementSyntax)
            result.Add(new Node("Kind", "k:" + node.Kind().ToString()));
        return result;
    }

    /// <summary>
    /// Parse the value of the property <paramref name="property"/> of object <paramref
    /// name="node"/>
    /// </summary>
    private Node QuotePropertyValue(SyntaxNode node, PropertyInfo property)
    {
        var value = property.GetValue(node, null);
        var propertyType = property.PropertyType;
        if (propertyType == typeof(SyntaxToken))
            return QuoteToken((SyntaxToken)value, property.Name);
        if (propertyType == typeof(SyntaxTokenList))
            return QuoteList((IEnumerable)value, property.Name);
        if (propertyType.IsGenericType &&
            (propertyType.GetGenericTypeDefinition() == typeof(SyntaxList<>) ||
             propertyType.GetGenericTypeDefinition() == typeof(SeparatedSyntaxList<>)))
            return QuoteList((IEnumerable)value, property.Name);
        if (value is SyntaxNode)
            return QuoteNodeRecurse((SyntaxNode)value, property.Name);
        if (value is string)
            return new Node(property.Name, Escape(value.ToString()));
        if (value is bool)
            return new Node(property.Name, value.ToString().ToLowerInvariant());
        return null;
    }

    private void AddFactoryMethodArguments(MethodInfo factory, MethodCall factoryMethodCall, List<Node> quotedValues)
    {
        foreach (var factoryMethodParameter in factory.GetParameters())
        {
            var parameterName = factoryMethodParameter.Name;
            var parameterType = factoryMethodParameter.ParameterType;
            var quotedCodeBlock = FindValue(parameterName, quotedValues);
            // if we have Block(List<StatementSyntax>(new StatementSyntax[] { A, B })), just simplify it to Block(A, B)
            if (quotedCodeBlock != null && factory.GetParameters().Length == 1 && factoryMethodParameter.GetCustomAttribute<ParamArrayAttribute>() != null)
            {
                var methodCall = (quotedCodeBlock.FactoryMethodCall as MethodCall);
                if (methodCall != null && methodCall.Name.Contains("List") && methodCall.Arguments.Count == 1)
                {
                    var argument = (methodCall.Arguments[0] as Node);
                    var arrayCreation = (argument.FactoryMethodCall as MethodCall);
                    if (argument != null && arrayCreation != null && arrayCreation.Name.StartsWith("n:"))
                    {
                        foreach (var arrayElement in arrayCreation.Arguments)
                            factoryMethodCall.AddArgument(arrayElement);
                        quotedValues.Remove(quotedCodeBlock);
                        return;
                    }
                }
            }
            // special case to prefer SyntaxFactory.IdentifierName("C") to SyntaxFactory.IdentifierName(Syntax.Identifier("C"))
            else if (parameterName == "name" && parameterType == typeof(string))
            {
                quotedCodeBlock = quotedValues.First(a => a.Name == "Identifier");
                var methodCall = (quotedCodeBlock.FactoryMethodCall as MethodCall);
                if (methodCall != null && methodCall.Name == GetSyntaxFactory("Identifier"))
                {
                    factoryMethodCall.AddArgument(methodCall.Arguments.Count == 1 ? methodCall.Arguments[0] : quotedCodeBlock);
                    quotedValues.Remove(quotedCodeBlock);
                    continue;
                }
            }
            // special case to prefer SyntaxFactory.ClassDeclarationSyntax(string) instead of SyntaxFactory.ClassDeclarationSyntax(SyntaxToken)
            else if (parameterName == "identifier" && parameterType == typeof(string))
            {
                var methodCall = quotedCodeBlock.FactoryMethodCall as MethodCall;
                if (methodCall != null && methodCall.Name == GetSyntaxFactory("Identifier") && methodCall.Arguments.Count == 1)
                {
                    factoryMethodCall.AddArgument(methodCall.Arguments[0]);
                    quotedValues.Remove(quotedCodeBlock);
                    continue;
                }
            }
            if (quotedCodeBlock != null)
            {
                factoryMethodCall.AddArgument(quotedCodeBlock);
                quotedValues.Remove(quotedCodeBlock);
            }
            else if (!factoryMethodParameter.IsOptional)
            {
                if (parameterType.IsArray)
                    continue; // assuming this is a params parameter that accepts an array, so if we have nothing we don't need to pass anything
                throw new InvalidOperationException(string.Format("Couldn't find value for parameter '{0}' of method '{1}'. Go to QuotePropertyValues() and add your node type to the exception list.", parameterName, factory));
            }
        }
    }

    private Node QuoteList(IEnumerable syntaxList, string name)
    {
        var sourceList = syntaxList.Cast<object>();
        var methodName = GetSyntaxFactory("List");
        string listType = null;
        var propertyType = syntaxList.GetType();
        if (propertyType.IsGenericType)
        {
            var methodType = propertyType.GetGenericArguments()[0].Name;
            if (propertyType.GetGenericTypeDefinition() == typeof(SeparatedSyntaxList<>))
            {
                listType = "SyntaxNodeOrToken";
                methodName = GetSyntaxFactory("SeparatedList");
                sourceList = ((SyntaxNodeOrTokenList)syntaxList.GetType().GetMethod("GetWithSeparators").Invoke(syntaxList, null)).Cast<object>().ToArray();
            }
            else listType = methodType;
            methodName += "<" + methodType + ">";
        }
        if (propertyType.Name == "SyntaxTokenList")
            methodName = GetSyntaxFactory("TokenList");
        else if (propertyType.Name == "SyntaxTriviaList")
            methodName = GetSyntaxFactory("TriviaList");
        var elements = new List<object>(sourceList
            .Select(o => QuoteRecurse(o))
            .Where(cb => cb != null)).ToList();
        if (elements.Count == 0)
            return null;
        else if (elements.Count == 1)
        {
            if (methodName.StartsWith("List"))
                methodName = "SingletonList" + methodName.Substring("List".Length);
            else if (methodName.StartsWith(GetSyntaxFactory("List")))
                methodName = GetSyntaxFactory("SingletonList") + methodName.Substring(GetSyntaxFactory("List").Length);
            else if (methodName.StartsWith("SeparatedList"))
                methodName = "SingletonSeparatedList" + methodName.Substring("SeparatedList".Length);
            else if (methodName.StartsWith(GetSyntaxFactory("SeparatedList")))
                methodName = GetSyntaxFactory("SingletonSeparatedList") + methodName.Substring(GetSyntaxFactory("SeparatedList").Length);
        }
        else
            elements = new List<object>
            {
                new Node("methodName", "n:" + listType, elements) { UseCurliesInsteadOfParentheses = true }
            };
        return new Node(name, methodName, elements);
    }

    #endregion

    #region Token

    private Node QuoteToken(SyntaxToken value, string name)
    {
        SyntaxKind valueKind;
        if (value == default(SyntaxToken) || (valueKind = value.Kind()) == SyntaxKind.None)
            return null;
        var arguments = new List<object>();
        var methodName = GetSyntaxFactory("Token");
        var escapedTokenValueText = Escape(value.ToString());
        var leading = GetLeadingTrivia(value);
        object actualValue;
        var trailing = GetTrailingTrivia(value);
        if (leading != null || trailing != null)
        {
            leading = (leading ?? GetEmptyTrivia("LeadingTrivia"));
            trailing = (trailing ?? GetEmptyTrivia("TrailingTrivia"));
        }
        if (valueKind == SyntaxKind.IdentifierToken && !value.IsMissing)
        {
            methodName = (value.IsMissing ? GetSyntaxFactory("MissingToken") : GetSyntaxFactory("Identifier"));
            actualValue = (value.IsMissing ? (object)value.Kind() : escapedTokenValueText);
            AddIfNotNull(arguments, leading);
            arguments.Add(actualValue);
            AddIfNotNull(arguments, trailing);
        }
        else if (valueKind == SyntaxKind.InterpolatedStringTextToken && !value.IsMissing)
        {
            leading = (leading ?? GetEmptyTrivia("LeadingTrivia"));
            trailing = (trailing ?? GetEmptyTrivia("TrailingTrivia"));
            AddIfNotNull(arguments, leading);
            arguments.Add(valueKind);
            arguments.Add(escapedTokenValueText);
            arguments.Add(escapedTokenValueText);
            AddIfNotNull(arguments, trailing);
        }
        else if ((valueKind == SyntaxKind.XmlTextLiteralToken ||
            valueKind == SyntaxKind.XmlTextLiteralNewLineToken ||
            valueKind == SyntaxKind.XmlEntityLiteralToken) && !value.IsMissing)
        {
            if (valueKind == SyntaxKind.XmlTextLiteralNewLineToken) methodName = GetSyntaxFactory("XmlTextNewLine");
            else if (valueKind == SyntaxKind.XmlEntityLiteralToken) methodName = GetSyntaxFactory("XmlEntity");
            else methodName = GetSyntaxFactory("XmlTextLiteral");
            arguments.Add(leading ?? GetEmptyTrivia("LeadingTrivia"));
            arguments.Add(escapedTokenValueText);
            arguments.Add(escapedTokenValueText);
            arguments.Add(trailing ?? GetEmptyTrivia("TrailingTrivia"));
        }
        else if ((value.Parent is LiteralExpressionSyntax ||
            valueKind == SyntaxKind.StringLiteralToken ||
            valueKind == SyntaxKind.NumericLiteralToken) &&
            valueKind != SyntaxKind.TrueKeyword &&
            valueKind != SyntaxKind.FalseKeyword &&
            valueKind != SyntaxKind.NullKeyword &&
            valueKind != SyntaxKind.ArgListKeyword &&
            !value.IsMissing)
        {
            methodName = GetSyntaxFactory("Literal");
            var shouldAddTrivia = (leading != null || trailing != null);
            if (shouldAddTrivia)
                arguments.Add(leading ?? GetEmptyTrivia("LeadingTrivia"));
            var escapedText = Escape(value.Text);
            string escapedValue;
            if (valueKind == SyntaxKind.CharacterLiteralToken) escapedValue = Escape(value.ValueText, "c");
            else if (valueKind != SyntaxKind.StringLiteralToken) escapedValue = Escape(value.ValueText, "n");
            else escapedValue = Escape(value.ValueText);
            if (shouldAddTrivia || (valueKind == SyntaxKind.StringLiteralToken && value.ToString() != SyntaxFactory.Literal(value.ValueText).ToString()))
                arguments.Add(escapedText);
            arguments.Add(escapedValue);
            if (shouldAddTrivia)
                arguments.Add(trailing ?? GetEmptyTrivia("TrailingTrivia"));
        }
        else
        {
            if (value.IsMissing)
                methodName = GetSyntaxFactory("MissingToken");
            else if (valueKind == SyntaxKind.BadToken)
            {
                methodName = GetSyntaxFactory("BadToken");
                leading = leading ?? GetEmptyTrivia("LeadingTrivia");
                trailing = trailing ?? GetEmptyTrivia("TrailingTrivia");
            }
            var tokenValue = (valueKind == SyntaxKind.BadToken ? (object)escapedTokenValueText : valueKind);
            AddIfNotNull(arguments, leading);
            arguments.Add(tokenValue);
            AddIfNotNull(arguments, trailing);
        }
        return new Node(name, methodName, arguments);
    }

    #endregion

    #region Trivia

    private object GetLeadingTrivia(SyntaxToken value)
    {
        if (value.HasLeadingTrivia)
        {
            var quotedLeadingTrivia = QuoteList(value.LeadingTrivia, "LeadingTrivia");
            if (quotedLeadingTrivia != null)
                return quotedLeadingTrivia;
        }
        return null;
    }

    private object GetTrailingTrivia(SyntaxToken value)
    {
        if (value.HasTrailingTrivia)
        {
            var quotedTrailingTrivia = QuoteList(value.TrailingTrivia, "TrailingTrivia");
            if (quotedTrailingTrivia != null)
                return quotedTrailingTrivia;
        }
        return null;
    }

    private object GetEmptyTrivia(string parentPropertyName)
    {
        return new Node(parentPropertyName, GetSyntaxFactory("TriviaList"), arguments: null);
    }

    private Node QuoteTrivia(SyntaxTrivia syntaxTrivia)
    {
        var factoryMethodName = GetSyntaxFactory("Trivia");
        var text = syntaxTrivia.ToString();
        SyntaxKind syntaxKind;
        if (syntaxTrivia.FullSpan.Length == 0 || ((syntaxKind = syntaxTrivia.Kind()) == SyntaxKind.WhitespaceTrivia && UseDefaultFormatting))
            return null;
        PropertyInfo triviaFactoryProperty;
        if (_triviaFactoryProperties.TryGetValue(text, out triviaFactoryProperty) && syntaxKind == ((SyntaxTrivia)triviaFactoryProperty.GetValue(null)).Kind())
            return (UseDefaultFormatting ? null : new Node(null, GetSyntaxFactory(triviaFactoryProperty.Name)));
        if (!string.IsNullOrEmpty(text) && string.IsNullOrWhiteSpace(text) && syntaxKind == SyntaxKind.WhitespaceTrivia) factoryMethodName = (UseDefaultFormatting ? null : GetSyntaxFactory("Whitespace"));
        else if (syntaxKind == SyntaxKind.SingleLineCommentTrivia || syntaxKind == SyntaxKind.MultiLineCommentTrivia) factoryMethodName = GetSyntaxFactory("Comment");
        else if (syntaxKind == SyntaxKind.SingleLineDocumentationCommentTrivia || syntaxKind == SyntaxKind.MultiLineDocumentationCommentTrivia) factoryMethodName = GetSyntaxFactory("DocumentComment");
        else if (syntaxKind == SyntaxKind.PreprocessingMessageTrivia) factoryMethodName = GetSyntaxFactory("PreprocessingMessage");
        else if (syntaxKind == SyntaxKind.DisabledTextTrivia) factoryMethodName = GetSyntaxFactory("DisabledText");
        else if (syntaxKind == SyntaxKind.DocumentationCommentExteriorTrivia) factoryMethodName = GetSyntaxFactory("DocumentationCommentExterior");
        if (factoryMethodName == null)
            return null;
        var argument = (syntaxTrivia.HasStructure ? (object)QuoteNodeRecurse(syntaxTrivia.GetStructure(), "Structure") : Escape(text));
        return new Node(null, factoryMethodName, CreateArgumentList(argument));
    }

    #endregion

    #region Escape

    public static string Escape(string text, string type = null)
    {
        if (text == Environment.NewLine || text == "\n")
            return "\n";
        return (type == null ? text : type + "|" + text);
    }

    private static object Unescape(string value)
    {
        if (value.Length < 2 || value[1] != '|')
            return value;
        //Console.WriteLine(value);
        if (value[0] == 's') return value.Substring(2, value.Length - 2);
        else if (value[0] == 'n') return int.Parse(value.Substring(2, value.Length - 2));
        else if (value[0] == 'c') return value[2];
        return value;
    }

    #endregion

    #region Convert

    internal static void ToJsonRecurse(Node codeBlock, JsonTextWriter w)
    {
        w.WriteStartObject();
        ToJson(codeBlock.FactoryMethodCall, w);
        if (codeBlock.InstanceMethodCalls != null)
        {
            w.WritePropertyName("b");
            w.WriteStartArray();
            foreach (var call in codeBlock.InstanceMethodCalls)
            {
                w.WriteStartObject();
                ToJson(call, w);
                w.WriteEndObject();
            }
            w.WriteEndArray();
        }
        w.WriteEndObject();
    }

    internal static Node FromJsonRecurse(JsonTextReader r)
    {
        if (r.TokenType != JsonToken.StartObject) throw new InvalidOperationException();
        r.Read(); var codeBlock = new Node { FactoryMethodCall = FromJson(r) };
        if (r.TokenType == JsonToken.PropertyName && (string)r.Value == "b")
        {
            r.Read(); if (r.TokenType != JsonToken.StartArray) throw new InvalidOperationException();
            var calls = new List<MethodCall>();
            do
            {
                r.Read(); //if (r.TokenType != JsonToken.StartObject) throw new InvalidOperationException();
                if (r.TokenType == JsonToken.EndArray) break;
                else { r.Read(); calls.Add((MethodCall)FromJson(r)); }
                //if (r.TokenType != JsonToken.EndObject) throw new InvalidOperationException();
            }
            while (r.TokenType != JsonToken.EndArray);
            r.Read();
            codeBlock.InstanceMethodCalls = calls;
        }
        if (r.TokenType != JsonToken.EndObject) throw new InvalidOperationException();
        r.Read();
        return codeBlock;
    }

    internal static object FromApiRecurse(Node codeBlock)
    {
        var obj = FromApi(null, codeBlock.FactoryMethodCall);
        if (codeBlock.InstanceMethodCalls != null)
            foreach (var call in codeBlock.InstanceMethodCalls)
                obj = FromApi(obj, call);
        return obj;
    }

    internal static object FromJsonToSyntaxRecurse(JsonTextReader r)
    {
        if (r.TokenType != JsonToken.StartObject) throw new InvalidOperationException();
        r.Read(); var obj = FromJsonToSyntax(null, r);
        if (r.TokenType == JsonToken.PropertyName && (string)r.Value == "b")
        {
            r.Read(); if (r.TokenType != JsonToken.StartArray) throw new InvalidOperationException();
            do
            {
                r.Read(); //if (r.TokenType != JsonToken.StartObject) throw new InvalidOperationException();
                if (r.TokenType == JsonToken.EndArray) break;
                else { r.Read(); obj = FromJsonToSyntax(obj, r); }
                //if (r.TokenType != JsonToken.EndObject) throw new InvalidOperationException();
            }
            while (r.TokenType != JsonToken.EndArray);
            r.Read();
        }
        if (r.TokenType != JsonToken.EndObject) throw new InvalidOperationException();
        r.Read();
        return obj;
    }

    private static void ToJson(MemberCall call, JsonTextWriter w)
    {
        w.WritePropertyName(call.Name);
        var methodCall = (call as MethodCall);
        if (methodCall == null || methodCall.Arguments == null || !methodCall.Arguments.Any())
        {
            w.WriteValue((string)null);
            return;
        }
        //
        w.WriteStartArray();
        foreach (var block in methodCall.Arguments)
        {
            if (block is int) w.WriteValue((int)block);
            else if (block is string) w.WriteValue((string)block);
            else if (block is SyntaxKind) w.WriteValue("k:" + ((SyntaxKind)block).ToString());
            else if (block is Node) ToJsonRecurse(block as Node, w);
        }
        w.WriteEndArray();
    }

    private static MemberCall FromJson(JsonTextReader r)
    {
        var callName = (r.Value as string); if (r.TokenType != JsonToken.PropertyName) throw new InvalidOperationException();
        r.Read(); if (r.TokenType == JsonToken.Null)
        {
            r.Read();
            return new MemberCall { Name = callName };
        }
        //
        if (r.TokenType != JsonToken.StartArray) throw new InvalidOperationException();
        r.Read();
        var args = new List<object>();
        while (r.TokenType != JsonToken.EndArray)
        {
            if (r.TokenType != JsonToken.Integer && r.TokenType != JsonToken.String && r.TokenType != JsonToken.StartObject) throw new InvalidOperationException();
            else if (r.TokenType == JsonToken.StartObject) args.Add(FromJsonRecurse(r));
            else if (r.TokenType == JsonToken.Integer) { args.Add("n|" + r.Value.ToString()); r.Read(); }
            else if (r.TokenType == JsonToken.String && !((string)r.Value).StartsWith("k:")) { args.Add(r.Value); r.Read(); }
            else if (r.TokenType == JsonToken.String) { args.Add(Enum.Parse(typeof(SyntaxKind), ((string)r.Value).Substring(2))); r.Read(); }
            else throw new InvalidOperationException(r.TokenType.ToString());
        }
        r.Read();
        return new MethodCall { Name = callName, Arguments = args };
    }

    private static object FromApi(object obj, MemberCall call)
    {
        var callName = call.Name;
        var methodCall = (call as MethodCall);
        if (methodCall == null || methodCall.Arguments == null || !methodCall.Arguments.Any())
            return Evaluate(obj, callName, null);
        //
        var args = new List<object>();
        foreach (var block in methodCall.Arguments)
        {
            if (block is int) args.Add(Unescape("n|" + block.ToString()));
            else if (block is string) args.Add(Unescape((string)block));
            else if (block is SyntaxKind) args.Add(block);
            else if (block is Node) args.Add(FromApiRecurse(block as Node));
        }
        return Evaluate(obj, callName, args.ToArray());
    }

    private static object FromJsonToSyntax(object obj, JsonTextReader r)
    {
        var callName = (r.Value as string); if (r.TokenType != JsonToken.PropertyName) throw new InvalidOperationException();
        r.Read(); if (r.TokenType == JsonToken.Null)
        {
            r.Read();
            return Evaluate(obj, callName, null);
        }
        //
        if (r.TokenType != JsonToken.StartArray) throw new InvalidOperationException();
        r.Read();
        var args = new List<object>();
        while (r.TokenType != JsonToken.EndArray)
        {
            if (r.TokenType != JsonToken.Integer && r.TokenType != JsonToken.String && r.TokenType != JsonToken.StartObject) throw new InvalidOperationException();
            else if (r.TokenType == JsonToken.StartObject) args.Add(FromJsonToSyntaxRecurse(r));
            else if (r.TokenType == JsonToken.Integer) { args.Add(Unescape("n|" + r.Value.ToString())); r.Read(); }
            else if (r.TokenType == JsonToken.String && !((string)r.Value).StartsWith("k:")) { args.Add(Unescape((string)r.Value)); r.Read(); }
            else if (r.TokenType == JsonToken.String) { args.Add(Enum.Parse(typeof(SyntaxKind), ((string)r.Value).Substring(2))); r.Read(); }
            else throw new InvalidOperationException(r.TokenType.ToString());
        }
        r.Read();
        return Evaluate(obj, callName, args.ToArray());
    }

    #endregion

    #region Print

    /// <summary>
    /// Flattens a tree of ApiCalls into a single string.
    /// </summary>
    public static string Print(Node root)
    {
        var b = new StringBuilder();
        PrintRecurse(root, b, 0, openParenthesisOnNewLine: false, closingParenthesisOnNewLine: false);
        return b.ToString();
    }

    private static void PrintRecurse(Node codeBlock, StringBuilder b, int depth = 0, bool openParenthesisOnNewLine = false, bool closingParenthesisOnNewLine = false)
    {
        Print(codeBlock.FactoryMethodCall, b, depth, useCurliesInsteadOfParentheses: codeBlock.UseCurliesInsteadOfParentheses, openParenthesisOnNewLine: openParenthesisOnNewLine, closingParenthesisOnNewLine: closingParenthesisOnNewLine);
        if (codeBlock.InstanceMethodCalls != null)
            foreach (var call in codeBlock.InstanceMethodCalls)
            {
                PrintNewLine(b);
                Print(call, b, depth, useCurliesInsteadOfParentheses: codeBlock.UseCurliesInsteadOfParentheses, openParenthesisOnNewLine: openParenthesisOnNewLine, closingParenthesisOnNewLine: closingParenthesisOnNewLine);
            }
    }

    internal static void Print(MemberCall call, StringBuilder b, int depth, bool openParenthesisOnNewLine = false, bool closingParenthesisOnNewLine = false, bool useCurliesInsteadOfParentheses = false)
    {
        var openParen = (useCurliesInsteadOfParentheses ? "{" : "(");
        var closeParen = (useCurliesInsteadOfParentheses ? "}" : ")");
        Print(call.Name, b, depth);

        var methodCall = (call as MethodCall);
        if (methodCall != null)
        {
            if (methodCall.Arguments == null || !methodCall.Arguments.Any())
            {
                Print(openParen + closeParen, b, 0);
                return;
            }
            var needNewLine = (methodCall.Arguments.Count == 1 && (methodCall.Arguments[0] is string || methodCall.Arguments[0] is SyntaxKind) ? false : true);
            if (openParenthesisOnNewLine && needNewLine) { PrintNewLine(b); Print(openParen, b, depth); }
            else Print(openParen, b, 0);
            if (needNewLine) PrintNewLine(b);
            var needComma = false;
            foreach (var block in methodCall.Arguments)
            {
                if (needComma) { Print(",", b, 0); PrintNewLine(b); }
                if (block is string) Print((string)block, b, needNewLine ? depth + 1 : 0);
                else if (block is SyntaxKind) Print("SyntaxKind." + ((SyntaxKind)block).ToString(), b, needNewLine ? depth + 1 : 0);
                else if (block is Node) PrintRecurse(block as Node, b, depth + 1, openParenthesisOnNewLine: openParenthesisOnNewLine, closingParenthesisOnNewLine: closingParenthesisOnNewLine);
                needComma = true;
            }
            if (closingParenthesisOnNewLine && needNewLine) { PrintNewLine(b); Print(closeParen, b, depth); }
            else Print(closeParen, b, 0);
        }
    }

    internal static void PrintNewLine(StringBuilder sb)
    {
        sb.AppendLine();
    }

    internal static void Print(string line, StringBuilder sb, int indent)
    {
        PrintIndent(sb, indent);
        sb.Append(line);
    }

    internal static void PrintIndent(StringBuilder sb, int indent)
    {
        if (indent > 0)
            sb.Append(new string(' ', indent * 4));
    }

    #endregion

    #region Evaluate

    private static object Evaluate(object obj, string text, object[] args)
    {
        //Console.WriteLine("{0}{1}({2})", (obj == null ? string.Empty : obj.GetType().ToString() + ": "), text, (args == null ? "null" : string.Join(",", args.Select(x => x.ToString()).ToArray())));
        // parse generic
        string genericTypeName;
        int idx; if ((idx = text.IndexOf("<")) != -1)
        {
            genericTypeName = text.Substring(idx + 1, text.Length - idx - 2);
            text = text.Substring(0, idx);
        }
        else genericTypeName = null;
        //
        if (text.StartsWith("f:"))
        {
            var method = text.Substring(2);
            if (args == null)
            {
                var propertyInfo = GetProperty(_syntaxFactoryType, method, BindingFlags.Public | BindingFlags.Static);
                if (propertyInfo != null)
                    return propertyInfo.GetValue(null);
            }
            else if (method == "Block")
                if (args.Length == 1) return SyntaxFactory.Block(((SyntaxList<StatementSyntax>)args[0])[0]);
                else return SyntaxFactory.Block(args.Cast<StatementSyntax>().ToArray());
            else if (method == "Token") //: not needed
                if (args.Length == 1) return SyntaxFactory.Token((SyntaxKind)args[0]);
                else if (args.Length == 3) return SyntaxFactory.Token((SyntaxTriviaList)args[0], (SyntaxKind)args[1], (SyntaxTriviaList)args[2]);
                else if (args.Length == 5) return SyntaxFactory.Token((SyntaxTriviaList)args[0], (SyntaxKind)args[1], (string)args[2], (string)args[3], (SyntaxTriviaList)args[4]);
                else throw new InvalidOperationException();
            var methodInfo = GetMethod(_syntaxFactoryType, method, genericTypeName, BindingFlags.Public | BindingFlags.Static, ref args);
            if (methodInfo == null) throw new InvalidOperationException(method);
            return methodInfo.Invoke(null, args);
        }
        else if (text.StartsWith("w:"))
        {
            var method = "With" + text.Substring(2);
            var methodInfo = GetMethod(obj.GetType(), method, null, BindingFlags.Public | BindingFlags.Instance, ref args);
            if (methodInfo == null) throw new InvalidOperationException(method);
            return methodInfo.Invoke(obj, args);
        }
        else if (text.StartsWith("k:"))
        {
            return Enum.Parse(typeof(SyntaxKind), text.Substring(2));
        }
        else if (text.StartsWith("n:"))
        {
            // [http://stackoverflow.com/questions/9022059/dynamically-create-an-array-and-set-the-elements]
            var method = text.Substring(2);
            if (method == "SyntaxNodeOrToken")
            {
                var array2 = new SyntaxNodeOrToken[args.Length];
                for (var i = 0; i < args.Length; i++)
                    if (args[i] is SyntaxNode) array2[i] = (SyntaxNode)args[i];
                    else if (args[i] is SyntaxToken) array2[i] = (SyntaxToken)args[i];
                    else throw new InvalidOperationException();
                return array2;
            }
            var methodType = GetTypeFromAssemblyScan(method);
            if (methodType == null) throw new InvalidOperationException(method);
            var array = Array.CreateInstance(methodType, args.Length);
            for (var i = 0; i < args.Length; i++)
                array.SetValue(args[i], i);
            return array;
        }
        else
            throw new ArgumentOutOfRangeException("text", text);
    }

    #endregion

    #region Reflection

    readonly static Type _syntaxFactoryType = typeof(SyntaxFactory);
    readonly static Tuple<Assembly, string[]>[] _assemblyScans = {
        new Tuple<Assembly, string[]>(typeof(SyntaxFactory).Assembly, new[] { "Microsoft.CodeAnalysis.CSharp.SyntaxFactory", "Microsoft.CodeAnalysis.CSharp.Syntax" }),
        //new Tuple<Assembly, string[]>(typeof(SyntaxNodeOrToken).Assembly, new[] { "Microsoft.CodeAnalysis" })
    };

    private static Type GetTypeFromAssemblyScan(string name)
    {
        foreach (var assemblyScan in _assemblyScans)
        {
            var foundType = assemblyScan.Item2.Select(x => assemblyScan.Item1.GetType(x + "." + name)).FirstOrDefault(x => x != null);
            if (foundType != null)
                return foundType;
        }
        return null;
    }

    private static PropertyInfo GetProperty(Type type, string name, BindingFlags flags)
    {
        return type.GetProperty(name, flags);
    }

    private static MethodInfo GetMethod(Type type, string name, string genericTypeName, BindingFlags flags, ref object[] args)
    {
        var types = (args == null ? new Type[0] : args.Select(x => x.GetType()).ToArray());
        if (genericTypeName == null)
        {
            var quickMethod = type.GetMethod(name, flags, null, types, null);
            if (quickMethod != null)
                return quickMethod;
            var slowMethods = type.GetMethods(flags).Where(m => !m.IsGenericMethod && m.Name == name)
                .Select(m => new { m = m, p = m.GetParameters(), pt = m.GetParameters().Where(x => !x.HasDefaultValue).Select(x => x.ParameterType).ToArray() })
                .ToList();
            if (slowMethods.Count > 1)
                slowMethods = slowMethods.Where(m => ((types == null || types.Length == 0) && !m.p.Any()) || (types != null && m.pt.Length == types.Length))
                    .ToList();
            if (slowMethods.Count > 1)
                slowMethods = slowMethods.Where(m => types != null && m.pt.SequenceEqual(types, TypeCastableCompare.Default))
                    .ToList();
            var slowMethod = slowMethods.SingleOrDefault();
            if (slowMethod == null)
                throw new InvalidOperationException(name);
            var p = slowMethod.p;
            var newArgs = new object[p.Length];
            var argsLength = (args != null ? args.Length : 0);
            if (argsLength != 0)
                Array.Copy(args, newArgs, argsLength);
            for (var i = argsLength; i < p.Length; i++)
                newArgs[i] = p[i].DefaultValue;
            args = newArgs;
            return slowMethod.m;
        }
        //
        var genericType = _assemblyScans[0].Item2.Select(x => _assemblyScans[0].Item1.GetType(x + "." + genericTypeName)).FirstOrDefault(x => x != null);
        if (genericType == null) throw new InvalidOperationException(genericTypeName);
        var genericMethods = type.GetMethods(flags).Where(m => m.IsGenericMethod && m.ContainsGenericParameters && m.Name == name)
            .Select(m => new { m = m, p = m.GetParameters(), pt = m.GetParameters().Select(x => x.ParameterType).ToArray() })
            .ToList();
        if (genericMethods.Count > 1)
            genericMethods = genericMethods.Where(m => ((types == null || types.Length == 0) && !m.p.Any()) || (types != null && m.pt.Length == types.Length))
                 .ToList();
        if (genericMethods.Count > 1)
            genericMethods = genericMethods.Where(m => types != null && m.pt.SequenceEqual(types, TypeCastableCompare.Default))
                 .ToList();
        var genericMethod = genericMethods.Select(x => x.m).SingleOrDefault();
        return genericMethod.GetGenericMethodDefinition().MakeGenericMethod(genericType);
    }

    class TypeCastableCompare : IEqualityComparer<Type>
    {
        public static TypeCastableCompare Default = new TypeCastableCompare();
        public bool Equals(Type x, Type y) { return (x.IsAssignableFrom(y) || y.IsAssignableFrom(x)); }
        public int GetHashCode(Type a) { return a.GetHashCode(); }
    }

    #endregion
}
#endif