namespace nutriclinica_backend.Features.Alimentos.DTOs;

public class ActualizarAlimentoDto
{
    public string Nombre { get; set; } = string.Empty;
    public decimal Calorias { get; set; }
    public decimal Proteinas { get; set; }
    public decimal Carbohidratos { get; set; }
    public decimal Grasas { get; set; }
    public string? ImagenUrl { get; set; }

    /// <summary>Lista completa de medidas; null deja las que ya tenga.</summary>
    public List<MedidaCaseraDto>? Medidas { get; set; }
}
