# Roteiro de demonstração para portfólio

## Uma demonstração curta

1. Rode a API e abra o painel.
2. Cadastre uma peça de 1.299,90 e estoque zero. Mostre a moeda correta e o produto persistido.
3. Edite o preço, o estoque e a exclusividade. Atualize a página para demonstrar persistência.
4. Envie um preço negativo pelo arquivo HTTP. Mostre o 400 com os erros de validação, explicando por que validar apenas no frontend seria insuficiente.
5. Mostre o Location de um cadastro e consulte o produto por esse endereço.
6. Demonstre a confirmação de exclusão.
7. Execute os testes e explique que cada teste usa um banco isolado.
8. Mostre o fluxo no código: app.js → ProdutoRequest → ProdutosController → AppDbContext.

Use dados fictícios na gravação e não mostre segredos, tokens ou configurações pessoais.

## Pontos que vale explicar

- Por que separar uma entidade de banco dos contratos HTTP.
- Por que usar decimal no backend para preço.
- Por que estoque zero é válido e estoque ausente não é.
- Por que texto vindo do banco também deve ser renderizado com segurança.
- Como as migrations permitem reconstruir o esquema após clonar.
- O que um teste de integração comprova que um teste com banco simulado pode não comprovar.
- Por que CORS não substitui autenticação.
- O que ainda falta para publicar um painel administrativo de uso real.

## Sugestão de texto para adaptar

“Evoluí o DropMon, meu projeto de estudo de Backend na UVV, de um cadastro simples para um gerenciamento completo de produtos.

Nesta etapa, trabalhei validação na API, contratos de entrada e saída, respostas HTTP, tratamento de erros e persistência com Entity Framework Core e SQLite. Também corrigi a conversão de preços em reais e a renderização de textos na interface.

Adicionei testes de integração com bancos temporários e um workflow de verificação no GitHub Actions. Meu próximo passo é evoluir a interface e estudar as diferenças ao usar outro banco relacional.

O projeto ainda é uma demonstração local. Estou documentando tanto as decisões quanto as limitações para acompanhar o aprendizado.”

Antes de publicar, ajuste o texto à sua experiência e confirme que o workflow já passou no GitHub. Inclua o link do repositório e uma demonstração que você tenha conferido.

## Próximas etapas para contar a evolução

- Interface: melhorar hierarquia, navegação e estados sem mudar o contrato da API.
- Banco: comparar SQLite com PostgreSQL ou SQL Server, documentando migrations e diferenças.
- Uso real: adicionar autenticação e permissões antes da exposição pública.

Este arquivo é apenas um roteiro. Nenhuma postagem é feita automaticamente.
