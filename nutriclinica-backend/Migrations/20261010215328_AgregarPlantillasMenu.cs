using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nutriclinica_backend.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPlantillasMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlantillasMenu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ObjetivoCalorico = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    NutricionistaId = table.Column<Guid>(type: "uuid", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantillasMenu", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantillasMenu_Nutricionistas_NutricionistaId",
                        column: x => x.NutricionistaId,
                        principalTable: "Nutricionistas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Platos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ImagenUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    NutricionistaId = table.Column<Guid>(type: "uuid", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Platos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Platos_Nutricionistas_NutricionistaId",
                        column: x => x.NutricionistaId,
                        principalTable: "Nutricionistas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DetallesPlantillaMenu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantillaMenuId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlatoId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiaSemana = table.Column<string>(type: "text", nullable: false),
                    TiempoComida = table.Column<string>(type: "text", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Indicaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesPlantillaMenu", x => x.Id);
                    table.CheckConstraint("CK_DetallesPlantillaMenu_Cantidad", "\"Cantidad\" > 0");
                    table.CheckConstraint("CK_DetallesPlantillaMenu_DiaSemana", "\"DiaSemana\" IN ('Lunes','Martes','Miercoles','Jueves','Viernes','Sabado','Domingo')");
                    table.CheckConstraint("CK_DetallesPlantillaMenu_TiempoComida", "\"TiempoComida\" IN ('Desayuno','MediaManana','Almuerzo','Merienda','Cena','Colacion')");
                    table.ForeignKey(
                        name: "FK_DetallesPlantillaMenu_PlantillasMenu_PlantillaMenuId",
                        column: x => x.PlantillaMenuId,
                        principalTable: "PlantillasMenu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallesPlantillaMenu_Platos_PlatoId",
                        column: x => x.PlatoId,
                        principalTable: "Platos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetallesPlato",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlatoId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlimentoId = table.Column<Guid>(type: "uuid", nullable: false),
                    RacionGramos = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesPlato", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesPlato_Alimentos_AlimentoId",
                        column: x => x.AlimentoId,
                        principalTable: "Alimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesPlato_Platos_PlatoId",
                        column: x => x.PlatoId,
                        principalTable: "Platos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesPlantillaMenu_PlantillaMenuId_DiaSemana_TiempoComid~",
                table: "DetallesPlantillaMenu",
                columns: new[] { "PlantillaMenuId", "DiaSemana", "TiempoComida", "PlatoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetallesPlantillaMenu_PlatoId",
                table: "DetallesPlantillaMenu",
                column: "PlatoId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesPlato_AlimentoId",
                table: "DetallesPlato",
                column: "AlimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesPlato_PlatoId_AlimentoId",
                table: "DetallesPlato",
                columns: new[] { "PlatoId", "AlimentoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlantillasMenu_IsActive",
                table: "PlantillasMenu",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PlantillasMenu_Nombre",
                table: "PlantillasMenu",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_PlantillasMenu_NutricionistaId",
                table: "PlantillasMenu",
                column: "NutricionistaId");

            migrationBuilder.CreateIndex(
                name: "IX_Platos_IsActive",
                table: "Platos",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Platos_Nombre",
                table: "Platos",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_Platos_NutricionistaId",
                table: "Platos",
                column: "NutricionistaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetallesPlantillaMenu");

            migrationBuilder.DropTable(
                name: "DetallesPlato");

            migrationBuilder.DropTable(
                name: "PlantillasMenu");

            migrationBuilder.DropTable(
                name: "Platos");
        }
    }
}
