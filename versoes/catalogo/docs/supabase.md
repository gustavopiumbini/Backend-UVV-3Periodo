# Configuração manual do Supabase

O código não contém credenciais. A configuração é feita no painel do Supabase e nas variáveis de ambiente do Render.

## 1. Criar o administrador

No Supabase, abra **Authentication > Users** e crie um usuário com e-mail e senha. Copie o **User UID** desse usuário. Ele será o único autorizado a entrar no DropMon.

Em **Authentication > Providers > Email**, desative novos cadastros públicos. O DropMon não oferece tela de cadastro e compara o usuário autenticado com o UUID configurado no servidor.

## 2. Criar o bucket

Abra **Storage**, crie um bucket chamado `produtos` e mantenha-o **privado**.

Configure:

- tipos permitidos: `image/webp`;
- tamanho máximo: 5 MB;
- bucket público: desativado.

A API recebe JPG, PNG ou WebP, valida o conteúdo, remove metadados, converte para WebP e envia a imagem principal e a miniatura. O navegador acessa as fotos por uma rota autenticada da API, não diretamente pelo bucket.

Não é necessário criar policies de escrita no Storage. A chave secreta fica somente no backend e o bucket permanece fechado para clientes públicos.

## 3. Obter as chaves

Em **Settings > API Keys**, localize:

- Project URL;
- Publishable key, iniciada por `sb_publishable_`;
- Secret key, iniciada por `sb_secret_`.

Nunca coloque a Secret key no GitHub, no JavaScript ou em mensagens. Cadastre-a somente como variável protegida no Render.

## 4. Obter a conexão PostgreSQL

Clique em **Connect** no projeto e escolha **Session pooler**. O Render executa um container persistente em uma rede que pode depender de IPv4, por isso o session pooler na porta 5432 é a escolha compatível.

No Render, use uma connection string no formato Npgsql:

```text
Host=HOST_DO_SESSION_POOLER;Port=5432;Database=postgres;Username=postgres.PROJECT_REF;Password=SUA_SENHA;SSL Mode=Require;Trust Server Certificate=true;Maximum Pool Size=5;Minimum Pool Size=0
```

Copie host, usuário e senha do painel. Se a senha contiver ponto e vírgula, aspas ou outro caractere especial de connection string, gere uma nova senha forte sem esses caracteres ou escape o valor conforme o formato Npgsql.

## 5. Variáveis no Render

Abra o Web Service do DropMon e entre em **Environment**. Cadastre:

| Variável | Valor |
| --- | --- |
| `Database__Provider` | `Postgres` |
| `ConnectionStrings__DefaultConnection` | connection string Npgsql do passo anterior |
| `Database__MigrateOnStartup` | `true` |
| `Supabase__Url` | Project URL |
| `Supabase__PublishableKey` | chave `sb_publishable_...` |
| `Supabase__SecretKey` | chave `sb_secret_...` |
| `Supabase__StorageBucket` | `produtos` |
| `Admin__RequireAuthentication` | `true` |
| `Admin__UserId` | User UID do administrador |
| `Demo__Seed` | `true` para criar as seis peças fictícias no primeiro banco vazio |

Salve as variáveis e execute **Manual Deploy > Deploy latest commit**.

Na primeira inicialização, o Entity Framework cria as tabelas, habilita RLS, revoga acesso direto de `anon` e `authenticated` e insere a demonstração somente se o catálogo estiver vazio. A API acessa o PostgreSQL pela conexão de servidor; o navegador não acessa as tabelas pela Data API.

## 6. Conferência

Depois do deploy:

1. abrir a URL do Render deve mostrar somente a página de login;
2. credenciais erradas devem permanecer na página de login;
3. o usuário administrativo deve abrir catálogo e estoque;
4. criar um produto com foto deve gerar registros nas tabelas e dois objetos WebP no bucket;
5. sair deve bloquear novamente catálogo, API e fotos;
6. um novo deploy deve preservar os produtos e imagens.

Não cole chaves ou senha do banco em issues, commits, capturas de tela ou conversas. Se uma chave secreta for exposta, remova-a e crie outra no painel do Supabase.
