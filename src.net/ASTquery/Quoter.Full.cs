#if _Full
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

public class Quoter
{
    public bool OpenParenthesisOnNewLine { get; set; }
    public bool ClosingParenthesisOnNewLine { get; set; }
    public bool UseDefaultFormatting { get; set; }
    public bool RemoveRedundantModifyingCalls { get; set; }
    public bool ShortenCodeWithUsingStatic { get; set; }

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

    static readonly ScriptOptions _options = ScriptOptions.Default
        .AddReferences(typeof(SyntaxNode).Assembly, typeof(CSharpSyntaxNode).Assembly)
        .AddImports(
            "System",
            "Microsoft.CodeAnalysis",
            "Microsoft.CodeAnalysis.CSharp",
            "Microsoft.CodeAnalysis.CSharp.Syntax",
            "Microsoft.CodeAnalysis.CSharp.SyntaxFactory");

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

    public Quoter()
    {
        UseDefaultFormatting = true;
        RemoveRedundantModifyingCalls = true;
    }

    public string Quote(string sourceText)
    {
        var sourceTree = CSharpSyntaxTree.ParseText(sourceText);
        return Print(Parse(sourceTree.GetRoot()));
    }

    public string Quote(SyntaxNode node)
    {
        var rootApiCall = QuoteRecurse(node, name: null);
        if (UseDefaultFormatting)
            rootApiCall.Add(new MethodCall { Name = ".NormalizeWhitespace" });
        return Print(rootApiCall);
    }

    public ApiCall Parse(string sourceText)
    {
        var sourceTree = CSharpSyntaxTree.ParseText(sourceText);
        return Parse(sourceTree.GetRoot());
    }

    public ApiCall Parse(SyntaxNode node)
    {
        Console.WriteLine(node.ToString());
        var rootApiCall = QuoteRecurse(node, name: null);
        if (UseDefaultFormatting)
            rootApiCall.Add(new MethodCall { Name = ".NormalizeWhitespace" });
        return rootApiCall;
    }

    #region Utility

