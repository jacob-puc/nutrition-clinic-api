namespace nutriclinica_backend.Core.DTOs;

public class RespuestaPaginadaDto<T>
{
    public int TotalRegistros { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int TotalPaginas { get; set; }
    public List<T> Datos { get; set; } = [];
}
