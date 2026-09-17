# Catálogo: o que mudou e por quê

## Cópia independente

O código foi copiado para versoes/catalogo sem reutilizar o banco nem a pasta de fotos da versão consolidada. O endereço é localhost:5136; a versão anterior usa localhost:5126. Nenhum commit ou envio ao GitHub foi feito.

## Leitura da referência

A referência foi analisada em https://yeezy.com/ e na captura enviada: fundo branco, grade regular, produtos sem molduras, nomes curtos, tipografia discreta e informações secundárias reveladas ao selecionar uma peça.

A adaptação mantém esses princípios e acrescenta as necessidades de gestão: quantidade ao lado do nome, filtros e acesso ao cadastro. Não foram copiados código, logotipo ou fotografias da Yeezy.

A base anterior tinha fundo escuro, formulário permanente e produtos em caixas. O redesenho coloca o acervo em primeiro plano e move o formulário para um painel lateral. O nome tsey / DropMon e as funcionalidades de CRUD foram preservados.

Direção: catálogo de moda para o dono de marca; composição regular, movimento discreto, densidade baixa. Dials da skill: DESIGN_VARIANCE 3, MOTION_INTENSITY 2, VISUAL_DENSITY 3. Branco fixo foi uma escolha explícita do briefing.

## Interface

- Grade responsiva: fotos em fundo branco, nome e quantidade na mesma legenda.
- Detalhes: foto ampliada, miniaturas, preço, estoque, descrição e classificação.
- Painel lateral: cadastro e edição sem perder a posição do catálogo.
- Estoque: tabela alternativa para leitura operacional dos mesmos dados.
- Filtros: categoria, drop, ano e disponibilidade podem ser combinados com busca.
- Sem classificação: drop e ano podem ficar vazios e têm filtros próprios.
- Acessibilidade: labels, foco visível, área principal identificada, mensagens de estado e foco contido em dialogs nativos.
- JavaScript nativo foi mantido para continuar fácil de estudar. O redesenho não exigiu trocar a stack.

## Dados

Categoria continua sendo o tipo de produto. Drop é o nome de uma coleção; ano é opcional e independente do drop. Separar os campos evita colocar “Camisetas / Drop verão / 2026” dentro de uma única categoria e perder a capacidade de filtrar.

A nova tabela ProdutoFoto relaciona várias fotos a uma peça. A primeira foto cadastrada é a capa; remover a capa promove a próxima. Os campos novos são opcionais para preservar a compatibilidade com cadastros anteriores.

## Upload

A API usa ImageSharp para verificar o conteúdo real, em vez de confiar na extensão ou no MIME enviado pelo navegador. A imagem é regravada em WebP com nome aleatório e sem metadados, fora da pasta pública de código.

O limite de quatro fotos é conferido em transação no SQLite. Ao excluir um produto ou uma foto, a referência é removida do banco e o arquivo correspondente é limpo. Falha de limpeza física é registrada nos logs.

Banco e diretório de uploads devem ser preservados juntos. Eles foram excluídos do versionamento e da publicação para não misturar código, mídia local e dados reais.

## Exemplos visuais

Seis produtos fictícios demonstram drops, anos, estoque zero e ausência de classificação. As imagens foram geradas especificamente para a demonstração. O catálogo informa “Acervo de demonstração” enquanto essas peças estiverem presentes.

A carga de exemplos é opcional. As imagens da demonstração passam pelo mesmo processamento da aplicação para que o catálogo utilize WebP otimizado.

## Verificações

- Testes de integração: regras anteriores e novos campos, fotos reais e inválidas, limite por produto, exclusão e arquivos.
- Testes JavaScript: moeda, estoque, classificação opcional e combinação de filtros.
- Navegador: cadastro com duas imagens, galeria, edição, remoção de foto, filtros e tabela.
- Celular: grade com duas colunas e verificação de ausência de rolagem horizontal.
- Dependências: auditoria NuGet incluindo dependências transitivas.

O workflow está preparado nesta cópia, mas só será executado no GitHub quando esta versão for escolhida e enviada ao repositório.

## Próximos estudos

1. Variantes por tamanho/cor e estoque por variante.
2. Histórico de entradas e saídas.
3. Paginação e filtros na API.
4. Autenticação e autorização.
5. Outro banco relacional, mantendo os contratos da tela.

A auditoria de desempenho motivou a geração de miniaturas de 480 px e a compressão explícita com perdas no WebP. As primeiras imagens têm prioridade de carregamento; as demais são carregadas conforme necessário. Isso evita transferir a foto grande para cada item da grade.


## Resultado da verificação final

- 29 testes de integração aprovados em Release e 6 testes JavaScript aprovados.
- Relatório Lighthouse local, perfil móvel: desempenho 100/100, acessibilidade 100/100 e LCP de 1,6 s.
- A transferência total medida caiu de 8.823 KiB para aproximadamente 92 KiB após miniaturas e compressão. São medições de uma execução local, não uma garantia para hospedagem futura.
- Relatório completo: artifacts/lighthouse-final.json. O encerramento do Chrome pelo CLI apresentou um erro de limpeza de pasta temporária no Windows após gerar o relatório; os dados do relatório não apresentam erro de execução.
- Seis exemplos fictícios preservados; a peça temporária de teste foi removida.

