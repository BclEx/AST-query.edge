'use strict';

var _ = require('lodash');

// The chainable interface off the original "member" method.
var memberTypeAlias = {
  'boolean': 'bool',
};
function MemberBuilder(tree, classBuilder, type, args) {
  this.tree = tree;
  this.single = {};
  this.modifiers = {};
  this.statements = [];
  this.type = memberTypeAlias[type] || type;
  this.args = args;
  this.classBuilder = classBuilder;

  // If we're altering the table, extend the object with the available "alter" methods.
  if (classBuilder._method === 'alter') {
    _.extend(this, AlterMethods);
  }
}

// If we call any of the modifiers on the chainable, we pretend as though we're calling `class.method(member)` directly.
var modifierAlias = {
  'default': 'defaultTo',
};
_.each(['defaultTo', 'attribute', 'array', 'comment'], function (method) {
  MemberBuilder.prototype[method] = function () {
    if (modifierAlias[method]) {
      method = modifierAlias[method];
    }
    this.modifiers[method] = _.toArray(arguments);
    return this;
  };
});
_.each(['method', 'dropMethod'], function (method) {
  MemberBuilder.prototype[method] = function () {
    this.classBuilder[method].apply(this.classBuilder, [this.args[0]].concat(_.toArray(arguments)));
    return this;
  };
});

var AlterMethods = {
  // Specify that the column is to be dropped. This takes precedence over all other rules for the column.
  drop: function drop() {
    this.single.drop = true;
    return this;
  },

  // Specify the "type" that we're looking to set
  alterType: function alterType(type) {
    this._statements.push({
      grouping: 'alterType',
      value: type
    });
    return this;
  },
};

module.exports = MemberBuilder;
