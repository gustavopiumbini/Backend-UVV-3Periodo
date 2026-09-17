export function filtrarProdutos(produtos, filtros) {
    const termo = (filtros.busca || '').trim().toLocaleLowerCase('pt-BR');
    return produtos.filter(p => {
        if (termo && ![p.nome,p.categoria,p.dropNome,p.cor,p.material].some(v => v?.toLocaleLowerCase('pt-BR').includes(termo))) return false;
        if (filtros.categoria && p.categoria !== filtros.categoria) return false;
        if (filtros.drop === '__sem__' ? !!p.dropNome : filtros.drop && p.dropNome !== filtros.drop) return false;
        if (filtros.ano === '__sem__' ? p.ano !== null : filtros.ano && String(p.ano) !== filtros.ano) return false;
        if (filtros.estoque === 'disponivel' && p.quantidadeEstoque <= 0) return false;
        if (filtros.estoque === 'baixo' && (p.quantidadeEstoque < 1 || p.quantidadeEstoque > 5)) return false;
        if (filtros.estoque === 'esgotado' && p.quantidadeEstoque !== 0) return false;
        return true;
    });
}
export function lerAno(texto) {
    if (!texto.trim()) return null;
    if (!/^\d{4}$/.test(texto) || Number(texto) < 1900 || Number(texto) > 2100)
        throw new Error('Informe um ano entre 1900 e 2100 ou deixe em branco.');
    return Number(texto);
}
