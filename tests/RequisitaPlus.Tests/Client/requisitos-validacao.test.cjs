const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const { test } = require('node:test');

let validate;
let adapter;
const context = {
    $: { validator: {
        addMethod: (name, method) => { assert.equal(name, 'naobranco'); validate = method; },
        unobtrusive: { adapters: { addBool: name => { adapter = name; } } }
    } }
};
vm.runInNewContext(fs.readFileSync(path.join(__dirname,
    '../../../src/RequisitaPlus.Web/wwwroot/js/requisitos-validacao.js'), 'utf8'), context);

test('registers the unobtrusive adapter', () => assert.equal(adapter, 'naobranco'));
for (const value of ['', ' ', '\t\n', '\u0085\u00a0\u3000']) {
    test(`rejects blank text ${JSON.stringify(value)}`, () => assert.equal(validate(value), false));
}
for (const value of ['Título', ' descrição  ', '😀', '\ufeff']) {
    test(`accepts nonblank text ${JSON.stringify(value)}`, () => assert.equal(validate(value), true));
}
