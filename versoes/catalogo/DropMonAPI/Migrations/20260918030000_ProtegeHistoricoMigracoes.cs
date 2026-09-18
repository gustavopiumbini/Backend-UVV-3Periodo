using DropMonAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DropMonAPI.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260918030000_ProtegeHistoricoMigracoes")]
public sealed class ProtegeHistoricoMigracoes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (!ActiveProvider.Contains("Npgsql", StringComparison.Ordinal)) return;

        migrationBuilder.Sql("""
            ALTER TABLE "__EFMigrationsHistory" ENABLE ROW LEVEL SECURITY;
            REVOKE ALL ON TABLE "__EFMigrationsHistory" FROM anon, authenticated;
            CREATE POLICY deny_api_access ON "__EFMigrationsHistory"
                FOR ALL TO PUBLIC USING (false) WITH CHECK (false);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (!ActiveProvider.Contains("Npgsql", StringComparison.Ordinal)) return;

        migrationBuilder.Sql("""
            DROP POLICY IF EXISTS deny_api_access ON "__EFMigrationsHistory";
            ALTER TABLE "__EFMigrationsHistory" DISABLE ROW LEVEL SECURITY;
            """);
    }
}
