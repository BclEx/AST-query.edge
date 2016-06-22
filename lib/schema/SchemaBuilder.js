'use strict';

var _ = require('lodash');

function SchemaBuilder(tree) {
  this.tree = tree;
  this.sequence = [];
}

// Each of the schema builder methods just add to the "sequence" array for consistency.
_.each(['createClass', 'createClassIfNotExists', 'class', 'alterClass', 'dropClass', 'dropClassIfExists', 'renameClass', 'raw'], function (method) {
  SchemaBuilder.prototype[method] = function () {
    if (method === 'class') method = 'alterClass';
    this.sequence.push({
      method: method,
      args: _.toArray(arguments)
    });
    return this;
  };
});

SchemaBuilder.prototype.withSchema = function (schemaName) {
  this.schema = schemaName;
  return this;
};

SchemaBuilder.prototype.toAST = function () {
  return this.tree.schemaCompiler(this).toAST();
};

module.exports = SchemaBuilder;
