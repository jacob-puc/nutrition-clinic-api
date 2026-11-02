using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Core.Entities;

/// <summary>
/// Bloque semanal de atencion (p. ej. lunes 09:00-14:00). Sin nutricionista
/// es el horario general de la clinica, el que usa quien no tiene uno propio.
/// Las horas son de la clinica (Mexico), no UTC.
/// </summary>
public class HorarioAtencion
{
    public Guid Id { get; set; }
    public Guid? NutricionistaId { get; set; }
    public DiaSemana DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }

    public virtual Nutricionista? Nutricionista { get; set; }
}