    private string SyntaxFactory(string text)
    {
        if (!ShortenCodeWithUsingStatic)
            text = "SyntaxFactory." + text;
        return text;
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
    private ApiCall FindValue(string parameterName, IEnumerable<ApiCall> values)
    {
        return values.FirstOrDefault(v => parameterName.Equals(v.Name, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Recurse

    private ApiCall QuoteRecurse(object treeElement, string name = null)
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
    private ApiCall QuoteNodeRecurse(SyntaxNode node, string name)
    {
        var quotedPropertyValues = QuotePropertyValues(node);
        var factoryMethod = PickFactoryMethodToCreateNode(node);
        var factoryMethodName = factoryMethod.Name;
        if (!ShortenCodeWithUsingStatic)
            factoryMethodName = factoryMethod.DeclaringType.Name + "." + factoryMethodName;
        var factoryMethodCall = new MethodCall { Name = factoryMethodName };
        var codeBlock = new ApiCall(name, factoryMethodCall);
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
        factory = candidatesWithMinParameterCount[0];
        return factory;
    }

    /// <summary>
    /// Adds information about subsequent modifying fluent interface style calls on an object (like foo.With(...).With(...))
    /// </summary>
    private void AddModifyingCalls(object treeElement, ApiCall apiCall, List<ApiCall> values)
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
                methodName = "." + methodName;
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

    internal static string ProperCase(string str)
    {
        return char.ToUpperInvariant(str[0]) + str.Substring(1);
    }

    private void AddModifyingCall(ApiCall apiCall, MethodCall methodCall)
    {
        if (RemoveRedundantModifyingCalls)
        {
            var before = Evaluate(apiCall, UseDefaultFormatting);
            apiCall.Add(methodCall);
            var after = Evaluate(apiCall, UseDefaultFormatting);
            if (before == after)
                apiCall.Remove(methodCall);
            return;
        }
        apiCall.Add(methodCall);
    }

    /// <summary>
    /// Inspects the property values of the <paramref name="node"/> object using Reflection and
    /// creates API call descriptions for the property values recursively. Properties that are not
    /// essential to the shape of the syntax tree (such as Span) are ignored.
    /// </summary>
    private List<ApiCall> QuotePropertyValues(SyntaxNode node)
    {
        var result = new List<ApiCall>();
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
            result.Add(new ApiCall("Kind", "SyntaxKind." + node.Kind().ToString()));
        return result;
    }

    /// <summary>
    /// Parse the value of the property <paramref name="property"/> of object <paramref
    /// name="node"/>
    /// </summary>
    private ApiCall QuotePropertyValue(SyntaxNode node, PropertyInfo property)
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
        {
            var text = value.ToString();
            var verbatim = text.Contains("\r") || text.Contains("\n");
            return new ApiCall(property.Name, EscapeAndQuote(text, verbatim));
        }
        if (value is bool)
            return new ApiCall(property.Name, value.ToString().ToLowerInvariant());
        return null;
    }

    private void AddFactoryMethodArguments(MethodInfo factory, MethodCall factoryMethodCall, List<ApiCall> quotedValues)
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
                    var argument = (methodCall.Arguments[0] as ApiCall);
                    var arrayCreation = (argument.FactoryMethodCall as MethodCall);
                    if (argument != null && arrayCreation != null && arrayCreation.Name.StartsWith("new ") && arrayCreation.Name.EndsWith("[]"))
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
                if (methodCall != null && methodCall.Name == SyntaxFactory("Identifier"))
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
                if (methodCall != null && methodCall.Name == SyntaxFactory("Identifier") && methodCall.Arguments.Count == 1)
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

    private ApiCall QuoteList(IEnumerable syntaxList, string name)
    {
        var sourceList = syntaxList.Cast<object>();
        var methodName = SyntaxFactory("List");
        string listType = null;
        var propertyType = syntaxList.GetType();
        if (propertyType.IsGenericType)
        {
            var methodType = propertyType.GetGenericArguments()[0].Name;
            if (propertyType.GetGenericTypeDefinition() == typeof(SeparatedSyntaxList<>))
            {
                listType = "SyntaxNodeOrToken";
                methodName = SyntaxFactory("SeparatedList");
                sourceList = ((SyntaxNodeOrTokenList)syntaxList.GetType().GetMethod("GetWithSeparators").Invoke(syntaxList, null)).Cast<object>().ToArray();
            }
            else listType = methodType;
            methodName += "<" + methodType + ">";
        }
        if (propertyType.Name == "SyntaxTokenList")
            methodName = SyntaxFactory("TokenList");
        else if (propertyType.Name == "SyntaxTriviaList")
            methodName = SyntaxFactory("TriviaList");
        var elements = new List<object>(sourceList
            .Select(o => QuoteRecurse(o))
            .Where(cb => cb != null)).ToList();
        if (elements.Count == 0)
            return null;
        else if (elements.Count == 1)
        {
            if (methodName.StartsWith("List"))
                methodName = "SingletonList" + methodName.Substring("List".Length);
            else if (methodName.StartsWith(SyntaxFactory("List")))
                methodName = SyntaxFactory("SingletonList") + methodName.Substring(SyntaxFactory("List").Length);
            else if (methodName.StartsWith("SeparatedList"))
                methodName = "SingletonSeparatedList" + methodName.Substring("SeparatedList".Length);
            else if (methodName.StartsWith(SyntaxFactory("SeparatedList")))
                methodName = SyntaxFactory("SingletonSeparatedList") + methodName.Substring(SyntaxFactory("SeparatedList").Length);
        }
        else
            elements = new List<object>
            {
                new ApiCall("methodName", "new " + listType + "[]", elements, useCurliesInsteadOfParentheses: true)
            };
        return new ApiCall(name, methodName, elements);
    }

    #endregion

    #region Token

    private ApiCall QuoteToken(SyntaxToken value, string name)
    {
        SyntaxKind valueKind;
        if (value == default(SyntaxToken) || (valueKind = value.Kind()) == SyntaxKind.None)
            return null;
        var arguments = new List<object>();
        var methodName = SyntaxFactory("Token");
        var verbatim =
            value.Text.StartsWith("@") ||
            value.Text.Contains("\r") ||
            value.Text.Contains("\n");
        var escapedTokenValueText = EscapeAndQuote(value.ToString(), verbatim);
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
            methodName = (value.IsMissing ? SyntaxFactory("MissingToken") : SyntaxFactory("Identifier"));
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
            if (valueKind == SyntaxKind.XmlTextLiteralNewLineToken) methodName = SyntaxFactory("XmlTextNewLine");
            else if (valueKind == SyntaxKind.XmlEntityLiteralToken) methodName = SyntaxFactory("XmlEntity");
            else methodName = SyntaxFactory("XmlTextLiteral");
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
            methodName = SyntaxFactory("Literal");
            var shouldAddTrivia = (leading != null || trailing != null);
            if (shouldAddTrivia)
                arguments.Add(leading ?? GetEmptyTrivia("LeadingTrivia"));
            var escapedText = EscapeAndQuote(value.Text);
            string escapedValue;
            if (valueKind == SyntaxKind.CharacterLiteralToken) escapedValue = EscapeAndQuote(value.ValueText, "'");
            else if (valueKind != SyntaxKind.StringLiteralToken) escapedValue = value.ValueText;
            else escapedValue = EscapeAndQuote(value.ValueText);
            if (shouldAddTrivia ||
                (valueKind == SyntaxKind.StringLiteralToken && value.ToString() != Microsoft.CodeAnalysis.CSharp.SyntaxFactory.Literal(value.ValueText).ToString()))
                arguments.Add(escapedText);
            arguments.Add(escapedValue);
            if (shouldAddTrivia)
                arguments.Add(trailing ?? GetEmptyTrivia("TrailingTrivia"));
        }
        else
        {
            if (value.IsMissing)
                methodName = SyntaxFactory("MissingToken");
            else if (valueKind == SyntaxKind.BadToken)
            {
                methodName = SyntaxFactory("BadToken");
                leading = leading ?? GetEmptyTrivia("LeadingTrivia");
                trailing = trailing ?? GetEmptyTrivia("TrailingTrivia");
            }
            var tokenValue = (valueKind == SyntaxKind.BadToken ? (object)escapedTokenValueText : valueKind);
            AddIfNotNull(arguments, leading);
            arguments.Add(tokenValue);
            AddIfNotNull(arguments, trailing);
        }
        return new ApiCall(name, methodName, arguments);
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
        return new ApiCall(parentPropertyName, SyntaxFactory("TriviaList"), arguments: null);
    }

    private ApiCall QuoteTrivia(SyntaxTrivia syntaxTrivia)
    {
        var factoryMethodName = SyntaxFactory("Trivia");
        var text = syntaxTrivia.ToString();
        SyntaxKind syntaxKind;
        if (syntaxTrivia.FullSpan.Length == 0 || ((syntaxKind = syntaxTrivia.Kind()) == SyntaxKind.WhitespaceTrivia && UseDefaultFormatting))
            return null;
        PropertyInfo triviaFactoryProperty;
        if (_triviaFactoryProperties.TryGetValue(text, out triviaFactoryProperty) && syntaxKind == ((SyntaxTrivia)triviaFactoryProperty.GetValue(null)).Kind())
            return (UseDefaultFormatting ? null : new ApiCall(null, SyntaxFactory(triviaFactoryProperty.Name)));
        if (!string.IsNullOrEmpty(text) && string.IsNullOrWhiteSpace(text) && syntaxKind == SyntaxKind.WhitespaceTrivia) factoryMethodName = (UseDefaultFormatting ? null : SyntaxFactory("Whitespace"));
        else if (syntaxKind == SyntaxKind.SingleLineCommentTrivia || syntaxKind == SyntaxKind.MultiLineCommentTrivia) factoryMethodName = SyntaxFactory("Comment");
        else if (syntaxKind == SyntaxKind.SingleLineDocumentationCommentTrivia || syntaxKind == SyntaxKind.MultiLineDocumentationCommentTrivia) factoryMethodName = SyntaxFactory("DocumentComment");
        else if (syntaxKind == SyntaxKind.PreprocessingMessageTrivia) factoryMethodName = SyntaxFactory("PreprocessingMessage");
        else if (syntaxKind == SyntaxKind.DisabledTextTrivia) factoryMethodName = SyntaxFactory("DisabledText");
        else if (syntaxKind == SyntaxKind.DocumentationCommentExteriorTrivia) factoryMethodName = SyntaxFactory("DocumentationCommentExterior");
        if (factoryMethodName == null)
            return null;
        var verbatim = (text.Contains("\r") || text.Contains("\n"));
        var argument = (syntaxTrivia.HasStructure ? (object)QuoteNodeRecurse(syntaxTrivia.GetStructure(), "Structure") : EscapeAndQuote(text, verbatim: verbatim));
        return new ApiCall(null, factoryMethodName, CreateArgumentList(argument));
    }

    #endregion

    #region Escape

    /// <summary>
    /// Escapes strings to be included within "" using C# escaping rules
    /// </summary>
    public static string Escape(string text, bool escapeVerbatim = false)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < text.Length; i++)
        {
            string toAppend;
            if (text[i] == '"') toAppend = (escapeVerbatim ? "\"\"" : "\\\"");
            else if (text[i] == '\\' && !escapeVerbatim) toAppend = "\\\\";
            else toAppend = text[i].ToString();
            sb.Append(toAppend);
        }
        return sb.ToString();
    }

