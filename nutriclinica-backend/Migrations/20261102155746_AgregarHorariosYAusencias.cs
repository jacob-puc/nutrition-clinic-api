using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nutriclinica_backend.Migrations
{
    /// <inheritdoc />
    public partial class AgregarHorariosYAusencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DuracionConsultaMinutos",
                table: "Nutricionistas",
                type: "integer",
                nullable: false,
                defaultValue: 60);

            migrationBuilder.AddColumn<bool>(
                name: "HorarioPropio",
                table: "Nutricionistas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Ausencias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NutricionistaId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaFin = table.Column<DateOnly>(type: "date", nullable: false),
                    Motivo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ausencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ausencias_Nutricionistas_NutricionistaId",
                        column: x => x.NutricionistaId,
                        principalTable: "Nutricionistas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HorariosAtencion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NutricionistaId = table.Column<Guid>(type: "uuid", nullable: true),
                    DiaSemana = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    HoraFin = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosAtencion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HorariosAtencion_Nutricionistas_NutricionistaId",
                        column: x => x.NutricionistaId,
                        principalTable: "Nutricionistas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ausencias_NutricionistaId_FechaInicio_FechaFin",
                table: "Ausencias",
                columns: new[] { "NutricionistaId", "FechaInicio", "FechaFin" });

            migrationBuilder.CreateIndex(
                name: "IX_HorariosAtencion_NutricionistaId_DiaSemana",
                table: "HorariosAtencion",
                columns: new[] { "NutricionistaId", "DiaSemana" });

            /* Horario general inicial: lunes a sabado de 8:00 a 20:00, el mismo
               rango que la agenda permitia antes. El administrador lo ajusta. */
            migrationBuilder.Sql(@"
                INSERT INTO ""HorariosAtencion"" (""Id"", ""NutricionistaId"", ""DiaSemana"", ""HoraInicio"", ""HoraFin"")
                VALUES
                    (gen_random_uuid(), NULL, 'Lunes', TIME '08:00', TIME '20:00'),
                    (gen_random_uuid(), NULL, 'Martes', TIME '08:00', TIME '20:00'),
                    (gen_random_uuid(), NULL, 'Miercoles', TIME '08:00', TIME '20:00'),
                    (gen_random_uuid(), NULL, 'Jueves', TIME '08:00', TIME '20:00'),
                    (gen_random_uuid(), NULL, 'Viernes', TIME '08:00', TIME '20:00'),
                    (gen_random_uuid(), NULL, 'Sabado', TIME '08:00', TIME '20:00');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ausencias");

            migrationBuilder.DropTable(
                name: "HorariosAtencion");

            migrationBuilder.DropColumn(
                name: "DuracionConsultaMinutos",
                table: "Nutricionistas");

            migrationBuilder.DropColumn(
                name: "HorarioPropio",
                table: "Nutricionistas");
        }
    }
}
