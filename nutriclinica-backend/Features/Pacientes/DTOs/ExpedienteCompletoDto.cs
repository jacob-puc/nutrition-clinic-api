using nutriclinica_backend.Features.Antropometria.DTOs;
using nutriclinica_backend.Features.ExpedienteMedia.DTOs;
using nutriclinica_backend.Features.HistorialesClinicos.DTOs;

namespace nutriclinica_backend.Features.Pacientes.DTOs;

public class ExpedienteCompletoDto
{
    public Guid PacienteId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public int Edad { get; set; }

    public HistorialClinicoRespuestaDto? HistorialClinico { get; set; }

    public List<MedidaRespuestaDto> MedidasAntropometricas { get; set; } = new();
    public List<FotoRespuestaDto> Fotos { get; set; } = new();
    public List<DocumentoRespuestaDto> Documentos { get; set; } = new();
}