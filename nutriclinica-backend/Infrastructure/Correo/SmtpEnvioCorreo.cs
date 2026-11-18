using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;

namespace nutriclinica_backend.Infrastructure.Correo;

/// <summary>
/// Envio por SMTP (pensado para Gmail con contrasena de aplicacion). Aqui la
/// invitacion va como parte text/calendar del mensaje, que es iMIP completo:
/// Gmail y Outlook la agregan al calendario y la actualizan al cancelar.
/// </summary>
public class SmtpEnvioCorreo : IEnvioCorreo
{
    private readonly CorreoConfig _config;

    public SmtpEnvioCorreo(CorreoConfig config)
    {
        _config = config;
    }

    public string Nombre => "Smtp";

    public async Task EnviarAsync(MensajeCorreo mensaje, CancellationToken ct = default)
    {
        var smtp = _config.Smtp;
        if (!smtp.EstaConfigurado)
            throw new CorreoNoConfiguradoException("Faltan Correo__Smtp__Usuario o Correo__Smtp__Contrasena.");

        using var correo = new MailMessage
        {
            From = new MailAddress(_config.Remitente, _config.RemitenteNombre, Encoding.UTF8),
            Subject = mensaje.Asunto,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
        };
        correo.To.Add(new MailAddress(mensaje.Para, mensaje.ParaNombre ?? mensaje.Para, Encoding.UTF8));

        correo.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            mensaje.Texto, Encoding.UTF8, MediaTypeNames.Text.Plain));
        correo.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            mensaje.Html, Encoding.UTF8, MediaTypeNames.Text.Html));

        if (mensaje.Ics is not null)
        {
            var tipo = new ContentType("text/calendar") { CharSet = "UTF-8" };
            tipo.Parameters.Add("method", mensaje.IcsMetodo ?? "REQUEST");
            var vista = AlternateView.CreateAlternateViewFromString(mensaje.Ics, tipo);
            vista.TransferEncoding = TransferEncoding.Base64;
            correo.AlternateViews.Add(vista);
        }

        foreach (var adjunto in mensaje.Adjuntos)
        {
            correo.Attachments.Add(new Attachment(new MemoryStream(adjunto.Contenido), adjunto.Nombre, adjunto.TipoContenido));
        }

        using var cliente = new SmtpClient(smtp.Host, smtp.Puerto)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(smtp.Usuario.Trim(), smtp.Contrasena.Replace(" ", "")),
            Timeout = 20000,
        };
        await cliente.SendMailAsync(correo, ct);
    }
}
