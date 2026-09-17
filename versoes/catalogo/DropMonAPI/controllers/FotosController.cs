using DropMonAPI.Contracts;
using DropMonAPI.Data;
using DropMonAPI.Models;
using DropMonAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DropMonAPI.Controllers;

[ApiController]
public class FotosController(AppDbContext context, FotoStorage storage) : ControllerBase
{
    [HttpPost("api/produtos/{id:int}/fotos")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<ActionResult<FotoResponse>> Enviar(int id, IFormFile arquivo, CancellationToken ct)
    {
        // A transação serializa a contagem e a inclusão no SQLite, inclusive em envios simultâneos.
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        var produto = await context.Produtos.Include(p => p.Fotos).SingleOrDefaultAsync(p => p.Id == id, ct);
        if (produto is null) return NotFound();
        if (produto.Fotos.Count >= 4) return Problem(statusCode: 400, detail: "Cada produto pode ter até 4 fotos.");
        string url;
        try { url = await storage.SalvarAsync(arquivo, ct); }
        catch (ArgumentException ex) { return Problem(statusCode: 400, detail: ex.Message); }
        var foto = new ProdutoFoto { ProdutoId = id, Url = url };
        try
        {
            context.Add(foto);
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch { storage.Excluir(url); throw; }
        return Created(url, new FotoResponse(foto.Id, foto.Url));
    }

    [HttpDelete("api/produtos/{id:int}/fotos/{fotoId:int}")]
    public async Task<IActionResult> Excluir(int id, int fotoId, CancellationToken ct)
    {
        var foto = await context.Set<ProdutoFoto>().SingleOrDefaultAsync(f => f.Id == fotoId && f.ProdutoId == id, ct);
        if (foto is null) return NotFound();
        context.Remove(foto);
        await context.SaveChangesAsync(ct);
        storage.Excluir(foto.Url);
        return NoContent();
    }

    [HttpGet("media/{arquivo}")]
    public IActionResult Ler(string arquivo)
    {
        var caminho = storage.Resolver(arquivo);
        if (caminho is null || !System.IO.File.Exists(caminho)) return NotFound();
        Response.Headers.CacheControl = "public,max-age=86400";
        Response.Headers.XContentTypeOptions = "nosniff";
        return PhysicalFile(caminho, "image/webp");
    }
}
