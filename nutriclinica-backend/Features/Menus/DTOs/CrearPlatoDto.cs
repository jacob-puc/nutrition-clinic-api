namespace nutriclinica_backend.Features.Menus.DTOs;

public class CrearPlatoDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? ImagenUrl { get; set; }
    public Guid? NutricionistaId { get; set; }
    public List<CrearDetallePlatoDto> Detalles { get; set; } = [];
}
