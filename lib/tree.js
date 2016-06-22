'use strict';
var parser = require('./roslyn/parser.js');
var render = require('./roslyn/render.js');
var traverse = require('traverse');
var _ = require('lodash');
var utils = require('./util/utils');
var Variable = require('./nodes/Variable');
var CallExpression = require('./nodes/CallExpression');
var AssignmentExpression = require('./nodes/AssignmentExpression');
var Body = require('./nodes/Body');

// schema
var ClassBuilder = require('./schema/ClassBuilder');
var ClassCompiler = require('./schema/ClassCompiler');
var MemberBuilder = require('./schema/MemberBuilder');
var MemberCompiler = require('./schema/MemberCompiler');
var SchemaBuilder = require('./schema/SchemaBuilder');
var SchemaCompiler = require('./schema/SchemaCompiler');
var Raw = require('./schema/Raw');

var renderOptionDefaults = {
};

var parserOptionDefaults = {
};

function Tree(source, renderOptions, parserOptions) {
  this.parserOptions = _.extend({}, parserOptionDefaults, parserOptions);
  this.tree = parser.parse(source.toString(), this.parserOptions);
  this.body = new Body(this.tree.body, this.parserOptions);
  this.renderOptions = _.extend({}, renderOptionDefaults, renderOptions);
  this.alter = function alter(ast) {
    for (var i = 0, l = ast.length; i < l; i++) {
      this.tree.alters.push(ast[i]);
    }
  };
  // schema
  this.schemaBuilder = function schemaBuilder() {
    return new SchemaBuilder(this);
  };
  this.schemaCompiler = function schemaCompiler(builder) {
    return new SchemaCompiler(this, builder);
  };
  this.classBuilder = function classBuilder(type, className, fn) {
    return new ClassBuilder(this, type, className, fn);
  };
  this.classCompiler = function tableCompiler(classBuilder) {
    return new ClassCompiler(this, classBuilder);
  };
  this.memberBuilder = function columnBuilder(classBuilder, type, args) {
    return new MemberBuilder(this, classBuilder, type, args);
  };
  this.memberCompiler = function columnCompiler(classBuilder, memberBuilder) {
    return new MemberCompiler(this, classBuilder, memberBuilder);
  };
  // Run a "raw" query, though we can't do anything with it other than put
  // it in a query statement.
  this.raw = function (ast, bindings) {
    return new Raw(this).set(ast, bindings);
  };
}

/**
 * Return the regenerated code string
 * @return {String} outputted code
 */
Tree.prototype.toString = function () {
  // Filter the three to remove temporary placeholders
  var tree = traverse(this.tree.root).map(function (node) {
    if (node && node.TEMP === true) {
      this.remove();
    }
  });
  var alters = this.tree.alters;
  this.tree.alters = [];
  return render.generate(tree, alters, this.renderOptions);
};

/**
 * Find variables declaration
 * @param  {String|RegExp} name  Name of the declared variable
 * @return {Variable}
 */
Tree.prototype.var = function (name) {
  var nodes = traverse(this.tree.body).nodes().filter(function (node) {
    if (node && node['f:VariableDeclarator'] && utils.match(name, node['f:VariableDeclarator'][0]['f:Identifier'][0])) {
      return true;
    }
  });
  return new Variable(nodes);
};

/**
 * Select function/method calls
 * @param  {String|RegExp} name Name of the called function (`foo`, `foo.bar`)
 * @return {CallExpression}
 */
Tree.prototype.callExpression = function callExpression(name) {
  var nodes = traverse(this.tree.body).nodes().filter(function (node) {
    if (!node || node.type !== 'CallExpression') return false;

    // Simple function call
    if (node.callee.type === 'Identifier' && utils.match(name, node.callee.name)) return true;

    // Method call
    if (utils.matchMemberExpression(name, node.callee)) return true;
  });
  return new CallExpression(nodes);
};

/**
 * Select an AssignmentExpression node
 * @param  {String|RegExp} assignedTo Name of assignment left handside
 * @return {AssignmentExpression} Matched node
 */
Tree.prototype.assignment = function (assignedTo) {
  var nodes = traverse(this.tree).nodes().filter(function (node) {
    if (!node || node.type !== 'AssignmentExpression') return false;

    // Simple assignment
    if (node.left.type === 'Identifier' && utils.match(assignedTo, node.left.name)) return true;

    // Assignment to an object key
    if (utils.matchMemberExpression(assignedTo, node.left)) return true;
  });
  return new AssignmentExpression(nodes);
};

module.exports = function (source, renderOptions, parserOptions) {
  return new Tree(source, renderOptions, parserOptions);
};
