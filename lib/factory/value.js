'use strict';
var parser = require('../roslyn/parser.js');
var Literal = require('../nodes/Literal.js');
var ObjectExpression = require('../nodes/ObjectExpression.js');
var FunctionExpression = require('../nodes/FunctionExpression.js');
var ArrayExpression = require('../nodes/ArrayExpression.js');

var parserOptions = {
};

/**
 * Create a value node from a value string
 * @param  {String} valStr Value string
 * @return {Object}        Value node
 */
exports.create = function (valStr) {
  var tree = parser.parse('var astValFactory = ' + valStr + ';', parserOptions);
  var variable = tree.body[0]['w:Members'][0]['f:SingletonList<MemberDeclarationSyntax>'][0]['f:FieldDeclaration'][0]
    .b[0]['w:Variables'][0]['f:SingletonSeparatedList<VariableDeclaratorSyntax>'][0];
  if (variable['f:VariableDeclarator'][0]['f:Identifier'][0] !== 'astValFactory')
    throw 'InvalidOperation';
  return variable.b[0]['w:Initializer'][0]['f:EqualsValueClause'][0];
};

/**
 * Wrap a value node in a relevant type helper.
 * @param  {Object} node AST node
 * @return {Object}      Wrapped node
 */
exports.wrap = function (node) {
  if (node[''] === 'Literal') {
    return new Literal(node);
  }
  if (node[''] === 'ObjectExpression') {
    return new ObjectExpression(node);
  }
  if (node[''] === 'FunctionExpression') {
    return new FunctionExpression(node);
  }
  if (node[''] === 'ArrayExpression') {
    // Prewrap the elements so it isn't consider a list of node
    return new ArrayExpression([node]);
  }
  return node;
};
