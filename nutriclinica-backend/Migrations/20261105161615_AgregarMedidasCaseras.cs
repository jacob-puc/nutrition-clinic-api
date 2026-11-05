using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nutriclinica_backend.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMedidasCaseras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MedidaCantidad",
                table: "DetallesPlato",
                type: "numeric(7,2)",
                precision: 7,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedidaNombre",
                table: "DetallesPlato",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MedidasCaseras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlimentoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Gramos = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedidasCaseras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedidasCaseras_Alimentos_AlimentoId",
                        column: x => x.AlimentoId,
                        principalTable: "Alimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedidasCaseras_AlimentoId_Nombre",
                table: "MedidasCaseras",
                columns: new[] { "AlimentoId", "Nombre" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedidasCaseras");

            migrationBuilder.DropColumn(
                name: "MedidaCantidad",
                table: "DetallesPlato");

            migrationBuilder.DropColumn(
                name: "MedidaNombre",
                table: "DetallesPlato");
        }
    }
}
