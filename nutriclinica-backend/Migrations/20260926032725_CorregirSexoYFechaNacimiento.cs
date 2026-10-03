using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nutriclinica_backend.Migrations
{
    /// <inheritdoc />
    public partial class CorregirSexoYFechaNacimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Debe ejecutarse antes de agregar CK_Pacientes_Sexo, de lo contrario
            // el ALTER TABLE ... ADD CONSTRAINT falla sobre los valores legacy.
            // Solo toca filas que ya violan el dominio del enum Sexo.
            migrationBuilder.Sql(
                @"UPDATE ""Pacientes""
                  SET ""Sexo"" = CASE
                        WHEN lower(btrim(""Sexo"")) IN ('masculino', 'm') THEN 'M'
                        WHEN lower(btrim(""Sexo"")) IN ('femenino', 'f') THEN 'F'
                        ELSE 'Otro'
                      END
                  WHERE ""Sexo"" IS NOT NULL
                    AND btrim(""Sexo"") NOT IN ('M', 'F', 'Otro');");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "FechaNacimiento",
                table: "Pacientes",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Pacientes_Sexo",
                table: "Pacientes",
                sql: "\"Sexo\" IN ('M','F','Otro')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Pacientes_Sexo",
                table: "Pacientes");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaNacimiento",
                table: "Pacientes",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);
        }
    }
}
