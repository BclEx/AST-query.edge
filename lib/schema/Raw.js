'use strict';

var _ = require('lodash');

// Raw
// -------
function Raw(tree) {
  this.tree = tree;
  this.ast = {};
  this.bindings = [];
  this.wrappedBefore = undefined;
  this.wrappedAfter = undefined;
}

_.assign(Raw.prototype, {

  set: function set(ast, bindings) {
    this.cached = undefined;
    this.ast = ast;
    this.bindings = _.isObject(bindings) || _.isUndefined(bindings) ? bindings : [bindings];
    return this;
  },

  // Wraps the current sql with `before` and `after`.
  wrap: function wrap(before, after) {
    this.cached = undefined;
    this.wrappedBefore = before;
    this.wrappedAfter = after;
    return this;
  },

  // Returns the raw sql for the query.
  toAST: function toAST(method, tz) {
    if (this.cached) return this.cached;
    if (Array.isArray(this.bindings)) {
      this.cached = replaceRawArrBindings(this);
    } else if (this.bindings && _.isPlainObject(this.bindings)) {
      this.cached = replaceKeyBindings(this);
    } else {
      this.cached = {
        method: 'raw',
        ast: this.ast,
        bindings: _.isUndefined(this.bindings) ? void 0 : [this.bindings]
      };
    }
    if (this.wrappedBefore) {
      this.cached.ast = this.wrappedBefore(this.cached.ast);
    }
    if (this.wrappedAfter) {
      this.cached.ast = this.wrappedAfter(this.cached.ast);
    }
    this.cached.options = _.reduce(this.options, _.assign, {});
    if (this.tree && this.tree.prepBindings) {
      this.cached.bindings = this.tree.prepBindings(this.cached.bindings || [], tz);
    }
    return this.cached;
  }

});

function replaceRawArrBindings(raw) {
  var expectedBindings = raw.bindings.length;
  var values = raw.bindings;
  var tree = raw.tree;

  var index = 0; var bindings = [];
  var ast = raw.ast.replace(/\\?\?\??/g, function (match) {
    if (match === '\\?') {
      return match;
    }
    var value = values[index++];
    if (value && typeof value.toAST === 'function') {
      var bindingAST = value.toAST();
      if (bindingAST.bindings !== undefined) {
        bindings = bindings.concat(bindingAST.bindings);
      }
      return bindingAST.ast;
    }
    if (match === '??') {
      return value;
    }
    bindings.push(value);
    return '?';
  });

  if (expectedBindings !== index) {
    throw new Error('Expected ' + expectedBindings + ' bindings, saw ' + index);
  }

  return {
    method: 'raw',
    ast: ast,
    bindings: bindings
  };
}

function replaceKeyBindings(raw) {
  var values = raw.bindings;
  var tree = raw.tree;
  var ast = raw.ast; var bindings = [];

  var regex = new RegExp('(\\:\\w+\\:?)', 'g');
  ast = raw.ast.replace(regex, function (full) {
    var key = full.trim();
    var isIdentifier = key[key.length - 1] === ':';
    var value = isIdentifier ? values[key.slice(1, -1)] : values[key.slice(1)];
    if (value === undefined) {
      return full;
    }
    if (value && typeof value.toAST === 'function') {
      var bindingAST = value.toAST();
      if (bindingAST.bindings !== undefined) {
        bindings = bindings.concat(bindingAST.bindings);
      }
      return full.replace(key, bindingAST.ast);
    }
    if (isIdentifier) {
      return full.replace(key, value);
    }
    bindings.push(value);
    return full.replace(key, '?');
  });

  return {
    method: 'raw',
    ast: ast,
    bindings: bindings
  };
}

// Allow the `Raw` object to be utilized with full access to the relevant
// promise API.
require('./interface')(Raw);

module.exports = Raw;
