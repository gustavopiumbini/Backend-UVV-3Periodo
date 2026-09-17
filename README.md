# DropMon — gestão de produtos e drops

## Versões disponíveis

- **Catálogo visual (versão mais recente):** [versoes/catalogo](versoes/catalogo/README.md), com fotos, estoque, detalhes e filtros por categoria, drop e ano. Execute seguindo o README dessa pasta; disponível localmente na porta 5136.
- **Base consolidada:** mantida na raiz, com CRUD e validações, na porta 5126. As instruções abaixo descrevem essa base.

As duas versões usam bancos locais separados. Bancos e uploads não são enviados ao GitHub; o catálogo inclui imagens e um comando para gerar os produtos fictícios de demonstração. O GitHub Actions verifica ambas as versões.

Projeto de estudo e portfólio desenvolvido no contexto do 3º período de Backend da UVV. O DropMon é um painel administrativo para cadastrar e gerenciar peças de streetwear, usando a marca tsey como contexto.

O foco desta etapa é mostrar um fluxo completo e verificável: formulário → API HTTP → validação → persistência → atualização da interface.

> Demonstração local, sem autenticação. Antes de disponibilizar o painel na internet com dados reais, implemente autenticação, autorização e HTTPS.

## O que funciona

- Cadastrar, listar, consultar por ID, editar e excluir produtos.
- Nome, categoria, preço em reais, estoque e marcação de drop exclusivo.
- Validação no navegador e na API, incluindo campos ausentes e preços com mais de duas casas decimais.
- Preços como `149,90`, `1299.90` e `1.299,90`.
- Mensagens de carregamento, lista vazia, sucesso e falha.
- Bloqueio dos controles durante requisições e confirmação antes de excluir.
- Documentação OpenAPI no ambiente de desenvolvimento.
- Testes de integração usando SQLite temporário e testes de conversão de valores no JavaScript.

## Tecnologias e organização

- **Backend:** C#, ASP.NET Core .NET 10 e Entity Framework Core.
- **Banco atual:** SQLite com migrations.
- **Frontend:** HTML, CSS e JavaScript com módulos nativos, servido pela própria API.
- **Testes:** xUnit, WebApplicationFactory e test runner nativo do Node.js.
- **CI:** GitHub Actions para restaurar, auditar dependências, compilar, testar e gerar a publicação.

```text
DropMonAPI/
  Contracts/        Entrada validada e resposta da API
  controllers/      Rotas HTTP e operações de produtos
  Models/           Entidade persistida
  data/             DbContext
  Migrations/       Histórico do esquema SQLite
  wwwroot/          index.html, styles.css, app.js e produto.mjs
DropMonAPI.Tests/    Testes da API com banco isolado
tests/              Testes de validação e moeda do frontend
docs/               Explicações das decisões e roteiro de demonstração
```

O frontend antes ficava em `DropMon-WEB`. Agora está em `DropMonAPI/wwwroot`, a pasta padrão de arquivos públicos do ASP.NET Core. CSS e JavaScript continuam separados do HTML.

## Executar após clonar

Pré-requisito: **SDK do .NET 10** instalado. Node.js 22 ou superior é necessário apenas para os testes de JavaScript.

Na raiz do repositório:

```bash
dotnet restore BACKEND-UVV.sln
dotnet run --project DropMonAPI --launch-profile http
```

Abra **http://localhost:5126**. O frontend e a API usam a mesma origem, sem necessidade de Live Server ou CORS aberto. Não abra o HTML diretamente pelo explorador de arquivos.

No ambiente `Development`, a aplicação aplica as migrations existentes ao iniciar e cria `DropMonAPI/dropmon.db` se ele ainda não existir. A lista começa vazia; cadastre produtos pelo painel. Reiniciar a aplicação preserva os dados.

A string de conexão está em `DropMonAPI/appsettings.json`. Caminhos relativos do SQLite são resolvidos em relação à pasta da API. Também é possível sobrescrever a configuração pela variável `ConnectionStrings__DefaultConnection`.

## Contrato da API

| Método | Rota | Resultado |
| --- | --- | --- |
| GET | /api/produtos | 200 com lista, ordenada por ID |
| GET | /api/produtos/{id} | 200 com produto ou 404 |
| POST | /api/produtos | 201 e cabeçalho Location para o produto criado |
| PUT | /api/produtos/{id} | 204 ou 404; substitui todos os campos editáveis |
| DELETE | /api/produtos/{id} | 204 ou 404 |

Exemplo de corpo para POST e PUT:

```json
{
  "nome": "Camiseta tsey",
  "categoria": "Camisetas",
  "preco": 149.90,
  "quantidadeEstoque": 12,
  "isExclusivoDrop": true
}
```

O JSON utiliza número com ponto decimal. A interface faz a conversão da entrada em reais.

Regras:
- Nome obrigatório, até 120 caracteres; categoria obrigatória, até 60.
- Espaços nas extremidades são removidos ao salvar.
- Preço obrigatório de **0,01 a 999.999,99**, com até duas casas decimais.
- Estoque obrigatório de **0 a 2.147.483.647**, sempre inteiro. Zero indica produto sem estoque.
- A marcação de exclusividade é opcional, com valor padrão `false`.
- O ID é gerado pelo banco; não faz parte do contrato de escrita.
- Requisições inválidas retornam **400**, com os erros por campo em `errors`.
- Erros inesperados usam Problem Details, sem expor detalhes internos ao cliente.

Em desenvolvimento, o documento OpenAPI está em **http://localhost:5126/openapi/v1.json**. Exemplos executáveis estão em [DropMonAPI.http](DropMonAPI/DropMonAPI.http).

## Testes e auditoria

```bash
dotnet test BACKEND-UVV.sln
node --test tests/produto.test.mjs
dotnet list DropMonAPI package --vulnerable --include-transitive --no-restore
```

Os testes da API criam um arquivo SQLite temporário por teste, aplicam as migrations e o removem ao terminar. Eles não usam o banco do painel. Os testes de JavaScript não exigem `npm install`.

A auditoria depende dos avisos disponíveis no NuGet no momento da consulta; ausência de alertas não substitui revisão de segurança.

## Migrations e publicação

A ferramenta EF está fixada no manifesto local:

```bash
dotnet tool restore
dotnet ef migrations list --project DropMonAPI
dotnet ef database update --project DropMonAPI
```

Para gerar os arquivos de publicação:

```bash
dotnet publish DropMonAPI --configuration Release --output artifacts/publish
```

Isso gera o backend e os arquivos do frontend, mas **não publica na internet**. Em produção, migrations não são aplicadas automaticamente e OpenAPI não é exposto. Prepare o banco como uma etapa controlada do deploy.

Arquivos `.db`, `.db-wal`, `.db-shm` e resultados de compilação não devem entrar no Git. Os antigos arquivos auxiliares do SQLite foram retirados do versionamento; o esquema é reconstruído pelas migrations.

## Decisões e próximos estudos

Leia [a explicação da consolidação](docs/consolidacao.md) para entender cada mudança e [o roteiro de demonstração](docs/portfolio.md) para apresentar o projeto.

Próximos passos: evolução visual, busca e paginação, autenticação, variações de produtos e estudo de PostgreSQL ou SQL Server. A troca de banco exigirá revisar o provedor EF, as migrations e a representação dos valores monetários; não é apenas trocar a string de conexão.

A listagem atual retorna todos os produtos. Edições simultâneas ainda seguem a regra de que a última gravação vence. Essas limitações estão mantidas explícitas para orientar as próximas etapas.
