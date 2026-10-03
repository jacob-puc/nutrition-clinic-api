namespace nutriclinica_backend.Features.Antropometria.DTOs;

public class MedidaRespuestaDto
{
    public Guid Id { get; set; }
    public Guid PacienteId { get; set; }
    public Guid? ConsultaId { get; set; }
    public DateTime FechaMedicion { get; set; }
    
    public decimal Peso { get; set; }
    public decimal Estatura { get; set; }
    public decimal Imc { get; set; }
    
    public decimal? PorcentajeGrasa { get; set; }
    public decimal? PorcentajeMasaMuscular { get; set; }
    public decimal? MedidaCintura { get; set; }
    public decimal? MedidaCadera { get; set; }
    
    public string? NotasObservaciones { get; set; }
}