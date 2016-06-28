'use strict';

var _ = require('lodash');

function ClassBuilder(tree, method, className, fn) {
  this.tree = tree;
  this.fn = fn;
  this.method = method;
  this.schemaName = undefined;
  this.className = className;
  this.statements = [];
  this.single = {};
  if (!_.isFunction(this.fn)) {
    throw new TypeError('A callback function must be supplied to calls against `.createClass` or  `.class`');
  }
}

ClassBuilder.prototype.setSchema = function (schemaName) {
  this.schemaName = schemaName;
};

// Convert the current classBuilder object "toAST" giving us additional methods if we're altering
// rather than creating the class.
ClassBuilder.prototype.toAST = function () {
  if (this.method === 'alter') {
    _.extend(this, AlterMethods);
  }
  this.fn.call(this, this);
  return this.tree.classCompiler(this).toAST();
};

// Each of the class methods can be called individually, with the name to be used, e.g. class.method('name').
var memberMethods = [
  'method', 'dropMethod'];
_.each(memberMethods, function (method) {
  ClassBuilder.prototype[method] = function () {
    this.statements.push({
      grouping: 'alterClass',
      method: method,
      args: _.toArray(arguments)
    });
    return this;
  };
});

// For each of the class members, create a new "MemberBuilder" interface,
// push it onto the "allStatements" stack, and then return the interface, with which we can add indexes, etc.
var memberTypes = [
  // Numeric
  'byte', 'sbyte', 'short', 'ushort', 'int', 'uint', 'long', 'ulong', 'single', 'float', 'decimal',
  // String
  'char', 'string',
  // Additional
  'void', 'bool', 'dateTime', 'guid', 'x'];
_.each(memberTypes, function (type) {
  ClassBuilder.prototype[type] = function () {
    var args = _.toArray(arguments);
    var builder = this.tree.memberBuilder(this, type, args);
    this.statements.push({
      grouping: 'members',
      builder: builder
    });
    return builder;
  };
});

// Set the comment value for a class, they're only allowed to be called once per class.
ClassBuilder.prototype.comment = function (value) {
  this.single.comment = value;
};

var AlterMethods = {
  // Renames the current column `from` the current
  renameMember: function renameMember(from, to) {
    this.statements.push({
      grouping: 'alterClass',
      method: 'renameMember',
      args: [from, to]
    });
    return this;
  },

  dropMember: function dropMember(name) {
    this.statements.push({
      grouping: 'alterClass',
      method: 'dropMember',
      args: _.toArray(arguments)
    });
    return this;
  },
};

module.exports = ClassBuilder;