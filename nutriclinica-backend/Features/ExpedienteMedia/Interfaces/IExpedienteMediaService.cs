using nutriclinica_backend.Features.ExpedienteMedia.DTOs;

namespace nutriclinica_backend.Features.ExpedienteMedia.Interfaces;

public interface IExpedienteMediaService
{
    Task<FotoRespuestaDto> RegistrarFotoAsync(Guid pacienteId, CrearFotoDto dto);
    Task<IEnumerable<FotoRespuestaDto>> ObtenerFotosPorPacienteAsync(Guid pacienteId);
    Task<FotoRespuestaDto> ObtenerFotoPorIdAsync(Guid pacienteId, Guid fotoId);
    Task<FotoRespuestaDto> ActualizarFotoAsync(Guid pacienteId, Guid fotoId, ActualizarFotoDto dto);
    Task EliminarFotoAsync(Guid pacienteId, Guid fotoId);

    Task<DocumentoRespuestaDto> RegistrarDocumentoAsync(Guid pacienteId, CrearDocumentoDto dto);
    Task<IEnumerable<DocumentoRespuestaDto>> ObtenerDocumentosPorPacienteAsync(Guid pacienteId);
    Task<DocumentoRespuestaDto> ObtenerDocumentoPorIdAsync(Guid pacienteId, Guid documentoId);
    Task<DocumentoRespuestaDto> ActualizarDocumentoAsync(Guid pacienteId, Guid documentoId, ActualizarDocumentoDto dto);
    Task EliminarDocumentoAsync(Guid pacienteId, Guid documentoId);
}