using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nutriclinica_backend.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMenusAsignados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MenusAsignados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantillaMenuId = table.Column<Guid>(type: "uuid", nullable: true),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ObjetivoCalorico = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenusAsignados", x => x.Id);
                    table.CheckConstraint("CK_MenusAsignados_Estado", "\"Estado\" IN ('Borrador','Activo','Finalizado')");
                    table.CheckConstraint("CK_MenusAsignados_ObjetivoCalorico", "\"ObjetivoCalorico\" IS NULL OR \"ObjetivoCalorico\" > 0");
                    table.ForeignKey(
                        name: "FK_MenusAsignados_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MenusAsignados_PlantillasMenu_PlantillaMenuId",
                        column: x => x.PlantillaMenuId,
                        principalTable: "PlantillasMenu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DetallesMenuAsignado",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuAsignadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlatoId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiaSemana = table.Column<string>(type: "text", nullable: false),
                    TiempoComida = table.Column<string>(type: "text", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Indicaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesMenuAsignado", x => x.Id);
                    table.CheckConstraint("CK_DetallesMenuAsignado_Cantidad", "\"Cantidad\" > 0");
                    table.CheckConstraint("CK_DetallesMenuAsignado_DiaSemana", "\"DiaSemana\" IN ('Lunes','Martes','Miercoles','Jueves','Viernes','Sabado','Domingo')");
                    table.CheckConstraint("CK_DetallesMenuAsignado_TiempoComida", "\"TiempoComida\" IN ('Desayuno','MediaManana','Almuerzo','Merienda','Cena','Colacion')");
                    table.ForeignKey(
                        name: "FK_DetallesMenuAsignado_MenusAsignados_MenuAsignadoId",
                        column: x => x.MenuAsignadoId,
                        principalTable: "MenusAsignados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallesMenuAsignado_Platos_PlatoId",
                        column: x => x.PlatoId,
                        principalTable: "Platos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesMenuAsignado_MenuAsignadoId_DiaSemana_TiempoComida_~",
                table: "DetallesMenuAsignado",
                columns: new[] { "MenuAsignadoId", "DiaSemana", "TiempoComida", "PlatoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetallesMenuAsignado_PlatoId",
                table: "DetallesMenuAsignado",
                column: "PlatoId");

            migrationBuilder.CreateIndex(
                name: "IX_MenusAsignados_Estado",
                table: "MenusAsignados",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_MenusAsignados_FechaInicio",
                table: "MenusAsignados",
                column: "FechaInicio");

            migrationBuilder.CreateIndex(
                name: "IX_MenusAsignados_IsActive",
                table: "MenusAsignados",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MenusAsignados_PacienteId",
                table: "MenusAsignados",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_MenusAsignados_PlantillaMenuId",
                table: "MenusAsignados",
                column: "PlantillaMenuId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetallesMenuAsignado");

            migrationBuilder.DropTable(
                name: "MenusAsignados");
        }
    }
}
