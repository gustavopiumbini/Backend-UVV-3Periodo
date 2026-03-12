namespace DropMonAPI.Models
{
    public class Produto
    {
        // O Entity Framework entende automaticamente que a propriedade "Id" será a nossa Primary Key (Chave Primária) no Banco de Dados.
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Categoria { get; set; } = string.Empty;

        public decimal Preco { get; set; }

        public int QuantidadeEstoque { get; set; }

        public bool IsExclusivoDrop { get; set; }
    }
}