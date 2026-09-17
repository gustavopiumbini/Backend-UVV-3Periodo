using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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


    [Fact]
    public async Task Catalogo_PersisteCaracteristicasOpcionais()
    {
        using var client = Client;
        var response = await client.PostAsJsonAsync("/api/produtos", new {
            nome="Peça",categoria="Camisetas",preco=99.90m,quantidadeEstoque=8,
            dropNome="  Drop verão  ",ano=2026,descricao="  Corte amplo  ",cor="Preto",material="Algodão"
        });
        Assert.Equal(HttpStatusCode.Created,response.StatusCode);
        var p = (await client.GetFromJsonAsync<ProdutoResponse>(response.Headers.Location))!;
        Assert.Equal("Drop verão",p.DropNome); Assert.Equal(2026,p.Ano);
        Assert.Equal("Corte amplo",p.Descricao); Assert.Equal("Preto",p.Cor); Assert.Equal("Algodão",p.Material);
        Assert.Empty(p.Fotos);
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2101)]
    public async Task AnoForaDoLimite_Retorna400(int ano)
    {
        using var client = Client;
        var response=await client.PostAsJsonAsync("/api/produtos",new {nome="Peça",categoria="A",preco=10,quantidadeEstoque=1,ano});
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
    }

    private static MultipartFormDataContent Foto(bool valida=true)
    {
        var form=new MultipartFormDataContent();
        byte[] bytes;
        if(valida){
            using var image=new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(20,20);
            using var stream=new MemoryStream();
            image.Save(stream,new SixLabors.ImageSharp.Formats.Png.PngEncoder());
            bytes=stream.ToArray();
        } else bytes=System.Text.Encoding.UTF8.GetBytes("<script>alert(1)</script>");
        var content=new ByteArrayContent(bytes);
        content.Headers.ContentType=new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        form.Add(content,"arquivo","foto.png");return form;
    }

    [Fact]
    public async Task Foto_ValidaArquivo_Limite_Persistencia_E_Limpeza()
    {
        using var client=Client;
        var created=await client.PostAsJsonAsync("/api/produtos",ProdutoValido);
        var p=(await created.Content.ReadFromJsonAsync<ProdutoResponse>())!;
        var endpoint="/api/produtos/"+p.Id+"/fotos";
        using(var falsa=Foto(false))
            Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsync(endpoint,falsa)).StatusCode);
        FotoResponse? primeira=null;
        for(var i=0;i<4;i++){
            using var form=Foto();
            var upload=await client.PostAsync(endpoint,form);
            Assert.Equal(HttpStatusCode.Created,upload.StatusCode);
            primeira??=await upload.Content.ReadFromJsonAsync<FotoResponse>();
        }
        using(var extra=Foto())Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsync(endpoint,extra)).StatusCode);
        var consulta=(await client.GetFromJsonAsync<ProdutoResponse>(created.Headers.Location))!;
        Assert.Equal(4,consulta.Fotos.Length);
        var media=await client.GetAsync(primeira!.Url);
        Assert.Equal(HttpStatusCode.OK,media.StatusCode);
        Assert.Equal("image/webp",media.Content.Headers.ContentType!.MediaType);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync(primeira.ThumbnailUrl)).StatusCode);
        using(var decoded=SixLabors.ImageSharp.Image.Load(await media.Content.ReadAsByteArrayAsync()))
            Assert.Equal(20,decoded.Width);

        Assert.Equal(HttpStatusCode.NotFound,(await client.DeleteAsync("/api/produtos/999/fotos/"+primeira.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync(endpoint+"/"+primeira.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync(primeira.Url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync(primeira.ThumbnailUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync("/api/produtos/"+p.Id)).StatusCode);
        foreach(var foto in consulta.Fotos)Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync(foto.Url)).StatusCode);
    }

    [Fact]
    public async Task Foto_ProdutoInexistente_NaoGravaArquivo()
    {
        using var client=Client;using var foto=Foto();
        Assert.Equal(HttpStatusCode.NotFound,(await client.PostAsync("/api/produtos/999/fotos",foto)).StatusCode);
    }


    [Theory]
    [InlineData("jpeg")]
    [InlineData("webp")]
    public async Task Fotos_AceitamFormatosDocumentados(string formato)
    {
        using var client=Client;
        var created=await client.PostAsJsonAsync("/api/produtos",ProdutoValido);
        var produto=(await created.Content.ReadFromJsonAsync<ProdutoResponse>())!;
        using var image=new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(16,16);
        using var stream=new MemoryStream();
        SixLabors.ImageSharp.Formats.IImageEncoder encoder=formato=="jpeg"
            ? new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder()
            : new SixLabors.ImageSharp.Formats.Webp.WebpEncoder();
        image.Save(stream,encoder);
        using var content=new MultipartFormDataContent();
        content.Add(new ByteArrayContent(stream.ToArray()),"arquivo","foto."+formato);
        var response=await client.PostAsync("/api/produtos/"+produto.Id+"/fotos",content);
        Assert.Equal(HttpStatusCode.Created,response.StatusCode);
    }

    [Fact]
    public async Task UploadExcessivo_ERejeitadoPeloFormulario()
    {
        using var client=Client;
        using var content=new MultipartFormDataContent();
        content.Add(new ByteArrayContent(new byte[7*1024*1024]),"arquivo","grande.png");
        // O TestServer não implementa o limite do Kestrel (413); o limite multipart produz 400.
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsync("/api/produtos/1/fotos",content)).StatusCode);
    }

    public void Dispose() => factory.Dispose();

    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly string uploadDirectory = Path.Combine(Path.GetTempPath(), "dropmon-uploads-" + Guid.NewGuid());
        private readonly string database = Path.Combine(Path.GetTempPath(), "dropmon-test-" + Guid.NewGuid() + ".db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            // Testes não dependem de permissões do Event Log do Windows.
            builder.ConfigureLogging(logging => logging.ClearProviders().AddConsole());
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DropMonAPI.Services.FotoStorage>();
                services.AddSingleton(sp => new DropMonAPI.Services.FotoStorage(sp.GetRequiredService<IWebHostEnvironment>(), new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:RootPath"] = uploadDirectory }).Build(), sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DropMonAPI.Services.FotoStorage>>()));
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite("Data Source=" + database + ";Pooling=False"));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && Directory.Exists(uploadDirectory)) { foreach (var file in Directory.GetFiles(uploadDirectory)) File.Delete(file); Directory.Delete(uploadDirectory); }
            if (disposing)
                foreach (var suffix in new[] { "", "-shm", "-wal" })
                    File.Delete(database + suffix);
        }
    }
}


