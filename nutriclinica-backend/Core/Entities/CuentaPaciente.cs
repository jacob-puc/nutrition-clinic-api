namespace nutriclinica_backend.Core.Entities;

/// <summary>
/// Acceso del paciente a la app movil. Separada de <see cref="Paciente"/>:
/// el expediente lo crea la clinica (o la reserva en linea) y la cuenta solo
/// se le vincula cuando el correo esta verificado (codigo por correo o
/// Google), para que nadie pueda ver un historial ajeno registrandose con
/// el correo de otra persona.
/// </summary>
public class CuentaPaciente
{
    public Guid Id { get; set; }

    /// <summary>Correo normalizado (minusculas, sin espacios). Unico.</summary>
    public string Correo { get; set; } = string.Empty;

    /// <summary>Null hasta verificar el correo; despues, el expediente vinculado.</summary>
    public Guid? PacienteId { get; set; }
    public virtual Paciente? Paciente { get; set; }

    /// <summary>Null si solo entra con Google.</summary>
    public string? PasswordHash { get; set; }

    /// <summary>"sub" de Google: identifica la cuenta aunque cambie el correo.</summary>
    public string? GoogleId { get; set; }

    public bool CorreoVerificado { get; set; }

    /* Datos que da al registrarse; se usan solo si no existe expediente
       con ese correo y hay que crearlo. */
    public string NombreRegistro { get; set; } = string.Empty;
    public string? TelefonoRegistro { get; set; }
    public Enums.Sexo? SexoRegistro { get; set; }

    /* Codigo de 6 digitos (verificacion o recuperar contraseña). Se guarda
       solo su hash; caduca y admite pocos intentos. */
    public string? CodigoHash { get; set; }
    public string? CodigoProposito { get; set; }
    public DateTime? CodigoExpira { get; set; }
    public int CodigoIntentos { get; set; }
    public DateTime? CodigoEnviadoEn { get; set; }

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? UltimoAcceso { get; set; }
}
