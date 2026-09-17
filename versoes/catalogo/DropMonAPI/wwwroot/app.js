import {formatarPreco, lerProduto} from './produto.mjs';
import {filtrarProdutos, lerAno} from './catalogo.mjs';
const $ = id => document.getElementById(id);
const form = $('produto-form');
const state = { produtos: [], modo: 'catalogo', ativo: null, editando: null, ocupado: false,
    fotos: [], novas: [], remover: [], carregado: false };
function el(tag, texto, classe) {
    const node = document.createElement(tag);
    if (texto !== undefined) node.textContent = texto;
    if (classe) node.className = classe;
    return node;
}
function avisar(texto, erro = false) { $('mensagem').textContent = texto; $('mensagem').dataset.error = erro; }
function imagem(url, nome, lazy = true) {
    if (!url) return el('span','SEM FOTO','placeholder');
    const img = el('img');
    img.src = url; img.alt = nome; img.decoding = 'async';
    img.loading = lazy ? 'lazy' : 'eager';
    if (!lazy) img.fetchPriority = 'high';
    img.addEventListener('error', () => img.replaceWith(el('span','FOTO INDISPONÍVEL','placeholder')), {once:true});
    return img;
}
async function api(path = '', options = {}) {
    let response;
    try {
        response = await fetch('/api/produtos' + path, { ...options,
            headers: options.body instanceof FormData ? {Accept:'application/json'} : {Accept:'application/json', 'Content-Type':'application/json'},
            signal: AbortSignal.timeout(30000) });
    } catch { throw new Error('Não foi possível confirmar a operação. Confira a conexão e atualize o catálogo antes de repetir.'); }
    if (!response.ok) {
        const body = await response.json().catch(() => null);
        throw new Error(body?.errors ? Object.values(body.errors).flat().join(' ') :
            body?.detail || (response.status === 404 ? 'Esta peça não está mais disponível. Atualize o catálogo.' :
            response.status === 413 ? 'O arquivo é muito grande. Use fotos de até 5 MB.' : 'Não foi possível concluir a operação.'));
    }
    return response.status === 204 ? null : response.json();
}
function busy(value) {
    state.ocupado = value;
    $('campos').disabled = value;
    for (const id of ['salvar','nova-peca','atualizar','editar','excluir','confirmar-exclusao','cancelar-exclusao'])
        $(id).disabled = value;
    document.querySelectorAll('[data-close]').forEach(b => b.disabled = value);
}
function filtros() {
    return {categoria:$('categoria-filtro').value,drop:$('drop-filtro').value,ano:$('ano-filtro').value,
        estoque:$('estoque-filtro').value,busca:$('busca').value};
}
function opcoes(id, valores, titulo, sem) {
    const select = $(id); const atual = select.value;
    select.replaceChildren(new Option(titulo,''));
    for (const valor of [...new Set(valores.filter(v => v !== null && v !== ''))].sort((a,b)=>String(a).localeCompare(String(b),'pt-BR')))
        select.add(new Option(String(valor),String(valor)));
    if (sem) select.add(new Option(sem,'__sem__'));
    select.value = [...select.options].some(o=>o.value===atual) ? atual : '';
}
function atualizarFiltros() {
    opcoes('categoria-filtro',state.produtos.map(p=>p.categoria),'Todas as categorias');
    opcoes('drop-filtro',state.produtos.map(p=>p.dropNome),'Todos os drops','Sem drop');
    opcoes('ano-filtro',state.produtos.map(p=>p.ano),'Todos os anos','Sem ano');
    for (const [id,prop] of [['categorias','categoria'],['drops','dropNome']])
        $(id).replaceChildren(...[...new Set(state.produtos.map(p=>p[prop]).filter(Boolean))].map(v=>new Option(v,v)));
}
function abrirDetalhe(p) {
    if(state.ocupado) return;
    state.ativo = p;
    $('detalhe-nome').textContent = p.nome;
    $('detalhe-categoria').textContent = p.categoria;
    $('detalhe-preco').textContent = formatarPreco(p.preco);
    $('detalhe-estoque').textContent = p.quantidadeEstoque + ' un.';
    $('detalhe-descricao').textContent = p.descricao || 'Nenhuma descrição adicionada.';
    $('detalhe-exclusivo').hidden = !p.isExclusivoDrop;
    const atributos = [['Drop',p.dropNome || 'Sem drop'],['Ano',p.ano || 'Sem ano'],['Cor',p.cor || 'Não informada'],['Material',p.material || 'Não informado']];
    $('detalhe-atributos').replaceChildren(...atributos.flatMap(([k,v])=>[el('dt',k),el('dd',v)]));
    const trocar = index => {
        $('foto-principal').replaceChildren(imagem(p.fotos[index]?.url,p.nome,false));
        [...$('miniaturas').children].forEach((b,i)=>b.setAttribute('aria-pressed',String(i===index)));
    };
    $('miniaturas').replaceChildren(...p.fotos.map((f,i)=>{
        const b = el('button'); b.type='button'; b.setAttribute('aria-label','Ver foto '+(i+1));
        b.append(imagem(f.thumbnailUrl,p.nome)); b.addEventListener('click',()=>trocar(i)); return b;
    }));
    $('miniaturas').hidden = p.fotos.length < 2; trocar(0);
    if (!$('detalhes').open) $('detalhes').showModal();
}
function renderizar() {
    const f = filtros(); const produtos = filtrarProdutos(state.produtos, f);
    $('limpar').hidden = !Object.values(f).some(Boolean);
    $('contagem').textContent = produtos.length + (produtos.length===1 ? ' PEÇA' : ' PEÇAS') + ' / ' +
        produtos.reduce((s,p)=>s+p.quantidadeEstoque,0) + ' UN. EM ESTOQUE';
    $('resumo').textContent = state.produtos.some(p=>p.descricao?.includes('Produto fictício para demonstração.'))
        ? 'ACERVO DE DEMONSTRAÇÃO' : 'CATÁLOGO E GESTÃO DE ESTOQUE';
    $('grade').hidden = state.modo !== 'catalogo' || !produtos.length;
    $('tabela-container').hidden = state.modo !== 'estoque' || !produtos.length;
    $('vazio').hidden = !!produtos.length;
    $('vazio-texto').textContent = state.produtos.length ? 'Tente outros filtros ou limpe a busca.' : 'Adicione a primeira peça em Nova peça.';
    $('grade').replaceChildren(...produtos.map((p,index)=>{
        const article=el('article',undefined,'piece'), b=el('button',undefined,'piece-button');
        b.type='button'; b.setAttribute('aria-label',p.nome+' '+p.quantidadeEstoque+' UN. '+[p.dropNome,p.ano].filter(Boolean).join(' / ')+'. Ver detalhes');
        const photo=el('div',undefined,'product-photo'); photo.append(imagem(p.fotos[0]?.thumbnailUrl,p.nome,index>=2));
        const caption=el('div',undefined,'piece-caption');
        caption.append(el('span',p.nome,'piece-name'),el('span',p.quantidadeEstoque+' UN.','piece-stock'+(p.quantidadeEstoque===0?' out':'')));
        b.append(photo,caption);
        if (p.dropNome || p.ano) b.append(el('p',[p.dropNome,p.ano].filter(Boolean).join(' / '),'piece-drop'));
        b.addEventListener('click',()=>abrirDetalhe(p)); article.append(b); return article;
    }));
    $('tabela').replaceChildren(...produtos.map(p=>{
        const row=el('tr'), first=el('td');
        first.append(imagem(p.fotos[0]?.thumbnailUrl,p.nome),el('span',p.nome)); row.append(first,
            el('td',p.categoria),el('td',[p.dropNome||'Sem drop',p.ano||'Sem ano'].join(' / ')),
            el('td',formatarPreco(p.preco)),el('td',p.quantidadeEstoque+' un.'));
        const actions=el('td'), b=el('button','EDITAR'); b.setAttribute('aria-label','Editar '+p.nome);
        b.addEventListener('click',()=>abrirEditor(p)); actions.append(b); row.append(actions); return row;
    }));
}
async function recarregar() {
    const produtos = await api(); state.produtos=produtos; state.carregado=true;
    atualizarFiltros(); renderizar();
}
async function carregar() {
    if(state.ocupado) return; busy(true); avisar(''); $('grade').setAttribute('aria-busy','true');
    try { await recarregar(); }
    catch(e) { avisar(e.message,true); $('contagem').textContent='CATÁLOGO INDISPONÍVEL'; if(!state.carregado) $('grade').replaceChildren(); }
    finally { busy(false); $('grade').setAttribute('aria-busy','false'); }
}
function liberarPreviews() { state.novas.forEach(f=>URL.revokeObjectURL(f.url)); state.novas=[]; }
function fotosPreview() {
    const fotos = [...state.fotos.filter(f=>!state.remover.includes(f.id)).map(f=>({...f,tipo:'existente'})),
        ...state.novas.map((f,i)=>({url:f.url,index:i,tipo:'nova'}))];
    $('fotos-preview').replaceChildren(...fotos.map((f,i)=>{
        const wrapper=el('div',undefined,'photo-preview'); wrapper.append(imagem(f.url,'Foto '+(i+1),false));
        const b=el('button','×'); b.type='button'; b.setAttribute('aria-label','Remover foto '+(i+1));
        b.addEventListener('click',()=>{
            if(state.ocupado)return;
            if(f.tipo==='existente') state.remover.push(f.id);
            else {URL.revokeObjectURL(state.novas[f.index].url);state.novas.splice(f.index,1);}
            fotosPreview();
        });
        wrapper.append(b); if(i===0)wrapper.append(el('small','CAPA')); return wrapper;
    }));
}
function abrirEditor(p=null) {
    if(state.ocupado)return;
    liberarPreviews(); state.editando=p?.id??null;state.fotos=p?.fotos?[...p.fotos]:[];state.remover=[];
    form.reset();$('form-erro').textContent='';
    $('form-titulo').textContent=p?'Editar peça':'Nova peça';
    for(const key of ['nome','categoria','dropNome','ano','cor','material','descricao'])
        form.elements[key].value=p?.[key]??'';
    form.elements.preco.value=p?String(p.preco.toFixed(2)).replace('.',','):'';
    form.elements.estoque.value=p?.quantidadeEstoque??'';
    form.elements.exclusivo.checked=p?.isExclusivoDrop??false;
    fotosPreview();$('detalhes').close();$('editor').showModal();
}
$('fotos').addEventListener('change',()=>{
    $('form-erro').textContent='';
    const selected=[...$('fotos').files];
    const count=state.fotos.length-state.remover.length+state.novas.length;
    if(count+selected.length>4){$('form-erro').textContent='Cada peça pode ter até 4 fotos.';$('fotos').value='';return;}
    for(const file of selected) {
        if(!['image/jpeg','image/png','image/webp'].includes(file.type)||file.size>5*1024*1024||file.size===0) {
            $('form-erro').textContent='Use fotos JPG, PNG ou WebP, com até 5 MB cada.';$('fotos').value='';return;
        }
    }
    state.novas.push(...selected.map(file=>({file,url:URL.createObjectURL(file)})));
    $('fotos').value='';fotosPreview();
});
form.addEventListener('submit',async event=>{
    event.preventDefault();if(state.ocupado)return;
    $('form-erro').textContent='';let produto;
    try {
        produto={...lerProduto({nome:form.elements.nome.value,categoria:form.elements.categoria.value,
            preco:form.elements.preco.value,estoque:form.elements.estoque.value,exclusivo:form.elements.exclusivo.checked}),
            dropNome:form.elements.dropNome.value.trim()||null,ano:lerAno(form.elements.ano.value),
            cor:form.elements.cor.value.trim()||null,material:form.elements.material.value.trim()||null,
            descricao:form.elements.descricao.value.trim()||null};
    }catch(e){$('form-erro').textContent=e.message;return;}
    busy(true);let dadosSalvos=false;
    try {
        if(state.editando!==null)await api('/'+state.editando,{method:'PUT',body:JSON.stringify(produto)});
        else {const criado=await api('',{method:'POST',body:JSON.stringify(produto)});state.editando=criado.id;}
        dadosSalvos=true;
        // Cada etapa confirmada sai da fila: repetir uma falha não duplica fotos já enviadas.
        while(state.remover.length) {
            const id=state.remover[0];await api('/'+state.editando+'/fotos/'+id,{method:'DELETE'});
            state.remover.shift();state.fotos=state.fotos.filter(f=>f.id!==id);
        }
        while(state.novas.length){
            const item=state.novas[0],data=new FormData();data.append('arquivo',item.file);
            const foto=await api('/'+state.editando+'/fotos',{method:'POST',body:data});
            state.fotos.push(foto);URL.revokeObjectURL(item.url);state.novas.shift();
        }
        $('editor').close();avisar('Peça salva.');
        try{await recarregar();}catch{avisar('Peça salva. Não foi possível atualizar o catálogo; clique em Atualizar.',true);}
    }catch(e){
        $('form-erro').textContent=(dadosSalvos?'Os dados da peça foram salvos, mas as fotos não foram concluídas. ':'')+e.message;
        fotosPreview();
    }finally{busy(false);}
});
function trocarModo(modo) {
    state.modo=modo;
    $('modo-catalogo').setAttribute('aria-pressed',String(modo==='catalogo'));
    $('modo-estoque').setAttribute('aria-pressed',String(modo==='estoque'));
    if(state.carregado)renderizar();
}
$('modo-catalogo').addEventListener('click',()=>trocarModo('catalogo'));
$('modo-estoque').addEventListener('click',()=>trocarModo('estoque'));
$('nova-peca').addEventListener('click',()=>abrirEditor());
$('editar').addEventListener('click',()=>abrirEditor(state.ativo));
$('excluir').addEventListener('click',()=>{
    $('confirmar-texto').textContent=state.ativo.nome;$('excluir-erro').textContent='';$('confirmacao').showModal();
});
$('cancelar-exclusao').addEventListener('click',()=>$('confirmacao').close());
$('confirmar-exclusao').addEventListener('click',async()=>{
    if(state.ocupado)return;busy(true);
    try{
        await api('/'+state.ativo.id,{method:'DELETE'});$('confirmacao').close();$('detalhes').close();state.ativo=null;
        avisar('Peça excluída.');try{await recarregar();}catch{avisar('Peça excluída. Atualize o catálogo para conferir a lista.',true);}
    }catch(e){$('excluir-erro').textContent=e.message;}finally{busy(false);}
});
document.querySelectorAll('[data-close]').forEach(b=>b.addEventListener('click',()=>$(b.dataset.close).close()));
for(const id of ['detalhes','editor','confirmacao'])$(id).addEventListener('cancel',e=>{if(state.ocupado)e.preventDefault();});
$('editor').addEventListener('close',()=>liberarPreviews());
for(const id of ['categoria-filtro','drop-filtro','ano-filtro','estoque-filtro'])$(id).addEventListener('change',()=>{if(state.carregado)renderizar();});
$('busca').addEventListener('input',()=>{if(state.carregado)renderizar();});
$('limpar').addEventListener('click',()=>{
    for(const id of ['categoria-filtro','drop-filtro','ano-filtro','estoque-filtro','busca'])$(id).value='';
    renderizar();
});
$('atualizar').addEventListener('click',carregar);
for(let i=0;i<6;i++){const sk=el('div',undefined,'skeleton');sk.setAttribute('aria-hidden','true');sk.append(el('div',undefined,'product-photo'),el('div',undefined,'skeleton-line'));$('grade').append(sk);}
carregar();

