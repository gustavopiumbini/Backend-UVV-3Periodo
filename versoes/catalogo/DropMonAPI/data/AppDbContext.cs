using Microsoft.EntityFrameworkCore;
using DropMonAPI.Models;

namespace DropMonAPI.Data
{
    // Nós herdamos ( : ) da classe DbContext, que já vem pronta da Microsoft com todos os poderes de banco de dados.
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Esta linha é crucial! Ela diz: "Crie uma tabela no banco de dados chamada 'Produtos' usando a estrutura da classe 'Produto'".
        public DbSet<Produto> Produtos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var produto = modelBuilder.Entity<Produto>();
            produto.Property(p => p.Nome).HasMaxLength(120);
            produto.Property(p => p.Categoria).HasMaxLength(60);
            produto.Property(p => p.Preco).HasPrecision(12, 2);
            produto.Property(p => p.DropNome).HasMaxLength(80);
            produto.Property(p => p.Descricao).HasMaxLength(1500);
            produto.Property(p => p.Cor).HasMaxLength(60);
            produto.Property(p => p.Material).HasMaxLength(120);
            modelBuilder.Entity<ProdutoFoto>().Property(f => f.Url).HasMaxLength(500);
        }
    }
}
