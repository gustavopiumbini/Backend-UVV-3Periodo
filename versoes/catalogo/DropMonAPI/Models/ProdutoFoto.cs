namespace DropMonAPI.Models;

public class ProdutoFoto
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;
    public string Url { get; set; } = string.Empty;
}
