using DropMonAPI.Contracts;
using DropMonAPI.Data;
using DropMonAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DropMonAPI.Controllers;

[ApiController]
[Route("api/produtos")]
public class ProdutosController(AppDbContext context, DropMonAPI.Services.FotoStorage storage) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProdutoResponse>>> GetProdutos(CancellationToken cancellationToken)
    {
        var produtos = await context.Produtos.AsNoTracking().Include(p => p.Fotos)
            .OrderBy(p => p.Id).ToListAsync(cancellationToken);
        return Ok(produtos.Select(ProdutoResponse.From));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProdutoResponse>> GetProduto(int id, CancellationToken cancellationToken)
    {
        var produto = await context.Produtos.AsNoTracking().Include(p => p.Fotos)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        return produto is null ? NotFound() : Ok(ProdutoResponse.From(produto));
    }

    [HttpPost]
    public async Task<ActionResult<ProdutoResponse>> PostProduto(ProdutoRequest request, CancellationToken cancellationToken)
    {
        var produto = new Produto();
        AplicarDados(request, produto);
        context.Produtos.Add(produto);
        await context.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetProduto), new { id = produto.Id }, ProdutoResponse.From(produto));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutProduto(int id, ProdutoRequest request, CancellationToken cancellationToken)
    {
        var produto = await context.Produtos.FindAsync([id], cancellationToken);
        if (produto is null) return NotFound();
        AplicarDados(request, produto);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await context.Produtos.AnyAsync(p => p.Id == id, cancellationToken)) return NotFound();
            throw;
        }
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProduto(int id, CancellationToken cancellationToken)
    {
        var produto = await context.Produtos.Include(p => p.Fotos).SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (produto is null) return NotFound();
        var caminhos = produto.Fotos.Select(f => f.Url).ToArray();
        context.Produtos.Remove(produto);
        await context.SaveChangesAsync(cancellationToken);
        foreach (var caminho in caminhos) storage.Excluir(caminho);
        return NoContent();
    }

    private static void AplicarDados(ProdutoRequest request, Produto produto)
    {
        produto.Nome = request.Nome.Trim();
        produto.Categoria = request.Categoria.Trim();
        produto.Preco = request.Preco!.Value;
        produto.QuantidadeEstoque = request.QuantidadeEstoque!.Value;
        produto.IsExclusivoDrop = request.IsExclusivoDrop;
        produto.DropNome = Normalizar(request.DropNome);
        produto.Ano = request.Ano;
        produto.Descricao = Normalizar(request.Descricao);
        produto.Cor = Normalizar(request.Cor);
        produto.Material = Normalizar(request.Material);
    }
    private static string? Normalizar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
