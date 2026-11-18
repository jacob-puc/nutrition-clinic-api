using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nutriclinica_backend.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCuentasPaciente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CuentasPaciente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Correo = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: true),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    GoogleId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CorreoVerificado = table.Column<bool>(type: "boolean", nullable: false),
                    NombreRegistro = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TelefonoRegistro = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    SexoRegistro = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CodigoHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CodigoProposito = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CodigoExpira = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CodigoIntentos = table.Column<int>(type: "integer", nullable: false),
                    CodigoEnviadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefreshToken = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RefreshTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UltimoAcceso = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentasPaciente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CuentasPaciente_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CuentasPaciente_Correo",
                table: "CuentasPaciente",
                column: "Correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CuentasPaciente_GoogleId",
                table: "CuentasPaciente",
                column: "GoogleId",
                unique: true,
                filter: "\"GoogleId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasPaciente_PacienteId",
                table: "CuentasPaciente",
                column: "PacienteId",
                unique: true,
                filter: "\"PacienteId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasPaciente_RefreshToken",
                table: "CuentasPaciente",
                column: "RefreshToken");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CuentasPaciente");
        }
    }
}
