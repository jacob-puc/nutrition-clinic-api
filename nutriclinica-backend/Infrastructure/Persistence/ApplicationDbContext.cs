using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<HistorialClinico> HistorialesClinicos => Set<HistorialClinico>();
    public DbSet<MedidaAntropometrica> MedidasAntropometricas => Set<MedidaAntropometrica>();
    public DbSet<FotoSeguimiento> FotosSeguimiento => Set<FotoSeguimiento>();
    public DbSet<DocumentoPaciente> DocumentosPaciente => Set<DocumentoPaciente>();
    public DbSet<Cita> Citas => Set<Cita>();
    public DbSet<Consulta> Consultas => Set<Consulta>();
    public DbSet<Nutricionista> Nutricionistas => Set<Nutricionista>();
    public DbSet<HorarioAtencion> HorariosAtencion => Set<HorarioAtencion>();
    public DbSet<Ausencia> Ausencias => Set<Ausencia>();
    public DbSet<Alimento> Alimentos => Set<Alimento>();
    public DbSet<MedidaCasera> MedidasCaseras => Set<MedidaCasera>();
    public DbSet<Plato> Platos => Set<Plato>();
    public DbSet<DetallePlato> DetallesPlato => Set<DetallePlato>();
    public DbSet<PlantillaMenu> PlantillasMenu => Set<PlantillaMenu>();
    public DbSet<DetallePlantillaMenu> DetallesPlantillaMenu => Set<DetallePlantillaMenu>();
    public DbSet<MenuAsignado> MenusAsignados => Set<MenuAsignado>();
    public DbSet<DetalleMenuAsignado> DetallesMenuAsignado => Set<DetalleMenuAsignado>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<DatosFiscalesClinica> DatosFiscalesClinica => Set<DatosFiscalesClinica>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Paciente>(entity =>
        {
            entity.Property(p => p.Sexo).HasConversion<string>();

            entity.ToTable("Pacientes", table =>
                table.HasCheckConstraint("CK_Pacientes_Sexo", "\"Sexo\" IN ('M','F','Otro')"));
        });

        modelBuilder.Entity<HistorialClinico>(entity =>
        {
            entity.HasOne(h => h.Paciente)
                  .WithOne(p => p.HistorialClinico)
                  .HasForeignKey<HistorialClinico>(h => h.PacienteId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(h => h.Alergias).HasColumnType("jsonb");
            entity.Property(h => h.AlimentosFavoritos).HasColumnType("jsonb");
            entity.Property(h => h.AlimentosNoFavoritos).HasColumnType("jsonb");
        });

        modelBuilder.Entity<MedidaAntropometrica>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.HasOne(m => m.Paciente)
                .WithMany()
                .HasForeignKey(m => m.PacienteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(m => m.ConsultaId).IsRequired(false);

            entity.HasOne(m => m.Consulta)
                .WithMany()
                .HasForeignKey(m => m.ConsultaId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(m => m.Peso).HasPrecision(5, 2);
            entity.Property(m => m.Estatura).HasPrecision(5, 2);
            entity.Property(m => m.Imc).HasPrecision(4, 2);
            entity.Property(m => m.PorcentajeGrasa).HasPrecision(4, 2);
            entity.Property(m => m.PorcentajeMasaMuscular).HasPrecision(4, 2);
            entity.Property(m => m.MedidaCintura).HasPrecision(5, 2);
            entity.Property(m => m.MedidaCadera).HasPrecision(5, 2);
        });

        modelBuilder.Entity<FotoSeguimiento>(entity =>
        {
            entity.HasKey(f => f.Id);

            entity.HasOne(f => f.Paciente)
                .WithMany()
                .HasForeignKey(f => f.PacienteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(f => f.Tipo).HasConversion<string>();

            entity.Property(f => f.ConsultaId).IsRequired(false);

            entity.HasOne(f => f.Consulta)
                .WithMany()
                .HasForeignKey(f => f.ConsultaId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DocumentoPaciente>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.HasOne(d => d.Paciente)
                .WithMany()
                .HasForeignKey(d => d.PacienteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(d => d.Tipo).HasConversion<string>();

            entity.Property(d => d.ConsultaId).IsRequired(false);

            entity.HasOne(d => d.Consulta)
                .WithMany()
                .HasForeignKey(d => d.ConsultaId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Cita>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.HasOne(c => c.Paciente)
                .WithMany(p => p.Citas)
                .HasForeignKey(c => c.PacienteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Nutricionista)
                .WithMany(n => n.Citas)
                .HasForeignKey(c => c.NutricionistaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(c => c.Canal).HasConversion<string>().HasMaxLength(20);

            entity.Property(c => c.Estado).HasConversion<string>();
            entity.Property(c => c.Motivo).HasMaxLength(500);
            entity.Property(c => c.MotivoCancelacion).HasMaxLength(500);

            entity.HasIndex(c => c.NutricionistaId);
            entity.HasIndex(c => new { c.PacienteId, c.FechaInicio });
            entity.HasIndex(c => c.FechaInicio);
        });

        modelBuilder.Entity<Consulta>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.HasOne(c => c.Paciente)
                .WithMany(p => p.Consultas)
                .HasForeignKey(c => c.PacienteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Cita)
                .WithOne(cita => cita.Consulta)
                .HasForeignKey<Consulta>(c => c.CitaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Nutricionista)
                .WithMany(n => n.Consultas)
                .HasForeignKey(c => c.NutricionistaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(c => c.TipoConsulta).HasConversion<string>();
            entity.Property(c => c.NotasClinicas).HasMaxLength(4000);

            entity.HasIndex(c => c.CitaId).IsUnique();
            entity.HasIndex(c => new { c.PacienteId, c.FechaInicio });
            entity.HasIndex(c => c.NutricionistaId);
        });

        modelBuilder.Entity<HorarioAtencion>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.HasOne(h => h.Nutricionista)
                .WithMany(n => n.Horarios)
                .HasForeignKey(h => h.NutricionistaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(h => h.DiaSemana).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(h => new { h.NutricionistaId, h.DiaSemana });
        });

        modelBuilder.Entity<Ausencia>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasOne(a => a.Nutricionista)
                .WithMany(n => n.Ausencias)
                .HasForeignKey(a => a.NutricionistaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(a => a.Motivo).HasMaxLength(150);
            entity.HasIndex(a => new { a.NutricionistaId, a.FechaInicio, a.FechaFin });
        });

        modelBuilder.Entity<Nutricionista>(entity =>
        {
            entity.HasKey(n => n.Id);

            entity.Property(n => n.NombreCompleto).HasMaxLength(100);
            entity.Property(n => n.Especialidad).HasMaxLength(100);
            entity.Property(n => n.Rol).HasConversion<string>().HasMaxLength(20);
            entity.Property(n => n.DuracionConsultaMinutos).HasDefaultValue(60);

            entity.HasIndex(n => n.CorreoElectronico)
                .IsUnique()
                .HasFilter("\"IsActive\" = true");

            entity.HasIndex(n => n.NumeroColegiatura)
                .IsUnique();
        });

        modelBuilder.Entity<Alimento>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(a => a.NombreNormalizado).HasMaxLength(150).IsRequired();

            entity.Property(a => a.Calorias).HasPrecision(6, 2);
            entity.Property(a => a.Proteinas).HasPrecision(6, 2);
            entity.Property(a => a.Carbohidratos).HasPrecision(6, 2);
            entity.Property(a => a.Grasas).HasPrecision(6, 2);

            entity.Property(a => a.ImagenUrl).HasMaxLength(1000);

            entity.HasIndex(a => a.NombreNormalizado)
                .IsUnique()
                .HasFilter("\"IsActive\" = true");

            entity.HasIndex(a => a.Calorias);
            entity.HasIndex(a => a.IsActive);
        });

        modelBuilder.Entity<Plato>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(p => p.Descripcion).HasMaxLength(1000);
            entity.Property(p => p.ImagenUrl).HasMaxLength(1000);

            entity.HasOne(p => p.Nutricionista)
                .WithMany()
                .HasForeignKey(p => p.NutricionistaId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(p => p.Nombre);
            entity.HasIndex(p => p.IsActive);
        });

        modelBuilder.Entity<DetallePlato>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.HasOne(d => d.Plato)
                .WithMany(p => p.Detalles)
                .HasForeignKey(d => d.PlatoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Alimento)
                .WithMany()
                .HasForeignKey(d => d.AlimentoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(d => d.RacionGramos).HasPrecision(7, 2);
            entity.Property(d => d.MedidaCantidad).HasPrecision(7, 2);
            entity.Property(d => d.MedidaNombre).HasMaxLength(40);

            entity.HasIndex(d => new { d.PlatoId, d.AlimentoId }).IsUnique();
        });

        modelBuilder.Entity<MedidaCasera>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Nombre).HasMaxLength(40).IsRequired();
            entity.Property(m => m.Gramos).HasPrecision(7, 2);

            entity.HasOne(m => m.Alimento)
                .WithMany(a => a.Medidas)
                .HasForeignKey(m => m.AlimentoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(m => new { m.AlimentoId, m.Nombre }).IsUnique();
        });

        modelBuilder.Entity<PlantillaMenu>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(p => p.Descripcion).HasMaxLength(1000);
            entity.Property(p => p.ObjetivoCalorico).HasPrecision(6, 2);

            entity.HasOne(p => p.Nutricionista)
                .WithMany()
                .HasForeignKey(p => p.NutricionistaId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(p => p.Nombre);
            entity.HasIndex(p => p.IsActive);
        });

        modelBuilder.Entity<DetallePlantillaMenu>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.HasOne(d => d.PlantillaMenu)
                .WithMany(p => p.Detalles)
                .HasForeignKey(d => d.PlantillaMenuId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Plato)
                .WithMany()
                .HasForeignKey(d => d.PlatoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(d => d.DiaSemana).HasConversion<string>();
            entity.Property(d => d.TiempoComida).HasConversion<string>();
            entity.Property(d => d.Cantidad).HasPrecision(5, 2);
            entity.Property(d => d.Indicaciones).HasMaxLength(500);

            entity.ToTable("DetallesPlantillaMenu", table =>
            {
                table.HasCheckConstraint(
                    "CK_DetallesPlantillaMenu_DiaSemana",
                    "\"DiaSemana\" IN ('Lunes','Martes','Miercoles','Jueves','Viernes','Sabado','Domingo')");

                table.HasCheckConstraint(
                    "CK_DetallesPlantillaMenu_TiempoComida",
                    "\"TiempoComida\" IN ('Desayuno','MediaManana','Almuerzo','Merienda','Cena','Colacion')");

                table.HasCheckConstraint(
                    "CK_DetallesPlantillaMenu_Cantidad",
                    "\"Cantidad\" > 0");
            });

            entity.HasIndex(d => new { d.PlantillaMenuId, d.DiaSemana, d.TiempoComida, d.PlatoId })
                .IsUnique();
        });

        modelBuilder.Entity<MenuAsignado>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(m => m.Descripcion).HasMaxLength(1000);
            entity.Property(m => m.ObjetivoCalorico).HasPrecision(6, 2);
            entity.Property(m => m.Estado).HasConversion<string>();

            entity.HasOne(m => m.Paciente)
                .WithMany()
                .HasForeignKey(m => m.PacienteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.PlantillaMenu)
                .WithMany()
                .HasForeignKey(m => m.PlantillaMenuId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.ToTable("MenusAsignados", table =>
            {
                table.HasCheckConstraint(
                    "CK_MenusAsignados_Estado",
                    "\"Estado\" IN ('Borrador','Activo','Finalizado')");

                table.HasCheckConstraint(
                    "CK_MenusAsignados_ObjetivoCalorico",
                    "\"ObjetivoCalorico\" IS NULL OR \"ObjetivoCalorico\" > 0");
            });

            entity.HasIndex(m => m.PacienteId);
            entity.HasIndex(m => m.Estado);
            entity.HasIndex(m => m.FechaInicio);
            entity.HasIndex(m => m.IsActive);
        });

        modelBuilder.Entity<DetalleMenuAsignado>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.HasOne(d => d.MenuAsignado)
                .WithMany(m => m.Detalles)
                .HasForeignKey(d => d.MenuAsignadoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Plato)
                .WithMany()
                .HasForeignKey(d => d.PlatoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(d => d.DiaSemana).HasConversion<string>();
            entity.Property(d => d.TiempoComida).HasConversion<string>();
            entity.Property(d => d.Cantidad).HasPrecision(5, 2);
            entity.Property(d => d.Indicaciones).HasMaxLength(500);

            entity.ToTable("DetallesMenuAsignado", table =>
            {
                table.HasCheckConstraint(
                    "CK_DetallesMenuAsignado_DiaSemana",
                    "\"DiaSemana\" IN ('Lunes','Martes','Miercoles','Jueves','Viernes','Sabado','Domingo')");

                table.HasCheckConstraint(
                    "CK_DetallesMenuAsignado_TiempoComida",
                    "\"TiempoComida\" IN ('Desayuno','MediaManana','Almuerzo','Merienda','Cena','Colacion')");

                table.HasCheckConstraint(
                    "CK_DetallesMenuAsignado_Cantidad",
                    "\"Cantidad\" > 0");
            });

            entity.HasIndex(d => new { d.MenuAsignadoId, d.DiaSemana, d.TiempoComida, d.PlatoId })
                .IsUnique();
        });

        modelBuilder.Entity<Pago>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Monto).HasPrecision(10, 2);
            entity.Property(p => p.Moneda).HasMaxLength(3).IsRequired();
            entity.Property(p => p.NumeroRecibo).HasMaxLength(20).IsRequired();
            entity.Property(p => p.Notas).HasMaxLength(500);
            entity.Property(p => p.MetodoPago).HasConversion<string>();
            entity.Property(p => p.Estado).HasConversion<string>();

            entity.HasOne(p => p.Consulta)
                .WithMany()
                .HasForeignKey(p => p.ConsultaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Paciente)
                .WithMany()
                .HasForeignKey(p => p.PacienteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable("Pagos", table =>
            {
                table.HasCheckConstraint(
                    "CK_Pagos_MetodoPago",
                    "\"MetodoPago\" IN ('Efectivo','TarjetaDebito','TarjetaCredito','Transferencia','QR','Otro')");

                table.HasCheckConstraint(
                    "CK_Pagos_Estado",
                    "\"Estado\" IN ('Pagado','Anulado')");

                table.HasCheckConstraint(
                    "CK_Pagos_Monto",
                    "\"Monto\" > 0");
            });

            // Solo un pago vigente por consulta; al anularse se libera para volver a cobrar.
            entity.HasIndex(p => p.ConsultaId)
                .IsUnique()
                .HasFilter("\"Estado\" = 'Pagado'");

            entity.HasIndex(p => p.NumeroRecibo).IsUnique();
            entity.HasIndex(p => p.PacienteId);
            entity.HasIndex(p => p.FechaPago);
            entity.HasIndex(p => p.IsActive);
        });

        modelBuilder.Entity<Factura>(entity =>
        {
            entity.HasKey(f => f.Id);

            entity.Property(f => f.TipoPersona).HasConversion<string>();
            entity.Property(f => f.UsoCfdi).HasConversion<string>();
            entity.Property(f => f.Estatus).HasConversion<string>();
            entity.Property(f => f.RegimenFiscal).HasConversion(
                v => ((int)v).ToString(),
                v => (RegimenFiscal)int.Parse(v));

            entity.Property(f => f.RFC).HasMaxLength(13).IsRequired();
            entity.Property(f => f.NombreRazonSocial).HasMaxLength(200).IsRequired();
            entity.Property(f => f.CodigoPostal).HasMaxLength(5).IsRequired();
            entity.Property(f => f.Serie).HasMaxLength(10).IsRequired();
            entity.Property(f => f.UUID).HasMaxLength(36);
            entity.Property(f => f.Subtotal).HasPrecision(10, 2);
            entity.Property(f => f.TarifaIva).HasPrecision(5, 2);
            entity.Property(f => f.Iva).HasPrecision(10, 2);
            entity.Property(f => f.Total).HasPrecision(10, 2);

            entity.HasOne(f => f.Pago)
                .WithOne()
                .HasForeignKey<Factura>(f => f.PagoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable("Facturas", table =>
            {
                table.HasCheckConstraint(
                    "CK_Facturas_TipoPersona",
                    "\"TipoPersona\" IN ('Fisica','Moral')");

                table.HasCheckConstraint(
                    "CK_Facturas_Estatus",
                    "\"Estatus\" IN ('Borrador','Emitida','Anulada')");

                table.HasCheckConstraint(
                    "CK_Facturas_UsoCfdi",
                    "\"UsoCfdi\" IN ('G01','G02','G03','I01','I02','I03','I04','I05','I06','I07','I08',"
                    + "'D01','D02','D03','D04','D06','D07','D08','D09','D10','S01','CP01','CN01')");

                table.HasCheckConstraint(
                    "CK_Facturas_RegimenFiscal",
                    "\"RegimenFiscal\" IN ('601','603','605','606','607','608','610','611','612','614',"
                    + "'615','616','621','622','623','624','625','626')");

                table.HasCheckConstraint(
                    "CK_Facturas_Totales",
                    "\"Subtotal\" >= 0 AND \"Iva\" >= 0 AND \"Total\" >= 0");
            });

            entity.HasIndex(f => f.PagoId).IsUnique();
            entity.HasIndex(f => f.RFC);
            entity.HasIndex(f => f.FechaEmision);
            entity.HasIndex(f => f.IsActive);
        });

        modelBuilder.Entity<DatosFiscalesClinica>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.Property(d => d.RegimenFiscal).HasConversion(
                v => ((int)v).ToString(),
                v => (RegimenFiscal)int.Parse(v));

            entity.Property(d => d.RazonSocial).HasMaxLength(200).IsRequired();
            entity.Property(d => d.RFC).HasMaxLength(13).IsRequired();
            entity.Property(d => d.CodigoPostal).HasMaxLength(5).IsRequired();
            entity.Property(d => d.Direccion).HasMaxLength(300);
            entity.Property(d => d.Telefono).HasMaxLength(30);
            entity.Property(d => d.Correo).HasMaxLength(150);
            entity.Property(d => d.LogoUrl).HasMaxLength(1000);
            entity.Property(d => d.Serie).HasMaxLength(10).IsRequired();
            entity.Property(d => d.TarifaIva).HasPrecision(5, 2);

            entity.ToTable("DatosFiscalesClinica", table =>
            {
                table.HasCheckConstraint(
                    "CK_DatosFiscalesClinica_RegimenFiscal",
                    "\"RegimenFiscal\" IN ('601','603','605','606','607','608','610','611','612','614',"
                    + "'615','616','621','622','623','624','625','626')");

                table.HasCheckConstraint(
                    "CK_DatosFiscalesClinica_TarifaIva",
                    "\"TarifaIva\" >= 0 AND \"TarifaIva\" <= 100");
            });
        });
    }
}