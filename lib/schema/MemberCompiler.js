'use strict';

var helpers = require('./helpers');
var _ = require('lodash');

// Member Compiler
// Used for designating member definitions
// during the class "create" / "alter" statements.
// -------
function MemberCompiler(tree, classCompiler, memberBuilder) {
  this.tree = tree;
  this.classCompiler = classCompiler;
  this.memberBuilder = memberBuilder;
  this.args = memberBuilder.args;
  this.type = memberBuilder.type.toLowerCase();
  this.grouped = _.groupBy(memberBuilder.statements, 'grouping');
  this.modified = memberBuilder.modifiers;
  this.modifiers = ['array', 'comment'];
  this.sequence = [];
}

MemberCompiler.prototype.pushQuery = helpers.pushQuery;

MemberCompiler.prototype.pushAdditional = helpers.pushAdditional;

// To convert to ast, we first go through and build the member as it would be in the insert statement
MemberCompiler.prototype.toAST = function () {
  this.pushQuery('?', this.compileMember());
  if (this.sequence.additional) {
    this.sequence = this.sequence.concat(this.sequence.additional);
  }
  return this.sequence;
};

// Compiles a member.
MemberCompiler.prototype.compileMember = function () {
  //this.getModifiers();
  var accessors = [];
  accessors.push({ "f:AccessorDeclaration": [{ "k:GetAccessorDeclaration": null }], b: [{ "w:SemicolonToken": [{ "f:Token": ["k:SemicolonToken"] }] }] });
  accessors.push({ "f:AccessorDeclaration": [{ "k:SetAccessorDeclaration": null }], b: [{ "w:SemicolonToken": [{ "f:Token": ["k:SemicolonToken"] }] }] });
  helpers.syntaxList('AccessorDeclarationSyntax', accessors)
  var ast = {
    'f:PropertyDeclaration': [this.getMemberType(), { 'f:Identifier': [this.getMemberName()] }], b: [
      { "w:Modifiers": [{ "f:TokenList": [{ "f:Token": ["k:PublicKeyword"] }] }] },
      {
        "w:AccessorList": [{
          "f:AccessorList": [helpers.syntaxList('AccessorDeclarationSyntax', accessors)], b: []
        }]
      }
    ]
  };
  return ast;
};

MemberCompiler.prototype.getMemberName = function () {
  var value = _.first(this.args);
  if (value) return value;
  throw new Error('You did not specify a member name for the ' + this.type + ' member.');
};

MemberCompiler.prototype.getMemberType = function () {
  var type = this[this.type];
  return typeof type === 'function' ? type.apply(this, _.tail(this.args)) : type;
};

MemberCompiler.prototype.getModifiers = function () {
  var modifiers = [];
  for (var i = 0, l = this.modifiers.length; i < l; i++) {
    var modifier = this.modifiers[i];
    if (_.has(this.modified, modifier)) {
      var val = this[modifier].apply(this, this.modified[modifier]);
      if (val) modifiers.push(val);
    }
  }
  return modifiers.length > 0 ? ' ' + modifiers.join(' ') : '';
};

// Numeric
MemberCompiler.prototype.byte = { 'f:PredefinedType': [{ 'f:Token': ['k:ByteKeyword'] }] };
MemberCompiler.prototype.sbyte = { 'f:PredefinedType': [{ 'f:Token': ['k:SByteKeyword'] }] };
MemberCompiler.prototype.short = { 'f:PredefinedType': [{ 'f:Token': ['k:ShortKeyword'] }] };
MemberCompiler.prototype.ushort = { 'f:PredefinedType': [{ 'f:Token': ['k:UShortKeyword'] }] };
MemberCompiler.prototype.int = { 'f:PredefinedType': [{ 'f:Token': ['k:IntKeyword'] }] };
MemberCompiler.prototype.uint = { 'f:PredefinedType': [{ 'f:Token': ['k:UIntKeyword'] }] };
MemberCompiler.prototype.long = { 'f:PredefinedType': [{ 'f:Token': ['k:LongKeyword'] }] };
MemberCompiler.prototype.ulong = { 'f:PredefinedType': [{ 'f:Token': ['k:ULongKeyword'] }] };
MemberCompiler.prototype.single = { 'f:PredefinedType': [{ 'f:Token': ['k:SingleKeyword'] }] };
MemberCompiler.prototype.float = { 'f:PredefinedType': [{ 'f:Token': ['k:FloatKeyword'] }] };
MemberCompiler.prototype.decimal = { 'f:PredefinedType': [{ 'f:Token': ['k:DecimalKeyword'] }] };
// String
MemberCompiler.prototype.char = { 'f:PredefinedType': [{ 'f:Token': ['k:CharKeyword'] }] };
MemberCompiler.prototype.string = { 'f:PredefinedType': [{ 'f:Token': ['k:StringKeyword'] }] };
// Additional
MemberCompiler.prototype.void = { 'f:PredefinedType': [{ 'f:Token': ['k:VoidKeyword'] }] };
MemberCompiler.prototype.bool = { 'f:PredefinedType': [{ 'f:Token': ['k:BoolKeyword'] }] };
MemberCompiler.prototype.dateTime = { 'f:IdentifierName': ['DateTime'] };
MemberCompiler.prototype.x = function (type) { return { 'f:IdentifierName': [type] }; };

// Modifiers
// -------

MemberCompiler.prototype.array = function (length) {
  return length ? '[' + length + ']' : '[]';
};
MemberCompiler.prototype.defaultTo = function (value) {
  if (value === void 0) {
    return '';
  } else if (value === null) {
    value = '"null';
  } else if (this.type === 'bool') {
    if (value === 'false') value = 0;
    value = '\'' + (value ? 1 : 0) + '\'';
  } else if (this.type === 'json' && _.isObject(value)) {
    return JSON.stringify(value);
  } else {
    value = '\'' + value + '\'';
  }
  return 'default ' + value;
};

module.exports = MemberCompiler;
