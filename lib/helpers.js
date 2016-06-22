'use strict';

exports.skim = skim;
exports.normalizeArr = normalizeArr;
exports.debugLog = debugLog;
exports.error = error;
exports.deprecate = deprecate;
exports.warn = warn;
exports.exit = exit;

var _ = require('lodash');
var chalk = require('chalk');

// Pick off the attributes from only the current layer of the object.
function skim(data) {
  return _.map(data, function (obj) {
    return _.pick(obj, _.keys(obj));
  });
}

// Check if the first argument is an array, otherwise uses all arguments as an array.
function normalizeArr() {
  var args = new Array(arguments.length);
  for (var i = 0; i < args.length; i++) {
    args[i] = arguments[i];
  }
  if (Array.isArray(args[0])) {
    return args[0];
  }
  return args;
}

function debugLog(msg) {
  console.log(msg);
}

function error(msg) {
  console.log(chalk.red('AST:Error ' + msg));
}

function deprecate(method, alternate) {
  warn(method + ' is deprecated, please use ' + alternate);
}

function warn(msg) {
  console.log(chalk.yellow('AST:warning - ' + msg));
}

function exit(msg) {
  console.log(chalk.red(msg));
  process.exit(1);
}
