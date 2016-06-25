'use strict';

var helpers1 = require('./helpers');
var helpers = require('../helpers');
var _ = require('lodash');


// Class Compiler
// -------

function ClassCompiler(tree, classBuilder) {
  this.tree = tree;
  this.method = classBuilder.method;
  this.schemaName = classBuilder.schemaName;
  this.classNameRaw = classBuilder.className;
  this.single = classBuilder.single;
  this.grouped = _.groupBy(classBuilder.statements, 'grouping');
  this.sequence = [];

  this.createClass = function createClass(members, ifNot) {
    if (this.single.comment) {
      var comment = this.single.comment || '';
      if (comment.length > 60) helpers.warn('The max length for a class comment is 60 characters');
    }
    // ifNot ? 'if object_id(\'' + this.tableName() + '\', \'U\') is null CREATE TABLE ' : 'CREATE TABLE '
    var ast = {
      'f:ClassDeclaration': [this.className()], b: [{
        'w:Members': [helpers1.syntaxList('MemberDeclarationSyntax', members.ast)]
      }]
    };
    if (this.schemaName) {
      ast = {
        'f:NamespaceDeclaration': [{ 'f:IdentifierName': [this.schemaName] }], b: [{
          'w:Members': [helpers1.syntaxList('MemberDeclarationSyntax', ast)]
        }]
      };
    }
    this.pushQuery('cunit.add', ast);
  };
}

ClassCompiler.prototype.pushQuery = helpers1.pushQuery;

ClassCompiler.prototype.pushAdditional = helpers1.pushAdditional;

// Convert the calssCompiler toAST
ClassCompiler.prototype.toAST = function () {
  this[this.method]();
  return this.sequence;
};

// Member Compilation
// -------

// If this is a table "creation", we need to first run through all
// of the columns to build them into a single string,
// and then run through anything else and push it to the query sequence.
ClassCompiler.prototype.create = function (ifNot) {
  var members = this.getMembers();
  var memberTypes = this.getMemberTypes(members);
  if (this.createAlterClassMethods) {
    this.alterClassForCreate(memberTypes);
  }
  this.createClass(memberTypes, ifNot);
  this.memberQueries(members);
  delete this.single.comment;
  this.alterClass();
};

// Only create the table if it doesn't exist.
ClassCompiler.prototype.createIfNot = function () {
  this.create(true);
};

// If we're altering the table, we need to one-by-one
// go through and handle each of the queries associated
// with altering the table's schema.
ClassCompiler.prototype.alter = function () {
  var members = this.getMembers();
  var memberTypes = this.getMemberTypes(members);
  this.addMembers(memberTypes);
  this.memberQueries(members);
  this.alterClass();
};

// Get all of the member sql & bindings individually for building the member ast.
ClassCompiler.prototype.getMemberTypes = function (members) {
  return _.reduce(_.map(members, _.first), function (memo, member) {
    memo.ast.push(member.ast);
    memo.bindings.concat(member.bindings);
    return memo;
  }, { ast: [], bindings: [] });
};

// Adds all of the additional queries from the "member"
ClassCompiler.prototype.memberQueries = function (members) {
  var queries = _.reduce(_.map(members, _.tail), function (memo, member) {
    if (!_.isEmpty(member)) return memo.concat(member);
    return memo;
  }, []);
  for (var i = 0, l = queries.length; i < l; i++) {
    this.pushQuery('member', queries[i]);
  }
};

// All of the columns to "add" for the query
ClassCompiler.prototype.addMembers = function (members) {
  var self = this;
  if (members.ast.length > 0) {
    var memberAST = _.map(members.ast, function (member) {
      return member;
    });
    console.log(memberAST);
    this.pushQuery('alterClass', {
      ast: 'alter table ' + this.className() + ' ' + memberAST.join(', '),
      bindings: members.bindings
    });
  }
};

// Compile the members as needed for the current create or alter class
ClassCompiler.prototype.getMembers = function () {
  var self = this;
  var members = this.grouped.members || [];
  return members.map(function (member) {
    return self.tree.memberCompiler(self, member.builder).toAST();
  });
};

ClassCompiler.prototype.className = function () {
  return this.classNameRaw;
};

// Generate all of the alter class statements necessary for the query.
ClassCompiler.prototype.alterClass = function () {
  var alterClass = this.grouped.alterClass || [];
  for (var i = 0, l = alterClass.length; i < l; i++) {
    var statement = alterClass[i];
    if (this[statement.method]) {
      this[statement.method].apply(this, statement.args);
    } else {
      helpers.debug('Debug: ' + statement.method + ' does not exist');
    }
  }
  for (var item in this.single) {
    if (typeof this[item] === 'function') this[item](this.single[item]);
  }
};

ClassCompiler.prototype.alterClassForCreate = function (memberTypes) {
  this.forCreate = true;
  var savedSequence = this.sequence;
  var alterClass = this.grouped.alterClass || [];
  this.grouped.alterClass = [];
  for (var i = 0, l = alterClass.length; i < l; i++) {
    var statement = alterClass[i];
    if (_.indexOf(this.createAlterClassMethods, statement.method) < 0) {
      this.grouped.alterClass.push(statement);
      continue;
    }
    if (this[statement.method]) {
      this.sequence = [];
      this[statement.method].apply(this, statement.args);
      memberTypes.ast.push(this.sequence[0].ast);
    } else {
      helpers.error('Debug: ' + statement.method + ' does not exist');
    }
  }
  this.sequence = savedSequence;
  this.forCreate = false;
};

ClassCompiler.prototype.dropMember = function () {
  //var self = this;
  var members = helpers.normalizeArr.apply(null, arguments);
  var drops = _.map(_.isArray(members) ? members : [members], function (member) {
    return member;
  });
  var node = {}; node[this.className()] = drops;
  this.pushQuery('class.dropMember', node);
};

module.exports = ClassCompiler;