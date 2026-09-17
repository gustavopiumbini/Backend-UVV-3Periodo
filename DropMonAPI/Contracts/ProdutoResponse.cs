using DropMonAPI.Models;

namespace DropMonAPI.Contracts;

public record ProdutoResponse(int Id, string Nome, string Categoria, decimal Preco,
    int QuantidadeEstoque, bool IsExclusivoDrop)
{
    public static ProdutoResponse From(Produto produto) => new(produto.Id, produto.Nome,
        produto.Categoria, produto.Preco, produto.QuantidadeEstoque, produto.IsExclusivoDrop);
}
