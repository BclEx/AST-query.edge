'use strict';

var helpers = require('./helpers');
var _ = require('lodash');

module.exports = function (Target) {

  // Create a new instance of the `Runner`, passing in the current object.
  Target.prototype.then = function () /* onFulfilled, onRejected */ {
    // var result = this.tree.runner(this).run();
    // return result.then.apply(result, arguments);
    return null;
  };

  // Add additional "options" to the builder.
  Target.prototype.options = function (opts) {
    this.options = this.options || [];
    this.options.push(_.clone(opts) || {});
    return this;
  };

  // Set a debug flag for the current schema query stack.
  Target.prototype.debug = function (enabled) {
    this.debug = arguments.length ? enabled : true;
    return this;
  };

  // Creates a method which "coerces" to a promise, by calling a
  // "then" method on the current `Target`
  _.each(['bind', 'catch', 'finally', 'asCallback', 'spread', 'map', 'reduce', 'tap', 'thenReturn', 'return', 'yield', 'ensure', 'exec', 'reflect'
  ], function (method) {
    Target.prototype[method] = function () {
      var then = this.then();
      then = then[method].apply(then, arguments);
      return then;
    };
  });
};


