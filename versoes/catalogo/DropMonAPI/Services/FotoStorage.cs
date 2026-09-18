using System.Net.Http.Headers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace DropMonAPI.Services;

// Processa as imagens no backend e usa disco local ou Supabase Storage conforme a configuração.
public sealed class FotoStorage
{
    private readonly IHttpClientFactory clients;
    private readonly ILogger<FotoStorage> logger;
    private readonly string? supabaseUrl;
    private readonly string? secretKey;
    private readonly string bucket;

    public FotoStorage(IWebHostEnvironment environment, IConfiguration configuration,
        IHttpClientFactory clients, ILogger<FotoStorage> logger)
    {
        this.clients = clients;
        this.logger = logger;
        Root = Path.GetFullPath(configuration["Storage:RootPath"]
            ?? Path.Combine(environment.ContentRootPath, "App_Data", "uploads"));
        supabaseUrl = configuration["Supabase:Url"]?.Trim().TrimEnd('/');
        secretKey = configuration["Supabase:SecretKey"]?.Trim();
        bucket = configuration["Supabase:StorageBucket"]?.Trim() ?? "produtos";
        if (string.IsNullOrWhiteSpace(supabaseUrl) != string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException("Configure Supabase:Url e Supabase:SecretKey juntos.");
    }

    public string Root { get; }
    private bool Remoto => !string.IsNullOrWhiteSpace(supabaseUrl) && !string.IsNullOrWhiteSpace(secretKey);

    public async Task<string> SalvarAsync(IFormFile arquivo, CancellationToken ct)
    {
        if (arquivo.Length == 0 || arquivo.Length > 5 * 1024 * 1024)
            throw new ArgumentException("A foto deve ter até 5 MB e não pode estar vazia.");
        using var entrada = new MemoryStream();
        await arquivo.CopyToAsync(entrada, ct);
        entrada.Position = 0;
        try
        {
            var formato = await Image.DetectFormatAsync(entrada, ct);
            if (formato.Name.ToUpperInvariant() is not ("JPEG" or "PNG" or "WEBP"))
                throw new ArgumentException("Envie uma imagem JPG, PNG ou WebP.");
            entrada.Position = 0;
            var info = await Image.IdentifyAsync(entrada, ct);
            if ((long)info.Width * info.Height > 24_000_000)
                throw new ArgumentException("A imagem deve ter até 24 megapixels.");
            entrada.Position = 0;
            using var imagem = await Image.LoadAsync(new DecoderOptions { MaxFrames = 1 }, entrada, ct);
            imagem.Mutate(x => x.AutoOrient());
            if (imagem.Width > 1600 || imagem.Height > 1600)
                imagem.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(1600, 1600) }));

