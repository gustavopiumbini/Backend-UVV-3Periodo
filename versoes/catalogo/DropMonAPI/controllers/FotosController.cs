using DropMonAPI.Contracts;
using DropMonAPI.Data;
using DropMonAPI.Models;
using DropMonAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DropMonAPI.Controllers;

[ApiController]
[Authorize(Policy = "Admin")]
public class FotosController(AppDbContext context, FotoStorage storage) : ControllerBase
{
    [HttpPost("api/produtos/{id:int}/fotos")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<ActionResult<FotoResponse>> Enviar(int id, IFormFile arquivo, CancellationToken ct)
    {
        // Serializa a contagem e a inclusão para respeitar o limite em SQLite e PostgreSQL.
        await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
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
        catch { await storage.ExcluirAsync(url, ct); throw; }
        return Created(url, new FotoResponse(foto.Id, foto.Url));
    }

    [HttpDelete("api/produtos/{id:int}/fotos/{fotoId:int}")]
    public async Task<IActionResult> Excluir(int id, int fotoId, CancellationToken ct)
    {
        var foto = await context.Set<ProdutoFoto>().SingleOrDefaultAsync(f => f.Id == fotoId && f.ProdutoId == id, ct);
        if (foto is null) return NotFound();
        context.Remove(foto);
        await context.SaveChangesAsync(ct);
        await storage.ExcluirAsync(foto.Url, ct);
        return NoContent();
    }

    [HttpGet("media/{arquivo}")]
    public async Task<IActionResult> Ler(string arquivo, CancellationToken ct)
    {
        var conteudo = await storage.LerAsync(arquivo, ct);
        if (conteudo is null) return NotFound();
        Response.Headers.CacheControl = "private,max-age=86400";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(conteudo, "image/webp");
    }
}
