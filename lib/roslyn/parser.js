'use strict';
var path = require('path');
var edge = require('edge');

var parseClr = edge.func({
    assemblyFile: path.join(__dirname, 'ASTquery.dll'),
    // assemblyFile: path.join(__dirname, '../../src.net/ASTquery/bin/Debug/ASTquery.exe'),
    typeName: 'ASTquery.Startup',
    methodName: 'Parse'
});

function Tree(source, options) {
    this.root = null;
    this.body = [];
    parseClr(source, function (error, result) {
        if (error) throw error; //: console.log(result);
        this.root = JSON.parse(result);
        this.body = this.root.b;
    }.bind(this));
}

function parse(source, options) {
    return new Tree(source, options);
}

module.exports = {
    parse: parse,
};