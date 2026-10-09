namespace nutriclinica_backend.Features.Alimentos.DTOs;

public class AlimentoRespuestaDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public decimal Calorias { get; set; }
    public decimal Proteinas { get; set; }
    public decimal Carbohidratos { get; set; }
    public decimal Grasas { get; set; }

    public string? ImagenUrl { get; set; }

    public DateTime FechaCreacion { get; set; }
    public bool IsActive { get; set; }

    public decimal CaloriasEstimadas { get; set; }
    public decimal DiferenciaCalorica { get; set; }
}
