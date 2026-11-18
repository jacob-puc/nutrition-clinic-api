using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nutriclinica_backend.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCorreosSalientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SecuenciaCalendario",
                table: "Citas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CorreosSalientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Para = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ParaNombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Asunto = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CuerpoHtml = table.Column<string>(type: "text", nullable: false),
                    CuerpoTexto = table.Column<string>(type: "text", nullable: false),
                    Ics = table.Column<string>(type: "text", nullable: true),
                    IcsMetodo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Tipo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ReferenciaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Intentos = table.Column<int>(type: "integer", nullable: false),
                    ProximoIntento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UltimoError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Proveedor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorreosSalientes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CorreosSalientes_Estado_ProximoIntento",
                table: "CorreosSalientes",
                columns: new[] { "Estado", "ProximoIntento" });

            migrationBuilder.CreateIndex(
                name: "IX_CorreosSalientes_ReferenciaId",
                table: "CorreosSalientes",
                column: "ReferenciaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CorreosSalientes");

            migrationBuilder.DropColumn(
                name: "SecuenciaCalendario",
                table: "Citas");
        }
    }
}
