import { formatarPreco, lerProduto } from './produto.mjs';

const API = '/api/produtos';
const form = document.querySelector('#produto-form');
const vitrine = document.querySelector('#vitrine');
const mensagem = document.querySelector('#mensagem');
const listaStatus = document.querySelector('#lista-status');
let editandoId = null;
let ocupado = false;

function avisar(texto, tipo = 'sucesso') {
    mensagem.textContent = texto;
    mensagem.dataset.tipo = tipo;
}

function bloquear(valor) {
    ocupado = valor;
    document.querySelector('#campos').disabled = valor;
    document.querySelectorAll('button').forEach(botao => { botao.disabled = valor; });
    vitrine.setAttribute('aria-busy', String(valor));
}

async function requisitar(caminho = '', options = {}) {
    let resposta;
    try {
        resposta = await fetch(API + caminho, {
            ...options, signal: AbortSignal.timeout(15000),
            headers: { 'Accept': 'application/json', ...(options.body ? { 'Content-Type': 'application/json' } : {}) }
        });
    } catch {
        throw new Error('Não foi possível confirmar a operação. Verifique a conexão e atualize a lista antes de tentar novamente.');
    }
    if (!resposta.ok) {
        const problema = await resposta.json().catch(() => null);
        const detalhes = problema?.errors ? Object.values(problema.errors).flat().join(' ') : null;
        throw new Error(detalhes || (resposta.status === 404
            ? 'Este produto não existe mais. Atualize a lista.'
            : 'Não foi possível concluir a operação. Tente novamente em instantes.'));
    }
    return resposta.status === 204 ? null : resposta.json();
}

// Dados cadastrados viram texto, nunca HTML executável.
function elemento(tag, texto, classe) {
    const el = document.createElement(tag);
    if (texto !== undefined) el.textContent = texto;
    if (classe) el.className = classe;
    return el;
}

function renderizar(produtos) {
    const fragmento = document.createDocumentFragment();
    for (const produto of produtos) {
        const card = elemento('article', undefined, 'card');
        card.append(
            elemento('h3', produto.nome),
            elemento('p', 'Categoria: ' + produto.categoria),
            elemento('p', 'Preço: ' + formatarPreco(produto.preco)),
            elemento('p', 'Estoque: ' + produto.quantidadeEstoque + ' un.')
        );
        if (produto.isExclusivoDrop)
            card.append(elemento('span', 'DROP EXCLUSIVO', 'tag-exclusivo'));
        const acoes = elemento('div', undefined, 'acoes');
        const editar = elemento('button', 'Editar');
        editar.type = 'button';
        editar.setAttribute('aria-label', 'Editar ' + produto.nome);
        editar.addEventListener('click', () => iniciarEdicao(produto));
        const excluir = elemento('button', 'Excluir');
        excluir.type = 'button';
        excluir.setAttribute('aria-label', 'Excluir ' + produto.nome);
        excluir.addEventListener('click', () => excluirProduto(produto));
        acoes.append(editar, excluir);
        card.append(acoes);
        fragmento.append(card);
    }
    vitrine.replaceChildren(fragmento);
    listaStatus.textContent = produtos.length ? '' : 'Nenhum produto cadastrado. Cadastre a primeira peça.';
}

async function atualizarLista() {
    listaStatus.textContent = 'Carregando produtos…';
    try {
        renderizar(await requisitar());
    } catch (erro) {
        listaStatus.textContent = 'Não foi possível atualizar a lista. Os dados exibidos podem estar desatualizados.';
        throw erro;
    }
}

async function carregar() {
    if (ocupado) return;
    bloquear(true);
    avisar('');
    try { await atualizarLista(); }
    catch (erro) { avisar(erro.message, 'erro'); }
    finally { bloquear(false); }
}

function limparFormulario() {
    editandoId = null;
    form.reset();
    document.querySelector('#form-titulo').textContent = 'Cadastrar novo drop';
    document.querySelector('#salvar').textContent = 'Cadastrar produto';
    document.querySelector('#cancelar').hidden = true;
}

function iniciarEdicao(produto) {
    if (ocupado) return;
    editandoId = produto.id;
    form.elements.nome.value = produto.nome;
    form.elements.categoria.value = produto.categoria;
    form.elements.preco.value = produto.preco.toFixed(2).replace('.', ',');
    form.elements.estoque.value = produto.quantidadeEstoque;
    form.elements.exclusivo.checked = produto.isExclusivoDrop;
    document.querySelector('#form-titulo').textContent = 'Editar produto';
    document.querySelector('#salvar').textContent = 'Salvar alterações';
    document.querySelector('#cancelar').hidden = false;
    avisar('');
    form.elements.nome.focus();
}

form.addEventListener('submit', async event => {
    event.preventDefault();
    if (ocupado) return;
    let produto;
    try {
        produto = lerProduto({
            nome: form.elements.nome.value, categoria: form.elements.categoria.value,
            preco: form.elements.preco.value, estoque: form.elements.estoque.value,
            exclusivo: form.elements.exclusivo.checked
        });
    } catch (erro) { avisar(erro.message, 'erro'); return; }
    const editando = editandoId !== null;
    bloquear(true);
    avisar('');
    try {
        await requisitar(editando ? '/' + editandoId : '', {
            method: editando ? 'PUT' : 'POST', body: JSON.stringify(produto)
        });
        limparFormulario();
        avisar(editando ? 'Produto atualizado.' : 'Produto cadastrado.');
        try { await atualizarLista(); }
        catch { avisar('Produto salvo, mas a lista não pôde ser atualizada. Clique em Atualizar produtos.', 'erro'); }
    } catch (erro) { avisar(erro.message, 'erro'); }
    finally { bloquear(false); }
});

async function excluirProduto(produto) {
    if (ocupado || !window.confirm('Excluir "' + produto.nome + '"? Esta ação não pode ser desfeita.')) return;
    bloquear(true);
    avisar('');
    try {
        await requisitar('/' + produto.id, { method: 'DELETE' });
        if (editandoId === produto.id) limparFormulario();
        avisar('Produto excluído.');
        try { await atualizarLista(); }
        catch { avisar('Produto excluído, mas a lista não pôde ser atualizada. Clique em Atualizar produtos.', 'erro'); }
    } catch (erro) { avisar(erro.message, 'erro'); }
    finally { bloquear(false); }
}

document.querySelector('#carregar').addEventListener('click', carregar);
document.querySelector('#cancelar').addEventListener('click', () => {
    limparFormulario();
    avisar('');
    form.elements.nome.focus();
});
carregar();
