using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Core.Utils;

/// <summary>Nombres para el usuario de los estados internos (NoAsistio -> "No asistió").</summary>
public static class TextoEstado
{
    public static string Cita(EstadoCita estado) => estado switch
    {
        EstadoCita.Programada => "Programada",
        EstadoCita.Confirmada => "Confirmada",
        EstadoCita.EnCurso => "En curso",
        EstadoCita.Completada => "Completada",
        EstadoCita.Cancelada => "Cancelada",
        EstadoCita.NoAsistio => "No asistió",
        _ => estado.ToString(),
    };
}
