const moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
export const formatarPreco = valor => moeda.format(valor);

// Aceita 1299,90, 1.299,90 e 1299.90. Rejeita valores parciais como "12abc".
export function interpretarPreco(texto) {
    const valor = texto.trim();
    let normalizado;
    if (/^\d{1,3}(\.\d{3})+,\d{1,2}$/.test(valor)) {
        normalizado = valor.replaceAll('.', '').replace(',', '.');
    } else if (/^\d+(?:[,.]\d{1,2})?$/.test(valor)) {
        normalizado = valor.replace(',', '.');
    } else {
        throw new Error('Informe um preço válido, como 149,90 ou 1.299,90.');
    }
    const numero = Number(normalizado);
    if (!Number.isFinite(numero) || numero < 0.01 || numero > 999999.99)
        throw new Error('O preço deve estar entre 0,01 e 999.999,99.');
    return numero;
}

export function lerProduto(campos) {
    const nome = campos.nome.trim();
    const categoria = campos.categoria.trim();
    if (!nome || nome.length > 120) throw new Error('Informe um nome de até 120 caracteres.');
    if (!categoria || categoria.length > 60) throw new Error('Informe uma categoria de até 60 caracteres.');
    const textoEstoque = campos.estoque.trim();
    const estoque = Number(textoEstoque);
    if (!/^\d+$/.test(textoEstoque) || !Number.isInteger(estoque) || estoque > 2147483647)
        throw new Error('Informe um estoque inteiro maior ou igual a zero.');
    return { nome, categoria, preco: interpretarPreco(campos.preco),
        quantidadeEstoque: estoque, isExclusivoDrop: campos.exclusivo };
}
