'use strict';

var helpers1 = require('./helpers');
var helpers = require('../helpers');
var _ = require('lodash');

function SchemaCompiler(tree, builder) {
  this.tree = tree;
  this.builder = builder;
  this.schema = builder.schema;
  this.sequence = [];
}

_.assign(SchemaCompiler.prototype, {

  pushQuery: helpers1.pushQuery,

  pushAdditional: helpers1.pushAdditional,

  addUsing: buildUsing(),

  createClass: buildClass('create'),

  createClassIfNotExists: buildClass('createIfNot'),

  alterClass: buildClass('alter'),

  dropClass: function dropClass(className) {
    var node = {}; node[className] = null;
    this.pushQuery('class.remove', node);
  },

  dropClassIfExists: function dropTableIfExists(className) {
    var node = {}; node[className] = null;
    this.pushQuery('class.remove', node);
  },

  // Rename a class on the schema.
  renameClass: function renameClass(className, to) {
    var node = {}; node[className] = [to];
    this.pushQuery('class.rename', node);
  },

  raw: function raw(ast, bindings) {
    this.sequence.push(this.tree.raw(ast, bindings).toAST());
  },

  toAST: function toAST() {
    var sequence = this.builder.sequence;
    for (var i = 0, l = sequence.length; i < l; i++) {
      var query = sequence[i];
      this[query.method].apply(this, query.args);
    }
    return this.sequence;
  }
});

function buildUsing() {
  return function () {
    var args = helpers.normalizeArr.apply(null, arguments);
    var usings = _.map(_.isArray(args) ? args : [args], function (arg) {
      var fqdn = helpers1.syntaxFqdn(arg);
      return { 'f:UsingDirective': [fqdn] };
    });
    if (!usings.length) return;
    var ast = helpers1.syntaxList('UsingDirectiveSyntax', usings);
    this.pushQuery('cunit.using', ast);
  };
}

function buildClass(type) {
  return function (name, fn) {
    var builder = this.tree.classBuilder(type, name, fn);
    builder.setSchema(this.schema);
    var ast = builder.toAST();
    for (var i = 0, l = ast.length; i < l; i++) {
      this.sequence.push(ast[i]);
    }
  };
}

module.exports = SchemaCompiler;
