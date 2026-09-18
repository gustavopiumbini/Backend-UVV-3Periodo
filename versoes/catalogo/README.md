# DropMon / catálogo

Versão local independente da base consolidada, com catálogo visual e gestão de estoque inspirados na organização minimalista da [Yeezy](https://yeezy.com/). Mantém a identidade tsey / DropMon e usa imagens próprias de demonstração.

## Executar

Pré-requisito: SDK .NET 10. Na pasta `versoes/catalogo`:

```powershell
dotnet restore BACKEND-UVV.sln
dotnet run --project DropMonAPI --launch-profile http
```

Abra **http://localhost:5136**. Esta versão usa outra porta e o banco **DropMonAPI/catalogo.db**, separado da versão original.

Opcionalmente, para preencher um banco vazio com seis peças fictícias:

```powershell
dotnet run --project DropMonAPI --launch-profile http -- --Demo:Seed=true
```

A demonstração só é inserida em Development e se o catálogo estiver vazio. O comando normal não repõe produtos excluídos. O catálogo local entregue já contém os seis exemplos, identificados como demonstração.

Também é possível executar `./iniciar.ps1` ou `./iniciar.ps1 -Demo`.

## Testar no Render

Crie um **Web Service** conectado ao repositório e configure:

- Language: `Docker`
- Branch: `main`
- Root Directory: `versoes/catalogo`
- Dockerfile Path: `./Dockerfile`
- Docker Build Context Directory: `.`
- Compute: `Free`

Build Command e Start Command não são necessários: o Dockerfile compila a API e inicia na porta 10000. Para persistência, autenticação e fotos, configure o Supabase pelas variáveis de ambiente descritas no [guia de configuração](docs/supabase.md). Cada push na `main` pode gerar um novo deploy automaticamente.

## Funcionalidades

- Página de login antes do catálogo, validada pelo Supabase Auth.
- Sessão administrativa em cookie protegido; nenhuma chave é enviada ao JavaScript.
- Catálogo, estoque, fotos e rotas da API disponíveis somente após autenticação.
- Grade branca de produtos, com estoque ao lado do nome.
- Até quatro fotos por produto; clique na foto para abrir detalhes e galeria.
- Cadastro e edição em painel lateral.
- Nome, categoria, preço, estoque e indicação de drop exclusivo.
- Drop, ano, descrição, cor e material opcionais.
- Filtros combináveis de categoria, drop, ano e disponibilidade, incluindo “Sem drop” e “Sem ano”.
- Busca por nome, categoria, drop, cor ou material.
- Visualização alternativa de estoque em tabela.
- Exclusão com confirmação, estados vazios, mensagens de falha e bloqueio durante gravação.
- Layout para desktop e celular, navegação por teclado e dialogs nativos.

Categoria representa o tipo de peça (camisetas, calças etc.). Drop representa a coleção e ano representa o período. São campos diferentes para permitir filtros combinados. Deixar drop e ano vazios mantém a peça no acervo sem obrigar uma classificação.

## Fotos e persistência

Formatos: JPG, PNG e WebP. Limite por foto: 5 MB e 24 megapixels; máximo de quatro fotos por produto.

A API identifica e decodifica a imagem de verdade, aplica orientação, limita o tamanho a 1600 × 1600 sem distorcer a proporção e salva em WebP com compressão explícita. A grade usa miniaturas de até 480 px; os detalhes usam a imagem maior. Metadados são removidos na gravação. O nome original do arquivo não é usado no caminho de armazenamento.

- Desenvolvimento local: SQLite em `DropMonAPI/catalogo.db` e fotos em `DropMonAPI/App_Data/uploads/`.
- Render: PostgreSQL e bucket privado no Supabase quando as variáveis estão configuradas.
- Fotos: `/media/{identificador}.webp`, rota protegida pela mesma sessão do catálogo.
- Originais gerados para a demonstração: `DropMonAPI/DemoAssets/`, fora da pasta pública.

O modo local permanece disponível para desenvolvimento sem depender da internet. Em produção, a API escolhe PostgreSQL por `Database__Provider=Postgres` e o Storage remoto quando recebe a URL e a chave secreta do Supabase.

Dados do produto e uploads são operações HTTP separadas. Se uma foto falhar, a interface informa que os dados já foram salvos e mantém o editor aberto; as etapas já confirmadas saem da fila. Falhas de conexão com resultado desconhecido ainda exigem atualizar o catálogo antes de repetir.

## Rotas

| Método | Rota | Uso |
| --- | --- | --- |
| GET | /api/produtos | Listar com fotos e características |
| GET | /api/produtos/{id} | Consultar |
| POST | /api/produtos | Cadastrar dados |
| PUT | /api/produtos/{id} | Editar dados |
| DELETE | /api/produtos/{id} | Excluir peça e suas fotos |
| POST | /api/produtos/{id}/fotos | Enviar uma foto, multipart com campo arquivo |
| DELETE | /api/produtos/{id}/fotos/{fotoId} | Remover foto |
| GET | /media/{arquivo} | Ler imagem validada |

Todas essas rotas exigem a sessão administrativa quando `Admin__RequireAuthentication=true`. A página estática inicial exibe somente o login até a sessão ser confirmada.

As regras anteriores de preço e estoque foram mantidas: preço entre 0,01 e 999.999,99, no máximo duas casas decimais, e estoque inteiro não negativo. Ano aceita 1900-2100 ou null. O ID continua sendo gerado pelo servidor.

OpenAPI em desenvolvimento: **http://localhost:5136/openapi/v1.json**.

## Testes

```powershell
dotnet test BACKEND-UVV.sln
node --test tests/*.test.mjs
dotnet list DropMonAPI package --vulnerable --include-transitive --no-restore
dotnet publish DropMonAPI --configuration Release --output artifacts/publish
```

Node.js 22+ é necessário apenas para os testes de JavaScript. Não há framework frontend, bundler ou npm install obrigatório.

Os testes utilizam SQLite e diretórios de mídia temporários. Cobrem CRUD, validação, campos opcionais, formatos de imagem, arquivo falso, limite de fotos, remoção de mídia, filtros, proteção do catálogo e geração das migrations PostgreSQL com RLS.

## Evolução técnica

A migration `CatalogoComFotos` acrescenta os campos opcionais e a tabela de fotos. A migration `ProtecaoSupabase` habilita RLS e revoga o acesso direto dos papéis públicos às tabelas. A API aceita SQLite em desenvolvimento e PostgreSQL em produção pelo mesmo modelo do Entity Framework.

Leia [as decisões desta versão](docs/catalogo.md) e [a origem das imagens](docs/imagens-demo.md).

## Limites atuais

O acesso foi projetado para um único administrador definido pelo UUID do Supabase Auth. Ainda não há recuperação de senha dentro do DropMon, múltiplos gestores, papéis diferentes ou auditoria de alterações.

Filtros e busca operam no navegador sobre a lista carregada. Para um catálogo grande, o próximo passo será paginação e filtragem na API. O estoque ainda é total por peça, sem variantes por tamanho, movimentações ou histórico de auditoria.
