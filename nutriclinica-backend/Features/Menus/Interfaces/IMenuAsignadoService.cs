using nutriclinica_backend.Features.Menus.DTOs;

namespace nutriclinica_backend.Features.Menus.Interfaces;

public interface IMenuAsignadoService
{
    Task<MenuAsignadoRespuestaDto> AsignarPlantillaAsync(CrearMenuAsignadoDto dto);
    Task<MenuAsignadoRespuestaDto> DuplicarMenuAsignadoAsync(Guid menuId, string? nombreNuevo, Guid? pacienteId);
}
