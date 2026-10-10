namespace nutriclinica_backend.Features.Menus.DTOs;

public class ActualizarPlantillaMenuDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal? ObjetivoCalorico { get; set; }
    public List<CrearDetallePlantillaMenuDto> Detalles { get; set; } = [];
}
