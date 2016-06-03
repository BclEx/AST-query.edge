'use strict';
var edge = require('edge');

var parseText = edge.func('lib/roslyn/Syntax.csx');

function Tree(source, options) {
    parseText(source, function (error, result) {
        if (error) throw error;
        console.log(result);
    });
}

Tree.prototype.body = function () {
    return '';
};

function parse(source, options) {
    return new Tree(source, options);
}

module.exports = {
    parse: parse,
};

// [https://joshvarty.wordpress.com/2014/07/06/learn-roslyn-now-part-2-analyzing-syntax-trees-with-linq/]
// var helloWorld = edge.func(function () {/*
//     async (input) => { 
//         return ".NET Welcomes " + input.ToString(); 
//     }
// */});
//   helloWorld('JavaScript', function (error, result) {
//     if (error) throw error;
//     console.log(result);
//   });

// var toStringClr = edge.func(function () {/*
//     async (object input) => { 
//         return ((Microsoft.CodeAnalysis.SyntaxTree)input).ToString(); 
//     }
// */});