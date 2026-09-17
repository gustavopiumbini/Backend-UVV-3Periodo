using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DropMonAPI.Contracts;
using DropMonAPI.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace DropMonAPI.Tests;

// Cada teste ganha seu próprio banco real, criado pelas migrations da aplicação.
public sealed class ProdutoApiTests : IDisposable
{
    private readonly ApiFactory factory = new();
    private HttpClient Client => factory.CreateClient();

    private static object ProdutoValido => new {
        nome = "  Camiseta tsey  ", categoria = "  Camisetas  ",
        preco = 1299.90m, quantidadeEstoque = 0, isExclusivoDrop = true
    };

    [Fact]
    public async Task CrudCompleto_PersisteDados_E_LocationConsultaProduto()
    {
        using var client = Client;
        var criacao = await client.PostAsJsonAsync("/api/produtos", ProdutoValido);
        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);
        var criado = (await criacao.Content.ReadFromJsonAsync<ProdutoResponse>())!;
        Assert.True(criado.Id > 0);
        Assert.Equal("Camiseta tsey", criado.Nome);
        Assert.Equal("Camisetas", criado.Categoria);
        Assert.Equal(1299.90m, criado.Preco);
        Assert.Equal(0, criado.QuantidadeEstoque);
        Assert.True(criado.IsExclusivoDrop);
        Assert.EndsWith("/api/produtos/" + criado.Id, criacao.Headers.Location!.ToString());

        var consulta = await client.GetFromJsonAsync<ProdutoResponse>(criacao.Headers.Location);
        Assert.Equal(criado, consulta);
        var lista = await client.GetFromJsonAsync<ProdutoResponse[]>("/api/produtos");
        Assert.Single(lista!);

        var alteracao = await client.PutAsJsonAsync("/api/produtos/" + criado.Id, new {
            nome = "Boné", categoria = "Acessórios", preco = 89.99m,
            quantidadeEstoque = 12, isExclusivoDrop = false
        });
        Assert.Equal(HttpStatusCode.NoContent, alteracao.StatusCode);
        // Outro cliente/requisição confirma persistência fora do contexto que gravou.
        using var outroClient = Client;
        var atualizado = (await outroClient.GetFromJsonAsync<ProdutoResponse>("/api/produtos/" + criado.Id))!;
        Assert.Equal("Boné", atualizado.Nome);
        Assert.Equal("Acessórios", atualizado.Categoria);
        Assert.Equal(89.99m, atualizado.Preco);
        Assert.Equal(12, atualizado.QuantidadeEstoque);
        Assert.False(atualizado.IsExclusivoDrop);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/produtos/" + criado.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/produtos/" + criado.Id)).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<ProdutoResponse[]>("/api/produtos"))!);
    }

    public static IEnumerable<object[]> DadosInvalidos()
    {
        yield return ["""{}"""];
        yield return ["""{"nome":" ","categoria":"A","preco":10,"quantidadeEstoque":1}"""];
        yield return ["""{"nome":"A","categoria":"","preco":10,"quantidadeEstoque":1}"""];
        yield return ["""{"nome":"A","categoria":"B","preco":-1,"quantidadeEstoque":1}"""];
        yield return ["""{"nome":"A","categoria":"B","preco":0,"quantidadeEstoque":1}"""];
        yield return ["""{"nome":"A","categoria":"B","preco":1000000,"quantidadeEstoque":1}"""];
        yield return ["""{"nome":"A","categoria":"B","preco":1.999,"quantidadeEstoque":1}"""];
        yield return ["""{"nome":"A","categoria":"B","quantidadeEstoque":1}"""];
        yield return ["""{"nome":"A","categoria":"B","preco":10}"""];
        yield return ["""{"nome":"A","categoria":"B","preco":10,"quantidadeEstoque":-1}"""];
        yield return ["""{"nome":"A","categoria":"B","preco":10,"quantidadeEstoque":1.5}"""];
        yield return ["""{"nome":null,"categoria":"B","preco":10,"quantidadeEstoque":1}"""];
        yield return ["""{"nome":"A","categoria":"B","preco":"abc","quantidadeEstoque":1}"""];
        yield return [JsonSerializer.Serialize(new { nome = new string('A', 121), categoria = "B", preco = 1, quantidadeEstoque = 0 })];
        yield return [JsonSerializer.Serialize(new { nome = "A", categoria = new string('B', 61), preco = 1, quantidadeEstoque = 0 })];
    }

    [Theory]
    [MemberData(nameof(DadosInvalidos))]
    public async Task CadastroInvalido_Retorna400_SemGravar(string json)
    {
        using var client = Client;
        var response = await client.PostAsync("/api/produtos", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(problem.RootElement.TryGetProperty("errors", out _));
        Assert.Empty((await client.GetFromJsonAsync<ProdutoResponse[]>("/api/produtos"))!);
    }

    [Fact]
    public async Task AlteracaoInvalida_PreservaProdutoOriginal()
    {
        using var client = Client;
        var criacao = await client.PostAsJsonAsync("/api/produtos", ProdutoValido);
        var original = await criacao.Content.ReadFromJsonAsync<ProdutoResponse>();
        var resposta = await client.PutAsJsonAsync(criacao.Headers.Location, new {
            nome = "Outro", categoria = "A", preco = -10, quantidadeEstoque = 1
        });
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(original, await client.GetFromJsonAsync<ProdutoResponse>(criacao.Headers.Location));
    }

    [Fact]
    public async Task RecursosInexistentes_Retornam404()
    {
        using var client = Client;
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/produtos/999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/produtos/999", ProdutoValido)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/api/produtos/999")).StatusCode);
    }

    [Fact]
    public async Task IdEnviadoPeloCliente_NaoDefineChaveDoBanco()
    {
        using var client = Client;
        var response = await client.PostAsJsonAsync("/api/produtos", new {
            id = 999, nome = "Boné", categoria = "A", preco = 10, quantidadeEstoque = 1
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotEqual(999, (await response.Content.ReadFromJsonAsync<ProdutoResponse>())!.Id);
    }

    [Fact]
    public async Task HtmlNoNome_PermaneceDado_E_NaoAlteraContrato()
    {
        using var client = Client;
        const string nome = "<img src=x onerror=alert(1)>";
        var response = await client.PostAsJsonAsync("/api/produtos", new {
            nome, categoria = "<b>Categoria</b>", preco = 10, quantidadeEstoque = 1
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(nome, (await client.GetFromJsonAsync<ProdutoResponse>(response.Headers.Location))!.Nome);
    }

    [Fact]
    public async Task OpenApi_DocumentaAsRotas()
    {
        using var client = Client;
        var response = await client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = doc.RootElement.GetProperty("paths");
        Assert.True(paths.GetProperty("/api/produtos").TryGetProperty("post", out _));
        Assert.True(paths.GetProperty("/api/produtos/{id}").TryGetProperty("put", out _));
        Assert.True(paths.GetProperty("/api/produtos/{id}").TryGetProperty("delete", out _));
    }

    public void Dispose() => factory.Dispose();

    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly string database = Path.Combine(Path.GetTempPath(), "dropmon-test-" + Guid.NewGuid() + ".db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite("Data Source=" + database + ";Pooling=False"));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                foreach (var suffix in new[] { "", "-shm", "-wal" })
                    File.Delete(database + suffix);
        }
    }
}
