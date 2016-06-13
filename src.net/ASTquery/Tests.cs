#if !_LIB && !_Full
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Reflection;

[TestClass]
public class Tests
{
    [TestMethod]
    public void TestUsingSystemWithRedundantCalls()
    {
        Test1(@"using System;", @"{
  'f:CompilationUnit': null,
  'b': [
    {
      'w:Usings': [
        {
          'f:SingletonList<UsingDirectiveSyntax>': [
            {
              'f:UsingDirective': [
                {
                  'f:IdentifierName': [
                    'System'
                  ]
                }
              ],
              'b': [
                {
                  'w:UsingKeyword': [
                    {
                      'f:Token': [
                        'k:UsingKeyword'
                      ]
                    }
                  ]
                },
                {
                  'w:SemicolonToken': [
                    {
                      'f:Token': [
                        'k:SemicolonToken'
                      ]
                    }
                  ]
                }
              ]
            }
          ]
        }
      ]
    },
    {
      'w:EndOfFileToken': [
        {
          'f:Token': [
            'k:EndOfFileToken'
          ]
        }
      ]
    }
  ]
}".Replace("'", "\"")
, removeRedundantModifyingCalls: false);
    }

    [TestMethod]
    public void TestUsingSystem()
    {
        Test1(@"using System;", @"{
  'f:CompilationUnit': null,
  'b': [
    {
      'w:Usings': [
        {
          'f:SingletonList<UsingDirectiveSyntax>': [
            {
              'f:UsingDirective': [
                {
                  'f:IdentifierName': [
                    'System'
                  ]
                }
              ],
              'b': []
            }
          ]
        }
      ]
    }
  ]
}".Replace("'", "\""));
    }

    [TestMethod]
    public void TestSimpleClass()
    {
        Test1(@"class C
{
}", @"{
  'f:CompilationUnit': null,
  'b': [
    {
      'w:Members': [
        {
          'f:SingletonList<MemberDeclarationSyntax>': [
            {
              'f:ClassDeclaration': [
                'C'
              ],
              'b': [
                {
                  'w:Keyword': [
                    {
                      'f:Token': [
                        'k:ClassKeyword'
                      ]
                    }
                  ]
                },
                {
                  'w:OpenBraceToken': [
                    {
                      'f:Token': [
                        'k:OpenBraceToken'
                      ]
                    }
                  ]
                },
                {
                  'w:CloseBraceToken': [
                    {
                      'f:Token': [
                        'k:CloseBraceToken'
                      ]
                    }
                  ]
                }
              ]
            }
          ]
        }
      ]
    },
    {
      'w:EndOfFileToken': [
        {
          'f:Token': [
            'k:EndOfFileToken'
          ]
        }
      ]
    }
  ]
}".Replace("'", "\""), removeRedundantModifyingCalls: false);
    }

    [TestMethod]
    public void TestMissingToken()
    {
        Test1("class", @"{
  'f:CompilationUnit': null,
  'b': [
    {
      'w:Members': [
        {
          'f:SingletonList<MemberDeclarationSyntax>': [
            {
              'f:ClassDeclaration': [
                {
                  'f:MissingToken': [
                    'k:IdentifierToken'
                  ]
                }
              ],
              'b': [
                {
                  'w:Keyword': [
                    {
                      'f:Token': [
                        'k:ClassKeyword'
                      ]
                    }
                  ]
                },
                {
                  'w:OpenBraceToken': [
                    {
                      'f:MissingToken': [
                        'k:OpenBraceToken'
                      ]
                    }
                  ]
                },
                {
                  'w:CloseBraceToken': [
                    {
                      'f:MissingToken': [
                        'k:CloseBraceToken'
                      ]
                    }
                  ]
                }
              ]
            }
          ]
        }
      ]
    },
    {
      'w:EndOfFileToken': [
        {
          'f:Token': [
            'k:EndOfFileToken'
          ]
        }
      ]
    }
  ]
}".Replace("'", "\""), removeRedundantModifyingCalls: false);
    }

    [TestMethod]
    public void TestGlobal()
    {
        Test(@"class C { void M() { global::System.String s; } }");
    }

    [TestMethod]
    public void TestEmptyBlock()
    {
        Test(@"class C { void M() { } }");
    }

    [TestMethod]
    public void TestInterpolatedString()
    {
        Test(@"class C { string s = $""a""; }");
    }

    [TestMethod]
    public void TestAttribute()
    {
        Test(@"[Foo]class C { }");
    }

    [TestMethod]
    public void TestHelloWorld()
    {
        Test1(@"using System;

    namespace N
    {
        class Program
        {
            static void Main(string[] args)
            {
                Console.WriteLine(""Hello World""); // comment
            }
        }
    }", @"{
  'f:CompilationUnit': null,
  'b': [
    {
      'w:Usings': [
        {
          'f:SingletonList<UsingDirectiveSyntax>': [
            {
              'f:UsingDirective': [
                {
                  'f:IdentifierName': [
                    'System'
                  ]
                }
              ],
              'b': [
                {
                  'w:UsingKeyword': [
                    {
                      'f:Token': [
                        'k:UsingKeyword'
                      ]
                    }
                  ]
                },
                {
                  'w:SemicolonToken': [
                    {
                      'f:Token': [
                        'k:SemicolonToken'
                      ]
                    }
                  ]
                }
              ]
            }
          ]
        }
      ]
    },
    {
      'w:Members': [
        {
          'f:SingletonList<MemberDeclarationSyntax>': [
            {
              'f:NamespaceDeclaration': [
                {
                  'f:IdentifierName': [
                    'N'
                  ]
                }
              ],
              'b': [
                {
                  'w:NamespaceKeyword': [
                    {
                      'f:Token': [
                        'k:NamespaceKeyword'
                      ]
                    }
                  ]
                },
                {
                  'w:OpenBraceToken': [
                    {
                      'f:Token': [
                        'k:OpenBraceToken'
                      ]
                    }
                  ]
                },
                {
                  'w:Members': [
                    {
                      'f:SingletonList<MemberDeclarationSyntax>': [
                        {
                          'f:ClassDeclaration': [
                            'Program'
                          ],
                          'b': [
                            {
                              'w:Keyword': [
                                {
                                  'f:Token': [
                                    'k:ClassKeyword'
                                  ]
                                }
                              ]
                            },
                            {
                              'w:OpenBraceToken': [
                                {
                                  'f:Token': [
                                    'k:OpenBraceToken'
                                  ]
                                }
                              ]
                            },
                            {
                              'w:Members': [
                                {
                                  'f:SingletonList<MemberDeclarationSyntax>': [
                                    {
                                      'f:MethodDeclaration': [
                                        {
                                          'f:PredefinedType': [
                                            {
                                              'f:Token': [
                                                'k:VoidKeyword'
                                              ]
                                            }
                                          ]
                                        },
                                        {
                                          'f:Identifier': [
                                            'Main'
                                          ]
                                        }
                                      ],
                                      'b': [
                                        {
                                          'w:Modifiers': [
                                            {
                                              'f:TokenList': [
                                                {
                                                  'f:Token': [
                                                    'k:StaticKeyword'
                                                  ]
                                                }
                                              ]
                                            }
                                          ]
                                        },
                                        {
                                          'w:ParameterList': [
                                            {
                                              'f:ParameterList': [
                                                {
                                                  'f:SingletonSeparatedList<ParameterSyntax>': [
                                                    {
                                                      'f:Parameter': [
                                                        {
                                                          'f:Identifier': [
                                                            'args'
                                                          ]
                                                        }
                                                      ],
                                                      'b': [
                                                        {
                                                          'w:Type': [
                                                            {
                                                              'f:ArrayType': [
                                                                {
                                                                  'f:PredefinedType': [
                                                                    {
                                                                      'f:Token': [
                                                                        'k:StringKeyword'
                                                                      ]
                                                                    }
                                                                  ]
                                                                }
                                                              ],
                                                              'b': [
                                                                {
                                                                  'w:RankSpecifiers': [
                                                                    {
                                                                      'f:SingletonList<ArrayRankSpecifierSyntax>': [
                                                                        {
                                                                          'f:ArrayRankSpecifier': [
                                                                            {
                                                                              'f:SingletonSeparatedList<ExpressionSyntax>': [
                                                                                {
                                                                                  'f:OmittedArraySizeExpression': null,
                                                                                  'b': [
                                                                                    {
                                                                                      'w:OmittedArraySizeExpressionToken': [
                                                                                        {
                                                                                          'f:Token': [
                                                                                            'k:OmittedArraySizeExpressionToken'
                                                                                          ]
                                                                                        }
                                                                                      ]
                                                                                    }
                                                                                  ]
                                                                                }
                                                                              ]
                                                                            }
                                                                          ],
                                                                          'b': [
                                                                            {
                                                                              'w:OpenBracketToken': [
                                                                                {
                                                                                  'f:Token': [
                                                                                    'k:OpenBracketToken'
                                                                                  ]
                                                                                }
                                                                              ]
                                                                            },
                                                                            {
                                                                              'w:CloseBracketToken': [
                                                                                {
                                                                                  'f:Token': [
                                                                                    'k:CloseBracketToken'
                                                                                  ]
                                                                                }
                                                                              ]
                                                                            }
                                                                          ]
                                                                        }
                                                                      ]
                                                                    }
                                                                  ]
                                                                }
                                                              ]
                                                            }
                                                          ]
                                                        }
                                                      ]
                                                    }
                                                  ]
                                                }
                                              ],
                                              'b': [
                                                {
                                                  'w:OpenParenToken': [
                                                    {
                                                      'f:Token': [
                                                        'k:OpenParenToken'
                                                      ]
                                                    }
                                                  ]
                                                },
                                                {
                                                  'w:CloseParenToken': [
                                                    {
                                                      'f:Token': [
                                                        'k:CloseParenToken'
                                                      ]
                                                    }
                                                  ]
                                                }
                                              ]
                                            }
                                          ]
                                        },
                                        {
                                          'w:Body': [
                                            {
                                              'f:Block': [
                                                {
                                                  'f:SingletonList<StatementSyntax>': [
                                                    {
                                                      'f:ExpressionStatement': [
                                                        {
                                                          'f:InvocationExpression': [
                                                            {
                                                              'f:MemberAccessExpression': [
                                                                {
                                                                  'k:SimpleMemberAccessExpression': null
                                                                },
                                                                {
                                                                  'f:IdentifierName': [
                                                                    'Console'
                                                                  ]
                                                                },
                                                                {
                                                                  'f:IdentifierName': [
                                                                    'WriteLine'
                                                                  ]
                                                                }
                                                              ],
                                                              'b': [
                                                                {
                                                                  'w:OperatorToken': [
                                                                    {
                                                                      'f:Token': [
                                                                        'k:DotToken'
                                                                      ]
                                                                    }
                                                                  ]
                                                                }
                                                              ]
                                                            }
                                                          ],
                                                          'b': [
                                                            {
                                                              'w:ArgumentList': [
                                                                {
                                                                  'f:ArgumentList': [
                                                                    {
                                                                      'f:SingletonSeparatedList<ArgumentSyntax>': [
                                                                        {
                                                                          'f:Argument': [
                                                                            {
                                                                              'f:LiteralExpression': [
                                                                                {
                                                                                  'k:StringLiteralExpression': null
                                                                                },
                                                                                {
                                                                                  'f:Literal': [
                                                                                    'Hello World'
                                                                                  ]
                                                                                }
                                                                              ]
                                                                            }
                                                                          ]
                                                                        }
                                                                      ]
                                                                    }
                                                                  ],
                                                                  'b': [
                                                                    {
                                                                      'w:OpenParenToken': [
                                                                        {
                                                                          'f:Token': [
                                                                            'k:OpenParenToken'
                                                                          ]
                                                                        }
                                                                      ]
                                                                    },
                                                                    {
                                                                      'w:CloseParenToken': [
                                                                        {
                                                                          'f:Token': [
                                                                            'k:CloseParenToken'
                                                                          ]
                                                                        }
                                                                      ]
                                                                    }
                                                                  ]
                                                                }
                                                              ]
                                                            }
                                                          ]
                                                        }
                                                      ],
                                                      'b': [
                                                        {
                                                          'w:SemicolonToken': [
                                                            {
                                                              'f:Token': [
                                                                {
                                                                  'f:TriviaList': null
                                                                },
                                                                'k:SemicolonToken',
                                                                {
                                                                  'f:TriviaList': [
                                                                    {
                                                                      'f:Comment': [
                                                                        '// comment'
                                                                      ]
                                                                    }
                                                                  ]
                                                                }
                                                              ]
                                                            }
                                                          ]
                                                        }
                                                      ]
                                                    }
                                                  ]
                                                }
                                              ],
                                              'b': [
                                                {
                                                  'w:OpenBraceToken': [
                                                    {
                                                      'f:Token': [
                                                        'k:OpenBraceToken'
                                                      ]
                                                    }
                                                  ]
                                                },
                                                {
                                                  'w:CloseBraceToken': [
                                                    {
                                                      'f:Token': [
                                                        'k:CloseBraceToken'
                                                      ]
                                                    }
                                                  ]
                                                }
                                              ]
                                            }
                                          ]
                                        }
                                      ]
                                    }
                                  ]
                                }
                              ]
                            },
                            {
                              'w:CloseBraceToken': [
                                {
                                  'f:Token': [
                                    'k:CloseBraceToken'
                                  ]
                                }
                              ]
                            }
                          ]
                        }
                      ]
                    }
                  ]
                },
                {
                  'w:CloseBraceToken': [
                    {
                      'f:Token': [
                        'k:CloseBraceToken'
                      ]
                    }
                  ]
                }
              ]
            }
          ]
        }
      ]
    },
    {
      'w:EndOfFileToken': [
        {
          'f:Token': [
            'k:EndOfFileToken'
          ]
        }
      ]
    }
  ]
}".Replace("'", "\""), removeRedundantModifyingCalls: false);
    }

    [TestMethod]
    public void TestComment()
    {
        Test(@"class C
    {
      void M()
      {
        A(""M""); // comment
      }
    }");
    }

    [TestMethod]
    public void TestSimpleStringLiteral()
    {
        Test("class C { string s = \"z\"; }"); // "z"
    }

    [TestMethod]
    public void TestStringLiteralWithBackslash()
    {
        Test("class C { string s = \"a\\b\"");
    }

    [TestMethod]
    public void TestSimpleIntLiteral()
    {
        Test("class C { int i = 42; }");
    }

    [TestMethod]
    public void TestSimpleCharLiteral()
    {
        Test("class C { char c = 'z'; }");
    }

    [TestMethod]
    public void TestTrueFalseAndNull()
    {
        Test("class C { var x = true ? false : null; }");
    }

    [TestMethod]
    public void Roundtrip1()
    {
        Test("class C { string s = \"\\\"\"; }"); // "\""
    }

    [TestMethod]
    public void Roundtrip2()
    {
        Test(@"using System;

    class Program
    {
        static void Main(string[] args)
        {

        }
    }");
    }

    [TestMethod]
    public void Roundtrip3()
    {
        Test("class C { string s = \"\\\"\"; }");
    }

    [TestMethod]
    public void Roundtrip4()
    {
        Test("class C { string s = @\" zzz \"\" zzz \"; }");
    }

    [TestMethod]
    public void Roundtrip5()
    {
        Test(@"class C { void M() { M(1, 2); } }");
    }

    [TestMethod]
    public void Roundtrip6()
    {
        Test(@"class C { bool b = true; }");
    }

    [TestMethod]
    public void Roundtrip7()
    {
        Test(@"#error Foo");
    }

    [TestMethod]
    public void Roundtrip8()
    {
        Test(@"#if false
    int i
    " + "#endif");
    }

    [TestMethod]
    public void Roundtrip9()
    {
        Test(@"\\\");
    }

    [TestMethod]
    public void Roundtrip10()
    {
        Test(@"/// baz <summary>foo</summary> bar");
    }

    [TestMethod]
    public void Roundtrip11()
    {
        Test(@"class /*///*/C");
    }

    [TestMethod]
    public void Roundtrip12()
    {
        Test("#pragma checksum \"file.txt\" \"{00000000-0000-0000-0000-000000000000}\" \"2453\"");
    }

    [TestMethod]
    public void Roundtrip13()
    {
        Test(@"class \\u0066 { }");
    }

    [TestMethod]
    public void Roundtrip14()
    {
        Test(@"class C { }");
    }

    [TestMethod]
    public void Roundtrip15()
    {
        Test(@"class C { void M() { ((Action)(async () =>
                    {
                    }))(); } }");
    }

    [TestMethod]
    public void Roundtrip16()
    {
        Test(@"class C { void M() { a ? b : c; } }");
    }

    private static string GetPath(string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
            return Path.GetFullPath(relativePath);
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), relativePath));
    }

    private void RoundtripFile(string filePath)
    {
        Test(File.ReadAllText(GetPath(filePath)), useDefaultFormatting: false, removeRedundantCalls: false, useJson: true);
    }

    [TestMethod]
    public void Roundtrip20()
    {
        Test("#line 1 \"a\\b\"");
    }

    [TestMethod]
    public void Roundtrip21()
    {
        Test("#line 1 \"a\\\b\"");
    }

    [TestMethod]
    public void Roundtrip22()
    {
        Test("#pragma checksum \"..\\..\"");
    }

    [TestMethod]
    [WorkItem(15194)]
    public void Roundtrip23()
    {
        Test("class C { void P { a } }");
    }

    [TestMethod]
    public void Roundtrip24()
    {
        Test(@"///
    class C { }");
    }

    [TestMethod]
    public void Roundtrip25()
    {
        Test("class C { void M(__arglist) { M(__arglist); } }");
    }

    [TestMethod]
    public void Roundtrip26()
    {
        Test(@"
    namespace @N
    {
       public class @A
       {
           public @string @P { get; set; }
       }
    }
    ");
    }

    [TestMethod]
    public void Roundtrip27()
    {
        Test("class C { void M() { int x; x = 42; } }");
    }

    [TestMethod]
    public void Roundtrip28()
    {
        Test(@"[module: System.Copyright(""\n\t\u0123(C) \""2009"" + ""\u0123"")]");
    }

    [TestMethod]
    public void SwitchCase()
    {
        Test(@"class C { public C() { switch(0) { case 1: break; default: break;} } } ");
    }

    [TestMethod]
    public void TestObsoleteAttribute()
    {
        Test("class C { int i => 0; }");
    }

    [TestMethod]
    public void TestNewlineInConstant()
    {
        Test(@"[module: System.Copyright(""\n"")]");
    }

    [TestMethod]
    public void TestQuoteInLiteral()
    {
        Test(@"[module: A(""\"""")]");
    }

    [TestMethod]
    public void TestQuoteInVerbatimLiteral()
    {
        Test(@"[module: A(@"""""""")]");
    }

    [TestMethod]
    public void TestBackslashInLiteral()
    {
        Test(@"[module: A(""\\"")]");
    }

    [TestMethod]
    public void RoundtripMissingToken()
    {
        Test("class");
    }

    [TestMethod]
    public void TestXmlDocComment()
    {
        Test(@"    /// <summary>
        /// test
        /// </summary>
    class C { }");
    }

    private void Test1(string sourceText, string expected, bool useDefaultFormatting = true, bool removeRedundantModifyingCalls = true)
    {
        var quoter = new Quoter
        {
            UseDefaultFormatting = useDefaultFormatting,
            RemoveRedundantModifyingCalls = removeRedundantModifyingCalls,
        };
        var actual = quoter.Quote(sourceText, true);
        Assert.AreEqual(expected, actual);
        Test(sourceText);
    }

    private void Test(string sourceText)
    {
        Test(sourceText, useDefaultFormatting: true, removeRedundantCalls: true, useJson: true);
        Test(sourceText, useDefaultFormatting: true, removeRedundantCalls: true, useJson: false);
    }

    private static void Test(string sourceText, bool useDefaultFormatting, bool removeRedundantCalls, bool useJson)
    {
        if (useDefaultFormatting)
            sourceText = CSharpSyntaxTree.ParseText(sourceText).GetRoot()
                .NormalizeWhitespace().ToFullString();
        var quoter = new Quoter
        {
            UseDefaultFormatting = useDefaultFormatting,
            RemoveRedundantModifyingCalls = removeRedundantCalls
        };
        string actual;
        if (useJson)
        {
            var generatedCode = quoter.Quote(sourceText);
            actual = ApiCall.FromJsonToSyntax(generatedCode).ToFullString();
        }
        else
        {
            var generatedCode = quoter.Parse(sourceText);
            actual = generatedCode.ToSyntax().ToFullString();
        }
        //if (sourceText != resultText)
        //{
        //    //File.WriteAllText(@"D:\1.txt", sourceText);
        //    //File.WriteAllText(@"D:\2.txt", resultText);
        //    //File.WriteAllText(@"D:\3.txt", generatedCode);
        //}
        Assert.AreEqual(sourceText, actual);
    }

    public void CheckSourceFiles()
    {
        var rootFolder = @"C:\roslyn-internal\Closed\Test\Files\";
        var files = Directory.GetFiles(rootFolder, "*.cs", SearchOption.AllDirectories);
        for (var i = 0; i < files.Length; i++)
            VerifyRoundtrip(files[i]);
    }

    public void VerifyRoundtrip(string file)
    {
        try
        {
            var sourceText = File.ReadAllText(file);
            if (sourceText.Length > 50000)
            {
                //Log("Skipped large file: " + file);
                return;
            }
            Test(sourceText);
        }
        catch (Exception) { Log("Failed: " + file); }
    }

    private static void Log(string text)
    {
        File.AppendAllText(@"Failed.txt", text + Environment.NewLine);
    }
}
#endif