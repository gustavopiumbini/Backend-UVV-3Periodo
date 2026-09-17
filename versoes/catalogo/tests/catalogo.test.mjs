import test from 'node:test';
import assert from 'node:assert/strict';
import {filtrarProdutos,lerAno} from '../DropMonAPI/wwwroot/catalogo.mjs';
const produtos=[
 {nome:'TS-01',categoria:'Camisetas',dropNome:'Essentials',ano:2026,quantidadeEstoque:12,cor:'Preto'},
 {nome:'HD-01',categoria:'Moletons',dropNome:'Essentials',ano:2025,quantidadeEstoque:0},
 {nome:'CP-01',categoria:'Acessórios',dropNome:null,ano:null,quantidadeEstoque:3}
];
test('filtros combinam busca, categoria, drop e ano',()=>{
 assert.deepEqual(filtrarProdutos(produtos,{busca:'preto',categoria:'Camisetas',drop:'Essentials',ano:'2026'}),[produtos[0]]);
 assert.equal(filtrarProdutos(produtos,{categoria:'Camisetas',ano:'2025'}).length,0);
 assert.equal(filtrarProdutos(produtos,{}).length,3);
});
test('classificação opcional e estoque zero não desaparecem',()=>{
 assert.deepEqual(filtrarProdutos(produtos,{drop:'__sem__',ano:'__sem__'}),[produtos[2]]);
 assert.deepEqual(filtrarProdutos(produtos,{estoque:'esgotado'}),[produtos[1]]);
 assert.deepEqual(filtrarProdutos(produtos,{estoque:'baixo'}),[produtos[2]]);
 assert.equal(filtrarProdutos(produtos,{estoque:'disponivel'}).length,2);
});
test('ano opcional aceita vazio e rejeita ano parcial',()=>{
 assert.equal(lerAno(''),null);assert.equal(lerAno('2026'),2026);
 for(const v of ['26','2026x','1899','2101','2026.5'])assert.throws(()=>lerAno(v));
});

