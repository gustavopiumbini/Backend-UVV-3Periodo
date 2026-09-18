using DropMonAPI.Models;
using Microsoft.EntityFrameworkCore;
namespace DropMonAPI.Data;

public static class DemoData
{
    // Executado somente por opção explícita em Development e em um catálogo vazio.
    public static async Task SeedAsync(AppDbContext db, DropMonAPI.Services.FotoStorage storage, string contentRoot)
    {
        if (await db.Produtos.AnyAsync()) return;
        var dados = new[] {
            ("TS-01", "Camisetas", 149.90m, 32, "ESSENTIALS", (int?)2026, "camiseta", "Carvão", "Algodão", "Camiseta de modelagem ampla, ombros deslocados e acabamento lavado."),
            ("HD-01", "Moletons", 329.90m, 18, "ESSENTIALS", (int?)2026, "moletom", "Preto", "Moletom de algodão", "Moletom com capuz, bolso frontal e modelagem oversized."),
            ("PT-01", "Calças", 289.90m, 7, "UTILITY", (int?)2025, "calca", "Grafite", "Sarja de algodão", "Calça cargo de corte reto e amplo com bolsos utilitários."),
            ("SH-01", "Bermudas", 169.90m, 24, "ESSENTIALS", (int?)2026, "bermuda", "Off-white", "Algodão", "Bermuda ampla com cintura elástica e toque encorpado."),
            ("LS-01", "Camisetas", 189.90m, 0, "ARCHIVE", (int?)2024, "longsleeve", "Cinza mescla", "Algodão", "Camiseta de manga longa com punhos leves e corte amplo."),
            ("CP-01", "Acessórios", 99.90m, 42, (string?)null, (int?)null, "bone", "Preto", "Algodão", "Boné de seis painéis com aba curva e ajuste traseiro.")
        };
        var arquivos = new List<string>();
        try
        {
        foreach (var (nome, categoria, preco, estoque, drop, ano, foto, cor, material, descricao) in dados)
        {
            await using var source = File.OpenRead(Path.Combine(contentRoot, "DemoAssets", foto + ".png"));
            var upload = new FormFile(source, 0, source.Length, "arquivo", foto + ".png");
            var url = await storage.SalvarAsync(upload, CancellationToken.None);
            arquivos.Add(url);
            db.Produtos.Add(new Produto {
                Nome = nome, Categoria = categoria, Preco = preco, QuantidadeEstoque = estoque,
                DropNome = drop, Ano = ano, Cor = cor, Material = material,
                Descricao = descricao + " Produto fictício para demonstração.",
                IsExclusivoDrop = drop == "UTILITY",
                Fotos = [new ProdutoFoto { Url = url }]
            });
        }
        await db.SaveChangesAsync();
        }
        catch { foreach (var url in arquivos) await storage.ExcluirAsync(url); throw; }
    }
}
