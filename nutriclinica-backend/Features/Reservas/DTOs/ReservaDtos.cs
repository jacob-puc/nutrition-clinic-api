using nutriclinica_backend.Core.Enums;
using nutriclinica_backend.Features.Agenda.DTOs;

namespace nutriclinica_backend.Features.Reservas.DTOs;

/* Lo que ve el publico: solo lo necesario para elegir, sin datos internos. */

public class ClinicaPublicaDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
}

public class ProfesionalPublicoDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Especialidad { get; set; }
    public int DuracionMinutos { get; set; }
}

public class OpcionesReservaDto
{
    public ClinicaPublicaDto Clinica { get; set; } = new();
    public List<ProfesionalPublicoDto> Profesionales { get; set; } = [];
    /// <summary>Horario general de la clinica, para la pagina de inicio.</summary>
    public List<BloqueHorarioDto> HorarioClinica { get; set; } = [];
    /// <summary>Horas minimas entre la reservacion y la cita.</summary>
    public int AnticipacionHoras { get; set; }
    /// <summary>Hasta cuantos dias adelante se puede reservar.</summary>
    public int DiasMaximos { get; set; }
}

public class HoraPublicaDto
{
    public DateTime Inicio { get; set; }
    /// <summary>"HH:mm" en hora de la clinica.</summary>
    public string Hora { get; set; } = string.Empty;
}

public class DiaPublicoDto
{
    public DateOnly Fecha { get; set; }
    public List<HoraPublicaDto> Horas { get; set; } = [];
}

public class CrearReservaDto
{
    /// <summary>Null: con quien este libre a esa hora.</summary>
    public Guid? NutricionistaId { get; set; }
    public DateTime Inicio { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;
    public Sexo Sexo { get; set; }
    public string? Motivo { get; set; }
    /// <summary>Campo trampa: invisible para personas, los bots lo llenan.</summary>
    public string? Sitio { get; set; }
}

public class ReservaConfirmadaDto
{
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string NutricionistaNombre { get; set; } = string.Empty;
    public ClinicaPublicaDto Clinica { get; set; } = new();
}
