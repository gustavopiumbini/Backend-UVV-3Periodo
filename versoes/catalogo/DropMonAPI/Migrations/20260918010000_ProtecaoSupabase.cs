using DropMonAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DropMonAPI.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260918010000_ProtecaoSupabase")]
public sealed class ProtecaoSupabase : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (!ActiveProvider.Contains("Npgsql", StringComparison.Ordinal)) return;

        migrationBuilder.Sql("""
            ALTER TABLE "Produtos" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "ProdutoFoto" ENABLE ROW LEVEL SECURITY;
            REVOKE ALL ON TABLE "Produtos", "ProdutoFoto" FROM anon, authenticated;
            REVOKE ALL ON SEQUENCE "Produtos_Id_seq", "ProdutoFoto_Id_seq" FROM anon, authenticated;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (!ActiveProvider.Contains("Npgsql", StringComparison.Ordinal)) return;
        migrationBuilder.Sql("""
            ALTER TABLE "Produtos" DISABLE ROW LEVEL SECURITY;
            ALTER TABLE "ProdutoFoto" DISABLE ROW LEVEL SECURITY;
            """);
    }
}
