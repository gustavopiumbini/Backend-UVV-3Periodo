using DropMonAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DropMonAPI.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260918020000_CorrecaoBooleanoPostgres")]
public sealed class CorrecaoBooleanoPostgres : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (!ActiveProvider.Contains("Npgsql", StringComparison.Ordinal)) return;

        migrationBuilder.Sql("""
            ALTER TABLE "Produtos"
            ALTER COLUMN "IsExclusivoDrop" TYPE boolean
            USING ("IsExclusivoDrop" <> 0);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (!ActiveProvider.Contains("Npgsql", StringComparison.Ordinal)) return;

        migrationBuilder.Sql("""
            ALTER TABLE "Produtos"
            ALTER COLUMN "IsExclusivoDrop" TYPE integer
            USING (CASE WHEN "IsExclusivoDrop" THEN 1 ELSE 0 END);
            """);
    }
}
