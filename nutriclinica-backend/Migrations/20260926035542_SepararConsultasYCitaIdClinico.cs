using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nutriclinica_backend.Migrations
{
    /// <inheritdoc />
    public partial class SepararConsultasYCitaIdClinico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notas",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "TipoConsulta",
                table: "Citas");

            migrationBuilder.RenameColumn(
                name: "CitaId",
                table: "MedidasAntropometricas",
                newName: "ConsultaId");

            migrationBuilder.RenameColumn(
                name: "CitaId",
                table: "FotosSeguimiento",
                newName: "ConsultaId");

            migrationBuilder.RenameColumn(
                name: "CitaId",
                table: "DocumentosPaciente",
                newName: "ConsultaId");

            migrationBuilder.CreateTable(
                name: "Consultas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    CitaId = table.Column<Guid>(type: "uuid", nullable: true),
                    NutricionistaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoConsulta = table.Column<string>(type: "text", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NotasClinicas = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consultas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Consultas_Citas_CitaId",
                        column: x => x.CitaId,
                        principalTable: "Citas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Consultas_Nutricionistas_NutricionistaId",
                        column: x => x.NutricionistaId,
                        principalTable: "Nutricionistas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Consultas_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedidasAntropometricas_ConsultaId",
                table: "MedidasAntropometricas",
                column: "ConsultaId");

            migrationBuilder.CreateIndex(
                name: "IX_FotosSeguimiento_ConsultaId",
                table: "FotosSeguimiento",
                column: "ConsultaId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosPaciente_ConsultaId",
                table: "DocumentosPaciente",
                column: "ConsultaId");

            migrationBuilder.CreateIndex(
                name: "IX_Consultas_CitaId",
                table: "Consultas",
                column: "CitaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Consultas_NutricionistaId",
                table: "Consultas",
                column: "NutricionistaId");

            migrationBuilder.CreateIndex(
                name: "IX_Consultas_PacienteId_FechaInicio",
                table: "Consultas",
                columns: new[] { "PacienteId", "FechaInicio" });

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentosPaciente_Consultas_ConsultaId",
                table: "DocumentosPaciente",
                column: "ConsultaId",
                principalTable: "Consultas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FotosSeguimiento_Consultas_ConsultaId",
                table: "FotosSeguimiento",
                column: "ConsultaId",
                principalTable: "Consultas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MedidasAntropometricas_Consultas_ConsultaId",
                table: "MedidasAntropometricas",
                column: "ConsultaId",
                principalTable: "Consultas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentosPaciente_Consultas_ConsultaId",
                table: "DocumentosPaciente");

            migrationBuilder.DropForeignKey(
                name: "FK_FotosSeguimiento_Consultas_ConsultaId",
                table: "FotosSeguimiento");

            migrationBuilder.DropForeignKey(
                name: "FK_MedidasAntropometricas_Consultas_ConsultaId",
                table: "MedidasAntropometricas");

            migrationBuilder.DropTable(
                name: "Consultas");

            migrationBuilder.DropIndex(
                name: "IX_MedidasAntropometricas_ConsultaId",
                table: "MedidasAntropometricas");

            migrationBuilder.DropIndex(
                name: "IX_FotosSeguimiento_ConsultaId",
                table: "FotosSeguimiento");

            migrationBuilder.DropIndex(
                name: "IX_DocumentosPaciente_ConsultaId",
                table: "DocumentosPaciente");

            migrationBuilder.RenameColumn(
                name: "ConsultaId",
                table: "MedidasAntropometricas",
                newName: "CitaId");

            migrationBuilder.RenameColumn(
                name: "ConsultaId",
                table: "FotosSeguimiento",
                newName: "CitaId");

            migrationBuilder.RenameColumn(
                name: "ConsultaId",
                table: "DocumentosPaciente",
                newName: "CitaId");

            migrationBuilder.AddColumn<string>(
                name: "Notas",
                table: "Citas",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoConsulta",
                table: "Citas",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
