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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

            entity.Property(m => m.CitaId).IsRequired(false);

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
        });

        modelBuilder.Entity<DocumentoPaciente>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.HasOne(d => d.Paciente)
                .WithMany()
                .HasForeignKey(d => d.PacienteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(d => d.Tipo).HasConversion<string>();
        });
    }
}