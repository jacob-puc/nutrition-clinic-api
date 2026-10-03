using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nutriclinica_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddObjetivoPaciente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TituloObjetivo",
                table: "Pacientes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TituloObjetivo",
                table: "Pacientes");
        }
    }
}