    public static string EscapeAndQuote(string text, string quoteChar = "\"")
    {
        bool verbatim = (text.Contains("\n") || text.Contains("\r"));
        return EscapeAndQuote(text, verbatim, quoteChar);
    }

    public static string EscapeAndQuote(string text, bool verbatim, string quoteChar = "\"")
    {
        if (text == Environment.NewLine)
            return "Environment.NewLine";
        if (text == "\n")
            return "\"\\n\"";
        text = Escape(text, verbatim);
        text = SurroundWithQuotes(text, quoteChar);
        if (verbatim)
            text = "@" + text;
        return text;
    }

    private static string SurroundWithQuotes(string text, string quoteChar = "\"")
    {
        text = quoteChar + text + quoteChar;
        return text;
    }

    #endregion

    #region Evaluate

    /// <summary>
    /// Calls the Roslyn syntax API to actually create the syntax tree object and return the source
    /// code generated by the syntax tree.
    /// </summary>
    /// <param name="apiCallString">Code that calls Roslyn syntax APIs as a string</param>
    /// <returns>The string that corresponds to the code of the syntax tree.</returns>
    public string Evaluate(string apiCallString, bool normalizeWhitespace = false)
    {
        var generatedNode = CSharpScript.EvaluateAsync<SyntaxNode>(apiCallString, _options).Result;
        if (normalizeWhitespace)
            generatedNode = generatedNode.NormalizeWhitespace();
        var resultText = generatedNode.ToFullString();
        return resultText;
    }

