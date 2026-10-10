using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Entities;

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
    public DbSet<Alimento> Alimentos => Set<Alimento>();
    public DbSet<Plato> Platos => Set<Plato>();
    public DbSet<DetallePlato> DetallesPlato => Set<DetallePlato>();
    public DbSet<PlantillaMenu> PlantillasMenu => Set<PlantillaMenu>();
    public DbSet<DetallePlantillaMenu> DetallesPlantillaMenu => Set<DetallePlantillaMenu>();

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

        modelBuilder.Entity<Nutricionista>(entity =>
        {
            entity.HasKey(n => n.Id);

            entity.Property(n => n.NombreCompleto).HasMaxLength(100);
            entity.Property(n => n.Especialidad).HasMaxLength(100);

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

            entity.HasIndex(d => new { d.PlatoId, d.AlimentoId }).IsUnique();
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
    }
}