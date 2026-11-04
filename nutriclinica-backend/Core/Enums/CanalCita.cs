namespace nutriclinica_backend.Core.Enums;

/// <summary>Por donde llego la cita. Null en las citas anteriores a este campo.</summary>
public enum CanalCita
{
    Presencial = 1,
    Telefono = 2,
    WhatsApp = 3,
    EnLinea = 4
}
