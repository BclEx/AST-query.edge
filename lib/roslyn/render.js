'use strict';
var edge = require('edge');
var path = require('path');

var generateClr = edge.func({
  // assemblyFile: path.join(__dirname, 'ASTquery.dll'),
  assemblyFile: path.join(__dirname, '../../src.net/ASTquery/bin/Debug/ASTquery.exe'),
  typeName: 'ASTquery.Startup',
  methodName: 'Generate'
});

function generate(tree, alters, options) {
  var body;
  generateClr({ ast: JSON.stringify(tree), alters: JSON.stringify(alters) }, function (error, result) {
    if (error) throw error; //: console.log(result);
    body = (result ? result.replace(/\r\n/g, '\n') : null);
  });
  return body;
}

module.exports = {
  generate: generate,
};
