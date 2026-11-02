using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nutriclinica_backend.Migrations
{
    /// <inheritdoc />
    public partial class AgregarRolesUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DebeCambiarContrasena",
                table: "Nutricionistas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Rol",
                table: "Nutricionistas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Nutricionista");

            /* Antes de los roles toda cuenta tenia acceso completo: las que ya
               existen quedan como administradoras para no quitarle permisos a
               nadie. Desde aqui el administrador asigna el rol de cada alta. */
            migrationBuilder.Sql("UPDATE \"Nutricionistas\" SET \"Rol\" = 'Administrador';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DebeCambiarContrasena",
                table: "Nutricionistas");

            migrationBuilder.DropColumn(
                name: "Rol",
                table: "Nutricionistas");
        }
    }
}
