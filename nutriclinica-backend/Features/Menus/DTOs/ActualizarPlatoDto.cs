namespace nutriclinica_backend.Features.Menus.DTOs;

public class ActualizarPlatoDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? ImagenUrl { get; set; }
    public List<CrearDetallePlatoDto> Detalles { get; set; } = [];
}
