namespace nutriclinica_backend.Infrastructure.Correo;

/// <summary>
/// Envio de correos. Se configura con variables de entorno:
/// <c>Correo__BrevoApiKey</c>, <c>Correo__RemitenteCorreo</c>,
/// <c>Correo__RemitenteNombre</c>; o, para enviar por Gmail directo,
/// <c>Correo__Smtp__Usuario</c> y <c>Correo__Smtp__Contrasena</c>
/// (contrasena de aplicacion). <c>Correo__Proveedor</c> elige uno a la
/// fuerza ("Brevo" o "Smtp"); vacio = el que este configurado, Brevo primero.
/// </summary>
public class CorreoConfig
{
    public const string SectionName = "Correo";

    public string? Proveedor { get; set; }

    public string BrevoApiKey { get; set; } = string.Empty;

    public string RemitenteCorreo { get; set; } = string.Empty;
    public string RemitenteNombre { get; set; } = "NutriClínica";

    public SmtpConfig Smtp { get; set; } = new();

    /// <summary>
    /// Direccion publica de la web (p. ej. https://nutriclinica.vercel.app),
    /// para los botones "Reservar otra cita" y "Ver mi agenda". Opcional.
    /// </summary>
    public string? UrlSitio { get; set; }

    /// <summary>
    /// Direccion publica de este backend (p. ej.
    /// https://nutrition-clinic-api.onrender.com), para los botones
    /// "Confirmar asistencia" y "No podre asistir". Sin ella no se muestran.
    /// </summary>
    public string? UrlApi { get; set; }

    /// <summary>
    /// Enlace para descargar la app (p. ej. el APK publicado). Si existe, la
    /// invitacion a la app lleva el boton "Descargar la app".
    /// </summary>
    public string? UrlApp { get; set; }

    /// <summary>Correos sin enviar con mas antiguedad que esto se descartan.</summary>
    public int HorasMaximasEnCola { get; set; } = 48;

    public string ProveedorEfectivo
    {
        get
        {
            if (string.Equals(Proveedor, "Brevo", StringComparison.OrdinalIgnoreCase)) return "Brevo";
            if (string.Equals(Proveedor, "Smtp", StringComparison.OrdinalIgnoreCase)) return "Smtp";
            if (!string.IsNullOrWhiteSpace(BrevoApiKey)) return "Brevo";
            if (Smtp.EstaConfigurado) return "Smtp";
            return "Ninguno";
        }
    }

    public bool EstaConfigurado => ProveedorEfectivo switch
    {
        "Brevo" => !string.IsNullOrWhiteSpace(BrevoApiKey) && !string.IsNullOrWhiteSpace(Remitente),
        "Smtp" => Smtp.EstaConfigurado,
        _ => false
    };

    /// <summary>Con Gmail el remitente es la propia cuenta.</summary>
    public string Remitente =>
        !string.IsNullOrWhiteSpace(RemitenteCorreo) ? RemitenteCorreo.Trim() : Smtp.Usuario.Trim();
}

public class SmtpConfig
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Puerto { get; set; } = 587;
    public string Usuario { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;

    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(Usuario) && !string.IsNullOrWhiteSpace(Contrasena);
}
