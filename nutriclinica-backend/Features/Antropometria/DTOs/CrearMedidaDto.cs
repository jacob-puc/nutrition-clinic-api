namespace nutriclinica_backend.Features.Antropometria.DTOs;

public class CrearMedidaDto
{
    public Guid? CitaId { get; set; }

    public decimal Peso { get; set; }
    public decimal Estatura { get; set; }

    public decimal? PorcentajeGrasa { get; set; }
    public decimal? PorcentajeMasaMuscular { get; set; }
    public decimal? MedidaCintura { get; set; }
    public decimal? MedidaCadera { get; set; }

    public string? NotasObservaciones { get; set; }
}