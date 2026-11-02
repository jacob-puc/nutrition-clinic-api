using nutriclinica_backend.Core.Enums;
using nutriclinica_backend.Features.Nutricionistas.DTOs;

namespace nutriclinica_backend.Features.Agenda.DTOs;

/// <summary>Bloque de atencion. Horas "HH:mm" en hora de la clinica.</summary>
public class BloqueHorarioDto
{
    public DiaSemana DiaSemana { get; set; }
    public string HoraInicio { get; set; } = string.Empty;
    public string HoraFin { get; set; } = string.Empty;
}

public class HorarioPersonaDto
{
    public Guid NutricionistaId { get; set; }
    public bool HorarioPropio { get; set; }
    public int DuracionConsultaMinutos { get; set; }

    /// <summary>Los que aplican de verdad: los propios o, si no tiene, los de la clinica.</summary>
    public List<BloqueHorarioDto> Bloques { get; set; } = [];
}

/// <summary>Toda la configuracion de agenda en una sola respuesta: es chica y la usan varias pantallas.</summary>
public class HorariosRespuestaDto
{
    public List<BloqueHorarioDto> Clinica { get; set; } = [];
    public List<HorarioPersonaDto> Personas { get; set; } = [];
}

public class ActualizarHorarioClinicaDto
{
    public List<BloqueHorarioDto> Bloques { get; set; } = [];
}

public class ActualizarHorarioPersonaDto
{
    public bool HorarioPropio { get; set; }
    public int DuracionConsultaMinutos { get; set; } = 60;

    /// <summary>Se ignoran si HorarioPropio es false.</summary>
    public List<BloqueHorarioDto> Bloques { get; set; } = [];
}

public class AusenciaDto
{
    public Guid Id { get; set; }
    public Guid NutricionistaId { get; set; }
    public string NutricionistaNombre { get; set; } = string.Empty;
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public string? Motivo { get; set; }
}

public class CrearAusenciaDto
{
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public string? Motivo { get; set; }
}

public class AusenciaCreadaDto
{
    public AusenciaDto Ausencia { get; set; } = new();

    /// <summary>Citas pendientes que caen en esos dias: hay que reasignarlas o moverlas.</summary>
    public List<CitaPendienteDto> CitasAfectadas { get; set; } = [];
}

/// <summary>Espacio libre para iniciar una cita de la duracion pedida.</summary>
public class HuecoDto
{
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }

    /// <summary>"HH:mm" en hora de la clinica, para mostrar sin convertir.</summary>
    public string Hora { get; set; } = string.Empty;
}

public class DisponibilidadDiaDto
{
    public DateOnly Fecha { get; set; }

    /// <summary>Por que no atiende ese dia (ausencia, dia sin horario), o null.</summary>
    public string? MotivoNoDisponible { get; set; }
    public List<HuecoDto> Huecos { get; set; } = [];
}

public class DisponibilidadDto
{
    public Guid NutricionistaId { get; set; }
    public string NutricionistaNombre { get; set; } = string.Empty;
    public int DuracionMinutos { get; set; }
    public List<DisponibilidadDiaDto> Dias { get; set; } = [];
}
