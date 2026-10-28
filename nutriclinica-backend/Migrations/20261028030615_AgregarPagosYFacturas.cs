using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nutriclinica_backend.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPagosYFacturas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DatosFiscalesClinica",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RazonSocial = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RFC = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    RegimenFiscal = table.Column<string>(type: "text", nullable: false),
                    CodigoPostal = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Correo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Serie = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TarifaIva = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatosFiscalesClinica", x => x.Id);
                    table.CheckConstraint("CK_DatosFiscalesClinica_RegimenFiscal", "\"RegimenFiscal\" IN ('601','603','605','606','607','608','610','611','612','614','615','616','621','622','623','624','625','626')");
                    table.CheckConstraint("CK_DatosFiscalesClinica_TarifaIva", "\"TarifaIva\" >= 0 AND \"TarifaIva\" <= 100");
                });

            migrationBuilder.CreateTable(
                name: "Pagos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsultaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    MetodoPago = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    NumeroRecibo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaPago = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notas = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.Id);
                    table.CheckConstraint("CK_Pagos_Estado", "\"Estado\" IN ('Pagado','Anulado')");
                    table.CheckConstraint("CK_Pagos_MetodoPago", "\"MetodoPago\" IN ('Efectivo','TarjetaDebito','TarjetaCredito','Transferencia','QR','Otro')");
                    table.CheckConstraint("CK_Pagos_Monto", "\"Monto\" > 0");
                    table.ForeignKey(
                        name: "FK_Pagos_Consultas_ConsultaId",
                        column: x => x.ConsultaId,
                        principalTable: "Consultas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pagos_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Facturas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PagoId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoPersona = table.Column<string>(type: "text", nullable: false),
                    RFC = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    NombreRazonSocial = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RegimenFiscal = table.Column<string>(type: "text", nullable: false),
                    UsoCfdi = table.Column<string>(type: "text", nullable: false),
                    CodigoPostal = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Serie = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Folio = table.Column<int>(type: "integer", nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UUID = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Subtotal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    TarifaIva = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Iva = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Estatus = table.Column<string>(type: "text", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facturas", x => x.Id);
                    table.CheckConstraint("CK_Facturas_Estatus", "\"Estatus\" IN ('Borrador','Emitida','Anulada')");
                    table.CheckConstraint("CK_Facturas_RegimenFiscal", "\"RegimenFiscal\" IN ('601','603','605','606','607','608','610','611','612','614','615','616','621','622','623','624','625','626')");
                    table.CheckConstraint("CK_Facturas_TipoPersona", "\"TipoPersona\" IN ('Fisica','Moral')");
                    table.CheckConstraint("CK_Facturas_Totales", "\"Subtotal\" >= 0 AND \"Iva\" >= 0 AND \"Total\" >= 0");
                    table.CheckConstraint("CK_Facturas_UsoCfdi", "\"UsoCfdi\" IN ('G01','G02','G03','I01','I02','I03','I04','I05','I06','I07','I08','D01','D02','D03','D04','D06','D07','D08','D09','D10','S01','CP01','CN01')");
                    table.ForeignKey(
                        name: "FK_Facturas_Pagos_PagoId",
                        column: x => x.PagoId,
                        principalTable: "Pagos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_FechaEmision",
                table: "Facturas",
                column: "FechaEmision");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_IsActive",
                table: "Facturas",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_PagoId",
                table: "Facturas",
                column: "PagoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_RFC",
                table: "Facturas",
                column: "RFC");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_ConsultaId",
                table: "Pagos",
                column: "ConsultaId",
                unique: true,
                filter: "\"Estado\" = 'Pagado'");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_FechaPago",
                table: "Pagos",
                column: "FechaPago");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_IsActive",
                table: "Pagos",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_NumeroRecibo",
                table: "Pagos",
                column: "NumeroRecibo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_PacienteId",
                table: "Pagos",
                column: "PacienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DatosFiscalesClinica");

            migrationBuilder.DropTable(
                name: "Facturas");

            migrationBuilder.DropTable(
                name: "Pagos");
        }
    }
}
