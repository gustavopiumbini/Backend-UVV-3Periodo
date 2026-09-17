# Consolidação da base

Esta etapa mantém a aplicação pequena e compreensível para estudo e portfólio. O banco continua sendo SQLite e o visual escuro original foi preservado, com ajustes de largura e acessibilidade.

## 1. DTOs: separar HTTP de persistência

Antes, o POST recebia diretamente a entidade Produto, incluindo seu Id. Agora, ProdutoRequest descreve os campos que o cliente pode enviar, e ProdutoResponse define o retorno.

Isso evita que futuras propriedades internas da entidade sejam automaticamente expostas ou aceitas pela API. O mapeamento é explícito e pequeno, sem biblioteca adicional.

Os campos numéricos do request são nullable para distinguir uma omissão de um zero. Estoque zero é válido; estoque ausente é erro. Data Annotations e IValidatableObject expressam as regras, e ApiController produz a resposta 400 antes da ação salvar no banco.

O preço tem uma regra escolhida para esta etapa: deve ser positivo, até 999.999,99 e ter no máximo duas casas decimais. Valores com precisão maior são rejeitados, não arredondados silenciosamente.

## 2. CRUD e respostas HTTP

O cadastro retorna 201 com Location apontando para GET /api/produtos/{id}. Agora esse endereço identifica um recurso específico.

PUT substitui os campos editáveis; DELETE exclui o produto. Ambos retornam 204 quando concluem e 404 quando o recurso não existe. As consultas usam AsNoTracking, pois não precisam acompanhar mudanças em entidades lidas. CancellationToken permite cancelar operações quando a requisição é cancelada.

O controller ainda usa DbContext diretamente. Com uma entidade e regras pequenas, camadas adicionais de serviços e repositórios acrescentariam pouco. Podemos introduzir serviços quando surgirem operações de negócio compartilhadas.

## 3. Texto seguro no navegador

Antes, o nome e a categoria eram interpolados em innerHTML. Um conteúdo cadastrado poderia ser interpretado como marcação e executar código no navegador.

Agora os elementos são construídos com createElement, append e textContent. Assim, um nome como `<img src=x onerror=alert(1)>` aparece literalmente como texto. A API não precisa proibir os caracteres < e >: o tratamento correto acontece no contexto de exibição.

Referência: [MDN — innerHTML](https://developer.mozilla.org/en-US/docs/Web/API/Element/innerHTML).

## 4. Moeda e experiência de uso

A conversão valida a entrada inteira antes de transformá-la em número. Por exemplo, 1.299,90 vira 1299.90; 12abc é rejeitado. O formato ambíguo 1.299 também é rejeitado: para mil duzentos e noventa e nove, use 1299 ou 1.299,00.

Intl.NumberFormat exibe a moeda em pt-BR. A precisão final também é validada pela API com decimal.

O frontend passou a:
- Carregar os produtos ao abrir.
- Mostrar mensagens de lista vazia, carregamento, erro e sucesso.
- Preservar o formulário quando a gravação falha.
- Diferenciar falha ao salvar de falha ao recarregar uma gravação que já deu certo.
- Bloquear os controles durante requisições.
- Permitir edição, cancelamento de edição e exclusão com confirmação.
- Usar rótulos associados aos campos, foco visível e mensagens anunciadas por leitores de tela.

O bloqueio evita cliques repetidos enquanto a requisição está em andamento, mas não é idempotência no servidor. Se a conexão cair depois de o servidor gravar, é necessário atualizar a lista antes de repetir um cadastro.

## 5. Executar o clone de forma previsível

O frontend fica em wwwroot e é servido pelo ASP.NET Core. A URL no JavaScript é relativa: /api/produtos. Isso elimina a configuração duplicada de localhost e a necessidade de permitir todas as origens no CORS.

A conexão SQLite vem da configuração. Em Development, as migrations existentes são aplicadas ao iniciar; em produção, essa responsabilidade fica no deploy. Arquivos locais de banco são ignorados pelo Git.

O pacote de OpenAPI agora é utilizado e a documentação é exposta somente em desenvolvimento. Exceções inesperadas são tratadas pelo middleware de Problem Details; detalhes ficam nos logs.

## 6. Dependências

ASP.NET Core OpenAPI e EF Core foram atualizados para 10.0.12. O pacote ASP.NET Core atualizado resolve Microsoft.OpenApi 2.12.0, acima da correção do aviso consultado.

SQLitePCLRaw.bundle_e_sqlite3 foi atualizado para 3.0.5 para sair da dependência nativa antiga reportada pela auditoria. Os testes usam o SQLite real para verificar a integração do provedor após a atualização.

A consulta ao NuGet após a mudança não reportou pacotes vulneráveis na API, incluindo transitivos. Os avisos originais estão em:
- [Microsoft.OpenApi — GHSA-v5pm-xwqc-g5wc](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc).
- [SQLite — GHSA-2m69-gcr7-jv3q](https://github.com/advisories/GHSA-2m69-gcr7-jv3q).

## 7. Evidências e testes

A suíte da API verifica criação, consulta, edição, exclusão, Location, normalização de textos, estoque zero, campos ausentes, valores inválidos, precisão monetária, limites de tamanho, proteção do ID e documentação das rotas.

Cada caso usa um SQLite isolado e aplica as migrations reais. Isso testa HTTP, validação e persistência juntos, sem depender de mocks que poderiam esconder diferenças do banco.

Os testes JavaScript verificam os formatos aceitos de moeda, valores ambíguos ou parcialmente numéricos e estoque inválido. Eles não substituem um teste completo de navegador.

O workflow do GitHub Actions executa build, testes e publicação dos arquivos, além de tratar avisos de vulnerabilidades como falha na restauração. O workflow só executará no GitHub depois que as mudanças forem enviadas.

## 8. Limites deliberados

- Sem login e autorização nesta etapa: é uma demonstração local, ainda não um painel pronto para exposição pública.
- Sem paginação e filtros: adequados como próxima evolução conforme o catálogo crescer.
- Sem controle de versão de registros: duas edições simultâneas podem sobrescrever uma à outra.
- Sem nova estrutura do banco: a migration original foi preservada, e a validação adicionada protege a entrada HTTP, sem corrigir automaticamente dados antigos.
- Sem mudança de provedor: PostgreSQL ou SQL Server será uma etapa própria, com migrations e testes revisados.

Para estudar outro banco, os contratos HTTP e a tela podem ser preservados. O trabalho se concentra na configuração do provedor, nos mapeamentos, nas migrations e nos testes. Consultas monetárias e concorrência merecem atenção especial nessa comparação.
