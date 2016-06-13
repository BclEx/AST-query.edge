'use strict';
var _ = require('lodash');
var valueFactory = require('../factory/value.js');
var Base = require('./Base');

/**
 * Variable node object abstraction
 * @constructor
 * @param  {Array(object)} nodes
 */
var Variable = module.exports = Base.extend({

  /**
   * Change or get the variable value
   *
   * @param  {String} value  New value string
   * @return {Object}        Wrapped value
   *
   * @or
   * @return {Object}        Wrapped value
   */
  value: function (val) {
    if (_.isString(val)) {
      this.nodes.forEach(function (node) {
        var equalsNode = node.b[0]['w:Initializer'][0]['f:EqualsValueClause'];
        if (equalsNode) {
          equalsNode[0] = valueFactory.create(val);
        }
      });
    }
    var equalsNode = this.nodes[0].b[0]['w:Initializer'][0]['f:EqualsValueClause'];
    if (equalsNode) {
      return valueFactory.wrap(equalsNode[0]);
    }
    throw 'node not found';
  },

  /**
   * Rename the variable
   * @param  {string} name  New variable name
   * @return {null}
   */
  rename: function (name) {
    this.nodes.forEach(function (node) {
      var identifierNode = node['f:VariableDeclarator'][0]['f:Identifier'];
      if (identifierNode) {
        identifierNode[0] = name;
      }
    });
    return this;
  }

});