    private string Evaluate(ApiCall apiCall, bool normalizeWhitespace = false)
    {
        return Evaluate(Print(apiCall), normalizeWhitespace);
    }

    #endregion

    #region Print

    /// <summary>
    /// Flattens a tree of ApiCalls into a single string.
    /// </summary>
    public string Print(ApiCall root)
    {
        var b = new StringBuilder();
        PrintRecurse(root, b, 0, OpenParenthesisOnNewLine, ClosingParenthesisOnNewLine);
        return b.ToString();
    }

    internal static string PrintWithDefaultFormatting(ApiCall root)
    {
        var b = new StringBuilder();
        PrintRecurse(root, b, 0, openParenthesisOnNewLine: false, closingParenthesisOnNewLine: false);
        return b.ToString();
    }

    private static void PrintRecurse(ApiCall codeBlock, StringBuilder b, int depth = 0, bool openParenthesisOnNewLine = false, bool closingParenthesisOnNewLine = false)
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
            var needNewLine = true;
            if (methodCall.Arguments.Count == 1 && (methodCall.Arguments[0] is string || methodCall.Arguments[0] is SyntaxKind))
                needNewLine = false;
            if (openParenthesisOnNewLine && needNewLine)
            {
                PrintNewLine(b);
                Print(openParen, b, depth);
            }
            else
                Print(openParen, b, 0);
            if (needNewLine)
                PrintNewLine(b);
            var needComma = false;
            foreach (var block in methodCall.Arguments)
            {
                if (needComma)
                {
                    Print(",", b, 0);
                    PrintNewLine(b);
                }
                if (block is string)
                    Print((string)block, b, needNewLine ? depth + 1 : 0);
                else if (block is SyntaxKind)
                    Print("SyntaxKind." + ((SyntaxKind)block).ToString(), b, needNewLine ? depth + 1 : 0);
                else if (block is ApiCall)
                    PrintRecurse(block as ApiCall, b, depth + 1, openParenthesisOnNewLine: openParenthesisOnNewLine, closingParenthesisOnNewLine: closingParenthesisOnNewLine);
                needComma = true;
            }
            if (closingParenthesisOnNewLine && needNewLine)
            {
                PrintNewLine(b);
                Print(closeParen, b, depth);
            }
            else
                Print(closeParen, b, 0);
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
}
#endif