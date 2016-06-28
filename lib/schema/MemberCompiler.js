'use strict';

var helpers1 = require('./helpers');
var helpers = require('../helpers');
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
  this.modifiers = ['defaultTo', 'attribute', 'array', 'comment'];
  this.sequence = [];
}

MemberCompiler.prototype.pushQuery = helpers1.pushQuery;

MemberCompiler.prototype.pushAdditional = helpers1.pushAdditional;

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
  var memberName = this.getMemberName();
  var ast;
  if (memberName[0] === '_') {
    ast = {
      'f:FieldDeclaration': [{
        'f:VariableDeclaration': [this.getMemberType()], b: [
          { 'w:Variables': [{ 'f:SingletonSeparatedList<VariableDeclaratorSyntax>': [{ 'f:VariableDeclarator': [{ 'f:Identifier': [memberName] }] }] }] }] // jshint ignore:line
      }], b: [
        { 'w:Modifiers': [{ 'f:TokenList': [{ 'f:Token': ['k:PublicKeyword'] }] }] },
      ]
    };
  }
  else {
    var accessors = [];
    accessors.push({ 'f:AccessorDeclaration': [{ 'k:GetAccessorDeclaration': null }], b: [{ 'w:SemicolonToken': [{ 'f:Token': ['k:SemicolonToken'] }] }] }); // jshint ignore:line
    accessors.push({ 'f:AccessorDeclaration': [{ 'k:SetAccessorDeclaration': null }], b: [{ 'w:SemicolonToken': [{ 'f:Token': ['k:SemicolonToken'] }] }] }); // jshint ignore:line
    ast = {
      'f:PropertyDeclaration': [this.getMemberType(), { 'f:Identifier': [memberName] }], b: [
        { 'w:Modifiers': [{ 'f:TokenList': [{ 'f:Token': ['k:PublicKeyword'] }] }] },
        {
          'w:AccessorList': [{
            'f:AccessorList': [helpers1.syntaxList('AccessorDeclarationSyntax', accessors)], b: []
          }]
        }
      ]
    };
  }
  var modifiers = this.getModifiers();
  if (modifiers.length) {
    ast.b = modifiers.concat(ast.b);
  }
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
  return modifiers;
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
MemberCompiler.prototype.datetime = { 'f:IdentifierName': ['DateTime'] };
MemberCompiler.prototype.guid = { 'f:IdentifierName': ['Guid'] };
MemberCompiler.prototype.x = function (type) {
  // single type
  return { 'f:IdentifierName': [type] };
};

// Modifiers
// -------

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

MemberCompiler.prototype.attribute = function () {
  var args = helpers.normalizeArr.apply(null, arguments);
  var attrs = _.map(_.isArray(args) ? args : [args], function (arg) {
    var key = Object.keys(arg)[0];
    var values = _.isArray(arg[key]) ? arg[key] : [arg[key]];
    //console.log(key, values);
    var b = [];
    var attrAst = { 'f:Attribute': [{ 'f:IdentifierName': [key] }] };
    if (values.length === 1 && values[0] !== null) {
      attrAst.b = [{
        'w:ArgumentList': [{
          'f:AttributeArgumentList': [{
            'f:SingletonSeparatedList<AttributeArgumentSyntax>': [{
              'f:AttributeArgument': [helpers1.syntaxLiteral(values[0])]
            }]
          }], b: []
        }]
      }];
    }
    return attrAst;
  });
  var ast = {
    'w:AttributeLists': [{
      'f:SingletonList<AttributeListSyntax>': [{
        'f:AttributeList': [helpers1.syntaxSeperatedList('AttributeSyntax', attrs)], b: []
      }]
    }]
  };
  return ast;
};

MemberCompiler.prototype.array = function (length) {
  return length ? '[' + length + ']' : '[]';
};

MemberCompiler.prototype.comment = function (comment) {
  if (comment && comment.length > 255) {
    helpers.warn('Your comment is longer than the max comment length');
  }
  return '';
};

module.exports = MemberCompiler;
