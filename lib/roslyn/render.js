'use strict';
var edge = require('edge');

var toString = edge.func(function () {/*
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
        public async Task<object> Invoke(dynamic input)
        {
             return ((SyntaxTree)input.t).ToString();
        }
    }
*/});

function generate(tree, options) {
  toString({t: tree}, function (error, result) {
    if (error) throw error;
    console.log(result);
  });
  return 'var a = 1;';
}

module.exports = {
  generate: generate,
};
