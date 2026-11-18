using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Features.PortalPaciente;

public class RegistroPacienteDto
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public Sexo Sexo { get; set; }
    public string Contrasena { get; set; } = string.Empty;
}

public class VerificarCodigoDto
{
    public string CorreoElectronico { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
}

public class CorreoDto
{
    public string CorreoElectronico { get; set; } = string.Empty;
}

public class LoginPacienteDto
{
    public string CorreoElectronico { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
}

public class GooglePacienteDto
{
    /// <summary>ID token que entrega Google Sign-In en el dispositivo.</summary>
    public string IdToken { get; set; } = string.Empty;
}

public class RestablecerContrasenaDto
{
    public string CorreoElectronico { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string ContrasenaNueva { get; set; } = string.Empty;
}

public class RefreshPacienteDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>Respuesta de inicio de sesion del paciente.</summary>
public class TokenPacienteDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiraEnSegundos { get; set; }

    /// <summary>Siempre "Paciente": la app decide con esto que modo mostrar.</summary>
    public string Rol { get; set; } = "Paciente";
    public Guid PacienteId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;
}

/// <summary>Registro iniciado: falta escribir el codigo que llego al correo.</summary>
public record VerificacionPendienteDto(string CorreoElectronico, string Mensaje);
