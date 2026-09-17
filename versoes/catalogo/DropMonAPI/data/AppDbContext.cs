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
    }
}