            // O plano gratuito do Render tem CPU limitada. O método rápido evita que fotos
            // grandes ultrapassem o tempo do pedido sem sacrificar a resolução do catálogo.
            var encoder = new WebpEncoder
            {
                Quality = 82,
                FileFormat = WebpFileFormatType.Lossy,
                Method = WebpEncodingMethod.Fastest,
                SkipMetadata = true
            };
            using var principal = new MemoryStream();
            await imagem.SaveAsync(principal, encoder, ct);
            if (imagem.Width > 480 || imagem.Height > 480)
                imagem.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(480, 480) }));
            using var miniatura = new MemoryStream();
            await imagem.SaveAsync(miniatura, encoder, ct);

            var nome = Guid.NewGuid().ToString("N") + ".webp";
            var thumb = nome[..^5] + "-thumb.webp";
            if (Remoto)
            {
                await EnviarSupabaseAsync(nome, principal.ToArray(), ct);
                try { await EnviarSupabaseAsync(thumb, miniatura.ToArray(), ct); }
                catch { await ExcluirSupabaseAsync([nome], ct, false); throw; }
                return "/media/" + nome;
            }

            Directory.CreateDirectory(Root);
            var destino = Path.Combine(Root, nome);
            try
            {
                await File.WriteAllBytesAsync(destino, principal.ToArray(), ct);
                await File.WriteAllBytesAsync(Path.Combine(Root, thumb), miniatura.ToArray(), ct);
            }
            catch { File.Delete(destino); File.Delete(Path.Combine(Root, thumb)); throw; }
            return "/media/" + nome;
        }
        catch (UnknownImageFormatException) { throw new ArgumentException("O arquivo não é uma imagem válida."); }
        catch (InvalidImageContentException) { throw new ArgumentException("A imagem está corrompida ou é inválida."); }
    }

    public string? Resolver(string arquivo)
    {
        if (Remoto || !arquivo.EndsWith(".webp", StringComparison.Ordinal) ||
            !Guid.TryParseExact(arquivo.EndsWith("-thumb.webp", StringComparison.Ordinal) ? arquivo[..^11] : arquivo[..^5], "N", out _))
            return null;
        return Path.Combine(Root, arquivo);
    }

    public async Task ExcluirAsync(string url, CancellationToken ct = default)
    {
        if (!url.StartsWith("/media/", StringComparison.Ordinal)) return;
        var nome = url[7..];
        if (!NomePrincipalValido(nome)) return;
        if (Remoto)
        {
            await ExcluirSupabaseAsync([nome, nome[..^5] + "-thumb.webp"], ct, true);
            return;
        }

        var path = Resolver(nome);
        if (path is null) return;
        try { File.Delete(path); File.Delete(path[..^5] + "-thumb.webp"); }
        catch (IOException ex) { logger.LogWarning(ex, "Não foi possível remover mídia órfã {Path}", path); }
        catch (UnauthorizedAccessException ex) { logger.LogWarning(ex, "Não foi possível remover mídia órfã {Path}", path); }
    }

    public async Task<byte[]?> LerAsync(string arquivo, CancellationToken ct)
    {
        if (!NomeArquivoValido(arquivo)) return null;
        if (!Remoto)
        {
            var path = Path.Combine(Root, arquivo);
            return File.Exists(path) ? await File.ReadAllBytesAsync(path, ct) : null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"{supabaseUrl}/storage/v1/object/{Uri.EscapeDataString(bucket)}/{arquivo}");
        request.Headers.TryAddWithoutValidation("apikey", secretKey);
        using var response = await clients.CreateClient().SendAsync(request, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"O Supabase Storage recusou a leitura ({(int)response.StatusCode}).");
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    private async Task EnviarSupabaseAsync(string nome, byte[] dados, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{supabaseUrl}/storage/v1/object/{Uri.EscapeDataString(bucket)}/{nome}");
        request.Headers.TryAddWithoutValidation("apikey", secretKey);
        request.Headers.TryAddWithoutValidation("x-upsert", "false");
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent(dados);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/webp");
        form.Add(new StringContent("31536000"), "cacheControl");
        form.Add(file, "file", nome);
        request.Content = form;
        using var response = await clients.CreateClient().SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"O Supabase Storage recusou a imagem ({(int)response.StatusCode}).");
    }

    private async Task ExcluirSupabaseAsync(string[] nomes, CancellationToken ct, bool apenasRegistrar)
    {
        try
        {
            foreach (var nome in nomes)
            {
                using var request = new HttpRequestMessage(HttpMethod.Delete,
                    $"{supabaseUrl}/storage/v1/object/{Uri.EscapeDataString(bucket)}/{nome}");
                request.Headers.TryAddWithoutValidation("apikey", secretKey);
                using var response = await clients.CreateClient().SendAsync(request, ct);
                if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
                    throw new InvalidOperationException($"O Supabase Storage recusou a exclusão ({(int)response.StatusCode}).");
            }
        }
        catch (Exception ex) when (apenasRegistrar && ex is HttpRequestException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Não foi possível remover uma mídia órfã do Supabase Storage.");
        }
    }

    private static bool NomePrincipalValido(string nome) => nome.Length == 37 && nome.EndsWith(".webp", StringComparison.Ordinal) &&
        Guid.TryParseExact(nome[..^5], "N", out _);

    private static bool NomeArquivoValido(string nome) => nome.EndsWith(".webp", StringComparison.Ordinal) &&
        Guid.TryParseExact(nome.EndsWith("-thumb.webp", StringComparison.Ordinal) ? nome[..^11] : nome[..^5], "N", out _);
}
