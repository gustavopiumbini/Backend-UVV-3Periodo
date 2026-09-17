using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DropMonAPI.Migrations
{
    /// <inheritdoc />
    public partial class CatalogoComFotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Ano",
                table: "Produtos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cor",
                table: "Produtos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Descricao",
                table: "Produtos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropNome",
                table: "Produtos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Material",
                table: "Produtos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProdutoFoto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProdutoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutoFoto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProdutoFoto_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProdutoFoto_ProdutoId",
                table: "ProdutoFoto",
                column: "ProdutoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProdutoFoto");

            migrationBuilder.DropColumn(
                name: "Ano",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "Cor",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "Descricao",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "DropNome",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "Material",
                table: "Produtos");
        }
    }
}
