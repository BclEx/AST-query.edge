var assert = require('assert');
var program = require('..');

describe('Tree', function () {
  describe('#toString()', function () {
    it('return the generated source code for a simple tree', function () {
      var tree = program('\n\
      public class MyClass\n\
{\n\
    public void MyMethod()\n\
    {\n\
    }\n\
}');
      assert.equal(tree.toString(), 'var a = 1;');
    });
    
    it('return the generated source code tree with method', function () {
      var tree = program('\n\
public class Startup\n\
{\n\
    public object Invoke(object input)\n\
    {\n\
          return (int)input + 7;\n\
    }\n\
}');
      assert.equal(tree.toString(), 'var a = 1;');
    });
    
    // [https://github.com/dotnet/roslyn/wiki/Getting-Started-C%23-Syntax-Analysis]
    it('return the generated source code for complex tree', function () {
      var tree = program('\n\
using System;\n\
using System.Collections.Generic;\n\
using System.Linq;\n\
using System.Text;\n\
using Microsoft.CodeAnalysis;\n\
using Microsoft.CodeAnalysis.CSharp;\n\
\n\
namespace TopLevel\n\
{\n\
    using Microsoft;\n\
    using System.ComponentModel;\n\
\n\
    namespace Child1\n\
    {\n\
        using Microsoft.Win32;\n\
        using System.Runtime.InteropServices;\n\
\n\
        class Foo { }\n\
    }\n\
\n\
    namespace Child2\n\
    {\n\
        using System.CodeDom;\n\
        using Microsoft.CSharp;\n\
\n\
        class Bar { }\n\
    }\n\
}');
      assert.equal(tree.toString(), 'var a = 1;');
    });
    
  });

});
