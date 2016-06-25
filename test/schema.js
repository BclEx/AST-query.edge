'use strict';

var assert = require('assert');
var program = require('..');

var tree = program('', null, null);
var tree2 = program('\
class User\n\
{\n\
    public string Field { get; set; }\n\
}', null, null);

describe("SchemaBuilder", function () {

  var classAst;
  it('test basic create class with string', function () {
    classAst = tree.schemaBuilder().createClass('User', function (c) {
      c.string('Field');
    }).toAST();

    assert.equal(1, classAst.length);
    assert.equal(classAst[0].method, 'cunit.add');
    assert.deepEqual(classAst[0].ast, {
      "f:ClassDeclaration": ["User"], "b": [{ "w:Members": [{ "f:SingletonList<MemberDeclarationSyntax>": [{ "f:PropertyDeclaration": [{ "f:PredefinedType": [{ "f:Token": ["k:StringKeyword"] }] }, { "f:Identifier": ["Field"] }], "b": [{ "w:Modifiers": [{ "f:TokenList": [{ "f:Token": ["k:PublicKeyword"] }] }] }, { "w:AccessorList": [{ "f:AccessorList": [{ "f:List<AccessorDeclarationSyntax>": [{ "n:AccessorDeclarationSyntax": [{ "f:AccessorDeclaration": [{ "k:GetAccessorDeclaration": null }], "b": [{ "w:SemicolonToken": [{ "f:Token": ["k:SemicolonToken"] }] }] }, { "f:AccessorDeclaration": [{ "k:SetAccessorDeclaration": null }], "b": [{ "w:SemicolonToken": [{ "f:Token": ["k:SemicolonToken"] }] }] }] }] }], "b": [] }] }] }] }] }]
    });
    tree.alter(classAst);
    assert.equal(tree.toString(), '\
class User\n\
{\n\
    public string Field { get; set; }\n\
}');
  });

  it('test basic create class with schema and field', function () {
    classAst = tree.schemaBuilder().withSchema('Schema').createClass('User', function (c) {
      c.string('Field');
    }).toAST();

    assert.equal(1, classAst.length);
    assert.equal(classAst[0].method, 'cunit.add');
    assert.deepEqual(classAst[0].ast, {
      "f:NamespaceDeclaration": [{ "f:IdentifierName": ["Schema"] }], "b": [{ "w:Members": [{ "f:SingletonList<MemberDeclarationSyntax>": [{ "f:ClassDeclaration": ["User"], "b": [{ "w:Members": [{ "f:SingletonList<MemberDeclarationSyntax>": [{ "f:PropertyDeclaration": [{ "f:PredefinedType": [{ "f:Token": ["k:StringKeyword"] }] }, { "f:Identifier": ["Field"] }], "b": [{ "w:Modifiers": [{ "f:TokenList": [{ "f:Token": ["k:PublicKeyword"] }] }] }, { "w:AccessorList": [{ "f:AccessorList": [{ "f:List<AccessorDeclarationSyntax>": [{ "n:AccessorDeclarationSyntax": [{ "f:AccessorDeclaration": [{ "k:GetAccessorDeclaration": null }], "b": [{ "w:SemicolonToken": [{ "f:Token": ["k:SemicolonToken"] }] }] }, { "f:AccessorDeclaration": [{ "k:SetAccessorDeclaration": null }], "b": [{ "w:SemicolonToken": [{ "f:Token": ["k:SemicolonToken"] }] }] }] }] }], "b": [] }] }] }] }] }] }] }] }]
    });
    tree.alter(classAst);
    assert.equal(tree.toString(), '\
namespace Schema\n\
{\n\
    class User\n\
    {\n\
        public string Field { get; set; }\n\
    }\n\
}');
  });

  it('test basic create class with field and attribute', function () {
    classAst = tree.schemaBuilder().createClass('User', function (c) {
      c.string('Field').attribute({ DisplayName: ['Name'] });
    }).toAST();

    assert.equal(1, classAst.length);
    assert.equal(classAst[0].method, 'cunit.add');
    // assert.deepEqual(classAst[0].ast, {});
    tree.alter(classAst);
    assert.equal(tree.toString(), '\
class User\n\
{\n\
    [DisplayName("name")]\n\
    public string Field { get; set; }\n\
}');
  });

  it('test drop class', function () {
    classAst = tree2.schemaBuilder().dropClass('User').toAST();

    assert.equal(1, classAst.length);
    assert.equal(classAst[0].method, 'class.remove');
    assert.deepEqual(classAst[0].ast, { User: null });
    tree2.alter(classAst);
    assert.equal(tree2.toString(), null);
  });

  it('test drop table if exists', function () {
    classAst = tree2.schemaBuilder().dropClassIfExists('User').toAST();

    assert.equal(1, classAst.length);
    assert.equal(classAst[0].method, 'class.remove');
    assert.deepEqual(classAst[0].ast, { User: null });
    tree2.alter(classAst);
    assert.equal(tree2.toString(), null);
  });

  it('test drop member', function () {
    classAst = tree.schemaBuilder().class('User', function () {
      this.dropMember('Foo');
    }).toAST();

    assert.equal(1, classAst.length);
    assert.equal(classAst[0].method, 'class.dropMember');
    assert.deepEqual(classAst[0].ast, { 'User': ['Foo'] });
  });

  it('drops multiple members with an array', function () {
    classAst = tree.schemaBuilder().class('User', function () {
      this.dropMember(['Foo', 'Bar']);
    }).toAST();

    assert.equal(1, classAst.length);
    assert.equal(classAst[0].method, 'class.dropMember');
    assert.deepEqual(classAst[0].ast, { 'User': ['Foo', 'Bar'] });
  });

  it('drops multiple columns as multiple arguments', function () {
    classAst = tree.schemaBuilder().class('User', function () {
      this.dropMember('Foo', 'Bar');
    }).toAST();

    assert.equal(1, classAst.length);
    assert.equal(classAst[0].method, 'class.dropMember');
    assert.deepEqual(classAst[0].ast, { 'User': ['Foo', 'Bar'] });
  });




  // it('test drop Method', function () {
  //   classAst = tree.schemaBuilder().class('User', function () {
  //     this.dropMethod('foo');
  //   }).toAST();

  //   assert.equal(1, classAst.length);
  //   assert.equal(classAst[0].method, 'class.dropMember');
  //   assert.deepEqual(classAst[0].ast, {});
  // });

  it('test rename class', function () {
    classAst = tree2.schemaBuilder().renameClass('User', 'Foo').toAST();

    assert.equal(1, classAst.length);
    assert.equal(classAst[0].method, 'class.rename');
    assert.deepEqual(classAst[0].ast, { User: ['Foo'] });
    tree2.alter(classAst);
    assert.equal(tree2.toString(), '\
class Foo\n\
{\n\
    public string Field { get; set; }\n\
}');
  });







  // it('test adding unique key', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.unique('foo', 'bar');
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('CREATE UNIQUE INDEX [bar] ON [users] ([foo])');
  // });

  // it('test adding index', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.index(['foo', 'bar'], 'baz');
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('CREATE INDEX [baz] ON [users] ([foo], [bar])');
  // });

  // it('test adding foreign key', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.foreign('foo_id').references('id').on('orders');
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD CONSTRAINT [users_foo_id_foreign] FOREIGN KEY ([foo_id]) REFERENCES [orders] ([id])');
  // });

  // it("adds foreign key with onUpdate and onDelete", function() {
  //   tableSql = client.schemaBuilder().createTable('person', function(table) {
  //     table.integer('user_id').notNull().references('users.id').onDelete('SET NULL');
  //     table.integer('account_id').notNull().references('id').inTable('accounts').onUpdate('cascade');
  //   }).toSQL();
  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('CREATE TABLE [person] ([user_id] int not null, [account_id] int not null, CONSTRAINT [person_user_id_foreign] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE SET NULL, CONSTRAINT [person_account_id_foreign] FOREIGN KEY ([account_id]) REFERENCES [accounts] ([id]) ON UPDATE cascade)');
  // });

  // it('test adding incrementing id', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.increments('id');
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [id] int identity(1,1) not null primary key');
  // });

  // it('test adding big incrementing id', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.bigIncrements('id');
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [id] bigint identity(1,1) not null primary key');
  // });

  // it('test adding column after another column', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.string('name').after('foo');
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [name] nvarchar(255) after [foo]');
  // });

  // it('test adding column on the first place', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.string('first_name').first();
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [first_name] nvarchar(255) first');
  // });

  // it('test adding string', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.string('foo');
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [foo] nvarchar(255)');
  // });

  // it('uses the varchar column constraint', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.string('foo', 100);
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [foo] nvarchar(100)');
  // });

  // it('chains notNull and defaultTo', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.string('foo', 100).notNull().defaultTo('bar');
  //   }).toSQL();
  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [foo] nvarchar(100) not null default \'bar\'');
  // });

  // it('allows for raw values in the default field', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.string('foo', 100).nullable().defaultTo(client.raw('CURRENT TIMESTAMP'));
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [foo] nvarchar(100) null default CURRENT TIMESTAMP');
  // });

  // it('test adding text', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.text('foo');
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [foo] nvarchar(max)');
  // });

  // it('test adding big integer', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.bigInteger('foo');
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [foo] bigint');
  // });

  // it('test adding integer', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.integer('foo');
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [foo] int');
  // });

  // it('test adding medium integer', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.mediumint('foo');
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [foo] mediumint');
  // });

  // it('test adding small integer', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.smallint('foo');
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [foo] smallint');
  // });

  // it('test adding tiny integer', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.tinyint('foo');
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [foo] tinyint');
  // });

  // it('test adding float', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.float('foo', 5, 2);
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [foo] float(5, 2)');
  // });

  // it('test adding double', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.double('foo');
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [foo] double');
  // });

  // it('test adding double specifying precision', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.double('foo', 15, 8);
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [foo] double(15, 8)');
  // });

  // it('test adding decimal', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.decimal('foo', 5, 2);
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [foo] decimal(5, 2)');
  // });

  // it('test adding boolean', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.boolean('foo');
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [foo] bit');
  // });

  // it('test adding enum', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.enum('foo', ['bar', 'baz']);
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [foo] nvarchar(100)');
  // });

  // it('test adding date', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.date('foo');
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [foo] date');
  // });

  // it('test adding date time', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.dateTime('foo');
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [foo] datetime');
  // });

  // it('test adding time', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.time('foo');
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [foo] time');
  // });

  // it('test adding time stamp', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.timestamp('foo');
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [foo] datetime');
  // });

  // it('test adding time stamps', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.timestamps();
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [created_at] datetime, [updated_at] datetime');
  // });

  // it('test adding binary', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.binary('foo');
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [foo] blob');
  // });

  // it('test adding decimal', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.decimal('foo', 2, 6);
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [foo] decimal(2, 6)');
  // });

  // it('test adding multiple columns, #1348', function() {
  //   tableSql = client.schemaBuilder().table('users', function() {
  //     this.integer('foo');
  //     this.integer('baa');
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD [foo] int, [baa] int');
  // });

  // it('is possible to set raw statements in defaultTo, #146', function() {
  //   tableSql = client.schemaBuilder().createTable('default_raw_test', function(t) {
  //     t.timestamp('created_at').defaultTo(client.raw('GETDATE()'));
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('CREATE TABLE [default_raw_test] ([created_at] datetime default GETDATE())');
  // });

  // it('allows dropping a unique compound index', function() {
  //   tableSql = client.schemaBuilder().table('composite_key_test', function(t) {
  //     t.dropUnique(['column_a', 'column_b']);
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [composite_key_test] DROP CONSTRAINT [composite_key_test_column_a_column_b_unique]');
  // });

  // it('allows default as alias for defaultTo', function() {
  //   tableSql = client.schemaBuilder().createTable('default_raw_test', function(t) {
  //     t.timestamp('created_at').default(client.raw('GETDATE()'));
  //   }).toSQL();

  //   equal(1, tableSql.length);
  //   expect(tableSql[0].sql).to.equal('CREATE TABLE [default_raw_test] ([created_at] datetime default GETDATE())');
  // });


  // it('#1430 - .primary & .dropPrimary takes columns and constraintName', function() {
  //   tableSql = client.schemaBuilder().table('users', function(t) {
  //     t.primary(['test1', 'test2'], 'testconstraintname');
  //   }).toSQL();
  //   expect(tableSql[0].sql).to.equal('ALTER TABLE [users] ADD CONSTRAINT [testconstraintname] PRIMARY KEY ([test1], [test2])');

  //   tableSql = client.schemaBuilder().createTable('users', function(t) {
  //     t.string('test').primary('testconstraintname');
  //   }).toSQL();

  //   expect(tableSql[0].sql).to.equal('CREATE TABLE [users] ([test] nvarchar(255), CONSTRAINT [testconstraintname] PRIMARY KEY ([test]))');
  // });

});
