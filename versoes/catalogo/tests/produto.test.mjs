import test from 'node:test';
import assert from 'node:assert/strict';
import { interpretarPreco, formatarPreco, lerProduto } from '../DropMonAPI/wwwroot/produto.mjs';

test('preços em reais são interpretados integralmente', () => {
    for (const texto of ['1299,90', '1.299,90', '1299.90', ' 1299,90 '])
        assert.equal(interpretarPreco(texto), 1299.9);
    assert.equal(interpretarPreco('0,01'), 0.01);
    assert.equal(interpretarPreco('999.999,99'), 999999.99);
    assert.equal(interpretarPreco('10'), 10);
    assert.match(formatarPreco(1299.9), /1\.299,90/);
});

test('preços inválidos e ambíguos não são truncados nem arredondados', () => {
    for (const texto of ['', ' ', 'abc', '12abc', '1.299', '1,299.90', '1,999', '-1', '0', 'NaN', 'Infinity', '1e2', '1000000', '1.2.3'])
        assert.throws(() => interpretarPreco(texto), Error, texto);
});

test('estoque exige inteiro explícito e aceita zero', () => {
    const campos = { nome: ' Boné ', categoria: ' Acessórios ', preco: '10,00', estoque: '0', exclusivo: false };
    assert.deepEqual(lerProduto(campos), { nome: 'Boné', categoria: 'Acessórios', preco: 10, quantidadeEstoque: 0, isExclusivoDrop: false });
    for (const estoque of ['', ' ', '-1', '1.5', '1abc', '2147483648'])
        assert.throws(() => lerProduto({ ...campos, estoque }), Error);
    assert.throws(() => lerProduto({ ...campos, nome: ' ' }));
    assert.throws(() => lerProduto({ ...campos, categoria: ' ' }));
});
