var assert = require('assert');
var valueFactory = require('../../lib/factory/value.js');

describe('valueFactory', function () {
  it('generates a value AST (string)', function () {
    var value = valueFactory.create('"a"');
    assert(value.hasOwnProperty('f:LiteralExpression'));
    assert(value['f:LiteralExpression'][0].hasOwnProperty('k:StringLiteralExpression'));
    assert.equal(value['f:LiteralExpression'][1]['f:Literal'], 'a');
  });
});
