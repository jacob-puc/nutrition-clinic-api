namespace nutriclinica_backend.Features.Alimentos.DTOs;

public class MedidaCaseraDto
{
    public Guid? Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Gramos { get; set; }
}
