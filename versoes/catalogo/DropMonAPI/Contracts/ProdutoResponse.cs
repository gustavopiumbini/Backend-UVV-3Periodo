using DropMonAPI.Models;
namespace DropMonAPI.Contracts;

public record FotoResponse(int Id, string Url)
{
    public string ThumbnailUrl => Url[..^5] + "-thumb.webp";
}
public record ProdutoResponse(int Id, string Nome, string Categoria, decimal Preco,
    int QuantidadeEstoque, bool IsExclusivoDrop, string? DropNome, int? Ano,
    string? Descricao, string? Cor, string? Material, FotoResponse[] Fotos)
{
    public static ProdutoResponse From(Produto p) => new(p.Id, p.Nome, p.Categoria, p.Preco,
        p.QuantidadeEstoque, p.IsExclusivoDrop, p.DropNome, p.Ano, p.Descricao, p.Cor, p.Material,
        p.Fotos.OrderBy(f => f.Id).Select(f => new FotoResponse(f.Id, f.Url)).ToArray());
}
