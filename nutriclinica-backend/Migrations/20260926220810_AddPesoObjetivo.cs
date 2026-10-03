using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nutriclinica_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPesoObjetivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PesoObjetivo",
                table: "Pacientes",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PesoObjetivo",
                table: "Pacientes");
        }
    }
}
