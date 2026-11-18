using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nutriclinica_backend.Migrations
{
    /// <inheritdoc />
    public partial class AgregarAdjuntosCorreo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EnviadoPorId",
                table: "CorreosSalientes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CorreosAdjuntos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorreoSalienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TipoContenido = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Contenido = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorreosAdjuntos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorreosAdjuntos_CorreosSalientes_CorreoSalienteId",
                        column: x => x.CorreoSalienteId,
                        principalTable: "CorreosSalientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CorreosAdjuntos_CorreoSalienteId",
                table: "CorreosAdjuntos",
                column: "CorreoSalienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CorreosAdjuntos");

            migrationBuilder.DropColumn(
                name: "EnviadoPorId",
                table: "CorreosSalientes");
        }
    }
}
