var assert = require('assert');
var program = require('..');
var Body = require('../lib/nodes/Body');

describe('Tree', function () {
    describe('#toString()', function () {
        it('return the generated source code', function () {
            var tree = program('var a = 1');
            assert.equal(tree.toString(), 'var a = "1"');
        });
    });

    describe('#toString() - with comments', function () {
        it('return the generated source code', function () {
            var tree = program('/* comment */var a = 1');
            assert.equal(tree.toString().replace(/[\r\n\t\s]+/gm, ''), '/*comment*/vara=1;');

            tree = program('var a = {\n/* comment */a:1};');
            assert.equal(tree.toString().replace(/[\r\n\t\s]+/gm, ''), 'vara={/*comment*/a:1};');
        });
    });

    describe('#body', function () {
        it('is a Body node instance', function () {
            var tree = program('var a = 1');
            assert(tree.body instanceof Body);
        });
    });

    describe('#complex()', function () {
        it('return the generated source code for a simple tree', function () {
            var tree = program('public class MyClass { public void MyMethod() { } }');
            assert.equal(tree.toString(), 'public class MyClass\n\
{\n\
    public void MyMethod()\n\
    {\n\
    }\n\
}');
        });

        it('return the generated source code tree with method', function () {
            var tree = program('\n\
public class Startup\n\
{\n\
    public object Invoke(object input)\n\
    {\n\
	    return (int)input + \"7\";\n\
    }\n\
}');
            assert.equal(tree.toString(), 'public class Startup\n\
{\n\
    public object Invoke(object input)\n\
    {\n\
        return (int)input + \"7\";\n\
    }\n\
}');
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
            assert.equal(tree.toString(), 'using System;\n\
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
        class Foo\n\
        {\n\
        }\n\
    }\n\
\n\
    namespace Child2\n\
    {\n\
        using System.CodeDom;\n\
        using Microsoft.CSharp;\n\
\n\
        class Bar\n\
        {\n\
        }\n\
    }\n\
}');
        });

    });

});
