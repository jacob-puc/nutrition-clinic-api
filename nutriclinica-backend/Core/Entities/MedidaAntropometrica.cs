namespace nutriclinica_backend.Core.Entities;

public class MedidaAntropometrica
{
    public Guid Id { get; set; }
    
    public Guid PacienteId { get; set; }
    public virtual Paciente Paciente { get; set; } = null!;
    
    public Guid? CitaId { get; set; }
    
    public DateTime FechaMedicion { get; set; }
    
    //Medidas Básicas
    public decimal Peso { get; set; }
    public decimal Imc { get; set; }
    public decimal Estatura { get; set; }
    
    //Opcionales
    public decimal? PorcentajeGrasa { get; set; }
    public decimal? PorcentajeMasaMuscular { get; set; }
    public decimal? MedidaCintura { get; set; }
    public decimal? MedidaCadera { get; set; }
    
    public string? NotasObservaciones { get; set; }
    

}