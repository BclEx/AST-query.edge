'use strict';

exports.pushQuery = pushQuery;
exports.pushAdditional = pushAdditional;
exports.syntaxLiteral = syntaxLiteral;
exports.syntaxList = syntaxList;
exports.syntaxSeperatedList = syntaxSeperatedList;

var _ = require('lodash');

// Push a new query onto the compiled "sequence" stack,
// creating a new formatter, returning the compiler.
function pushQuery(method, query) {
  if (!query) return;
  query = { method: method, ast: query };
  if (!query.bindings) {
    query.bindings = {};
  }
  this.sequence.push(query);
}

// Used in cases where we need to push some additional column specific statements.
function pushAdditional(fn) {
  var child = new this.constructor(this.tree, this.classCompiler, this.memberBuilder);
  fn.call(child, _.tail(arguments));
  this.sequence.additional = (this.sequence.additional || []).concat(child.sequence);
}

function syntaxLiteral(value) {
  if (!_.isNumber(value)) {
    return { 'f:LiteralExpression': [{ 'k:StringLiteralExpression': null }, { 'f:Literal': [value] }] };
  }
  return { 'f:LiteralExpression': [{ 'k:NumericLiteralExpression': null }, { 'f:Literal': [value] }] };
}

function syntaxList(type, array) {
  //if (array.length == 0) return {};
  if (!_.isArray(array) || array.length === 1) {
    var obj = {}; obj['f:SingletonList<' + type + '>'] = (_.isArray(array) ? array : [array]);
    return obj;
  }
  var obj2 = {}; obj2['n:' + type] = array;
  var obj = {}; obj['f:List<' + type + '>'] = [obj2];
  return obj;
}

function syntaxSeperatedList(type, array) {
  //if (array.length == 0) return {};
  if (!_.isArray(array) || array.length === 1) {
    var obj = {}; obj['f:SingletonSeparatedList<' + type + '>'] = (_.isArray(array) ? array : [array]);
    return obj;
  }
  var array2 = [array[0]];
  for (var i = 1, l = array.length; i < l; i++) {
    array2.push({ 'f:Token': ['k:CommaToken'] });
    array2.push(array[i]);
  }
  var obj2 = { 'n:SyntaxNodeOrToken': array2 };
  var obj = {}; obj['f:SeparatedList<' + type + '>'] = [obj2];
  return obj;
}
