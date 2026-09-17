using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace DropMonAPI.Services;

// Armazena mídia fora de wwwroot e publica somente arquivos gerados pela aplicação.
public sealed class FotoStorage(IWebHostEnvironment environment, IConfiguration configuration, ILogger<FotoStorage> logger)
{
    public string Root { get; } = Path.GetFullPath(configuration["Storage:RootPath"]
        ?? Path.Combine(environment.ContentRootPath, "App_Data", "uploads"));

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
                imagem.Mutate(x => x.Resize(new ResizeOptions {
                    Mode = ResizeMode.Max, Size = new Size(1600, 1600)
                }));
            Directory.CreateDirectory(Root);
            var nome = Guid.NewGuid().ToString("N") + ".webp";
            var destino = Path.Combine(Root, nome);
            try
            {
                var encoder = new WebpEncoder { Quality = 82, FileFormat = WebpFileFormatType.Lossy, SkipMetadata = true };
                await imagem.SaveAsync(destino, encoder, ct);
                if (imagem.Width > 480 || imagem.Height > 480)
                    imagem.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(480, 480) }));
                await imagem.SaveAsync(destino[..^5] + "-thumb.webp", encoder, ct);
            }
            catch { File.Delete(destino); File.Delete(destino[..^5] + "-thumb.webp"); throw; }
            return "/media/" + nome;
        }
        catch (UnknownImageFormatException) { throw new ArgumentException("O arquivo não é uma imagem válida."); }
        catch (InvalidImageContentException) { throw new ArgumentException("A imagem está corrompida ou é inválida."); }
    }

    public string? Resolver(string arquivo) =>
        arquivo.EndsWith(".webp", StringComparison.Ordinal) &&
        Guid.TryParseExact(arquivo.EndsWith("-thumb.webp", StringComparison.Ordinal) ? arquivo[..^11] : arquivo[..^5], "N", out _)
        ? Path.Combine(Root, arquivo) : null;

    public void Excluir(string url)
    {
        if (!url.StartsWith("/media/", StringComparison.Ordinal)) return;
        var path = Resolver(url[7..]);
        if (path is null) return;
        try { File.Delete(path); File.Delete(path[..^5] + "-thumb.webp"); }
        catch (IOException ex) { logger.LogWarning(ex, "Não foi possível remover mídia órfã {Path}", path); }
        catch (UnauthorizedAccessException ex) { logger.LogWarning(ex, "Não foi possível remover mídia órfã {Path}", path); }
    }
}
