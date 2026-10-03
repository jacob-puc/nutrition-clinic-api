namespace nutriclinica_backend.Features.Pacientes.DTOs;

/// <summary>Objetivo clinico, capturado en consulta y no al alta del paciente.</summary>
public class ActualizarObjetivoPacienteDto
{
    public string? TituloObjetivo { get; set; }
    public decimal? PesoObjetivo { get; set; }
}
