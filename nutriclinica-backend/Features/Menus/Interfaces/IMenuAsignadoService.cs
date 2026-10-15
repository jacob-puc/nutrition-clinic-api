using System.Collections;
using nutriclinica_backend.Features.Menus.DTOs;

namespace nutriclinica_backend.Features.Menus.Interfaces;

public interface IMenuAsignadoService
{
    Task<IEnumerable<MenuAsignadoRespuestaDto>> ObtenerMenuAsignadoAsync(MenuAsignadoFiltroDto filtro);
    Task<MenuAsignadoRespuestaDto> ObtenerMenuAsignadoPorIdAsync(Guid menuId);
    Task<MenuAsignadoRespuestaDto> ActualizarMenuAsignadoAsync(Guid menuId, ActualizarMenuAsignadoDto dto);
    Task<MenuAsignadoRespuestaDto> AsignarPlantillaAsync(CrearMenuAsignadoDto dto);
    Task<MenuAsignadoRespuestaDto> DuplicarMenuAsignadoAsync(Guid menuId, string? nombreNuevo, Guid? pacienteId);
    Task RestaurarMenuAsignadoAsync(Guid menuId);
    Task EliminarMenuAsignadoAsync(Guid menuId);
}
