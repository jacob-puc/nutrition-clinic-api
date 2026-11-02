namespace nutriclinica_backend.Core.Enums;

/// <summary>
/// Rol de una cuenta del sistema. Administrador y Nutricionista atienden
/// pacientes; Recepcion agenda y cobra, pero no ve informacion clinica.
/// </summary>
public enum RolUsuario
{
    Administrador = 1,
    Nutricionista = 2,
    Recepcion = 3
}
