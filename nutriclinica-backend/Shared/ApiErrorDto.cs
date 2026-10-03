namespace nutriclinica_backend.Shared;

public class ApiErrorDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public List<ErrorDetalleDto>? Errores { get; set; }
}
