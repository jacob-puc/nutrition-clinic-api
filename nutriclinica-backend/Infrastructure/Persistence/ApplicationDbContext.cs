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
    }
}